﻿using System;
using System.Text;
using TDengine.Driver;
using TDengine.Driver.Client;
using Xunit;

namespace Driver.Test.Client.Query
{
    public partial class Client
    {
        [Fact]
        public void WebSocketQueryMSTest()
        {
            var db = "ws_query_test_ms";
            this.QueryTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MILLI);
        }

        [Fact]
        public void WebSocketQueryUSTest()
        {
            var db = "ws_query_test_us";
            this.QueryTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MICRO);
        }

        [Fact]
        public void WebSocketQueryNSTest()
        {
            var db = "ws_query_test_ns";
            this.QueryTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_NANO);
        }

        [Fact]
        public void WebSocketQueryWithReqIDMSTest()
        {
            var db = "ws_query_test_reqid_ms";
            this.QueryWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MILLI);
        }

        [Fact]
        public void WebSocketQueryWithReqIDUSTest()
        {
            var db = "ws_query_test_reqid_us";
            this.QueryWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MICRO);
        }

        [Fact]
        public void WebSocketQueryWithReqIDNSTest()
        {
            var db = "ws_query_test_reqid_ns";
            this.QueryWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_NANO);
        }

        [Fact]
        public void WebSocketStmtMSTest()
        {
            var db = "ws_stmt_test_ms";
            this.StmtTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MILLI);
        }

        [Fact]
        public void WebSocketStmtUSTest()
        {
            var db = "ws_stmt_test_us";
            this.StmtTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MICRO);
        }

        [Fact]
        public void WebSocketStmtNSTest()
        {
            var db = "ws_stmt_test_ns";
            this.StmtTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_NANO);
        }

        [Fact]
        public void WebSocketStmtWithReqIDMSTest()
        {
            var db = "ws_stmt_test_req_ms";
            this.StmtWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MILLI);
        }

        [Fact]
        public void WebSocketStmtWithReqIDUSTest()
        {
            var db = "ws_stmt_test_req_us";
            this.StmtWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MICRO);
        }

        [Fact]
        public void WebSocketStmtWithReqIDNSTest()
        {
            var db = "ws_stmt_test_req_ns";
            this.StmtWithReqIDTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_NANO);
        }

        [Fact]
        public void WebSocketStmtColumnsMSTest()
        {
            var db = "ws_stmt_columns_test_ms";
            this.StmtBindColumnsTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MILLI);
        }

        [Fact]
        public void WebSocketStmtColumnsUSTest()
        {
            var db = "ws_stmt_columns_test_us";
            this.StmtBindColumnsTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_MICRO);
        }

        [Fact]
        public void WebSocketStmtColumnsNSTest()
        {
            var db = "ws_stmt_columns_test_ns";
            this.StmtBindColumnsTest(this._wsConnectString, db, TDenginePrecision.TSDB_TIME_PRECISION_NANO);
        }

        [Fact]
        public void WebSocketVarbinaryTest()
        {
            var db = "ws_varbinary_test";
            this.VarbinaryTest(this._wsConnectString, db);
        }

        [Fact]
        public void WebSocketInfluxDBTest()
        {
            var db = "ws_influxdb_test";
            this.InfluxDBTest(this._wsConnectString, db);
        }

        [Fact]
        public void WebSocketTelnetTest()
        {
            var db = "ws_telnet_test";
            this.TelnetTest(this._wsConnectString, db);
        }

        [Fact]
        public void WebSocketSMLJsonTest()
        {
            var db = "ws_sml_json_test";
            this.SMLJsonTest(this._wsConnectString, db);
        }

        [Fact]
        public void WebSocketQueryConcurrencyTest()
        {
            var db = "ws_query_concurrency_test";
            this.QueryConcurrencyTest(this._wsConnectString, db);
        }

        [Fact]
        public void WebSocketQueryInvalidReqIdTest()
        {
            var db = "ws_invalid_reqid_test";

            var precision = TDenginePrecision.TSDB_TIME_PRECISION_MILLI;
            var builder = new ConnectionStringBuilder(_wsConnectString);
            var client = DbDriver.Open(builder);
            var count = 10;
            try
            {
                client.Exec($"drop database if exists {db}");
                client.Exec($"create database {db} precision '{PrecisionString(precision)}'");
                client.Exec($"use {db}");
                client.Exec("create table t1 (ts timestamp, a int, b float, c binary(10))");
                var ts = new long[count];
                var dateTime = DateTime.Now;
                var tsv = new DateTime[count];
                for (int i = 0; i < count; i++)
                {
                    ts[i] = (dateTime.Add(TimeSpan.FromSeconds(i)).ToUniversalTime().Ticks -
                             TDengineConstant.TimeZero.Ticks) / 10000;
                    tsv[i] = TDengineConstant.ConvertTimeToDatetime(ts[i], precision);
                }

                var valuesStr = "";
                for (int i = 0; i < count; i++)
                {
                    valuesStr += $"({ts[i]}, {i}, {i}, '中文')";
                }

                client.Exec($"insert into t1 values {valuesStr}");
                var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
                long reqid = 0x123456;
                bool haveException = false;
                for (var i = 0; i < count; i++)
                {
                    int localI = i;
                    string query = "select * from t1 where ts = " + ts[localI];
                    tasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            using (var rows = client.Query(query, reqid))
                            {
                                Assert.Equal(1, rows.GetOrdinal("a"));
                                var fieldCount = rows.FieldCount;
                                Assert.Equal(4, fieldCount);
                                Assert.Equal("ts", rows.GetName(0));
                                Assert.Equal("a", rows.GetName(1));
                                Assert.Equal("b", rows.GetName(2));
                                Assert.Equal("c", rows.GetName(3));
                                var haveNext = rows.Read();
                                Assert.True(haveNext);
                                Assert.Equal(tsv[localI], rows.GetValue(0));
                                Assert.Equal(localI, rows.GetValue(1));
                                Assert.Equal((float)localI, rows.GetValue(2));
                                Assert.Equal(Encoding.UTF8.GetBytes("中文"), rows.GetValue(3));
                            }
                        }
                        catch (AggregateException e)
                        {
                            if (e.InnerException != null)
                            {
                                Assert.Equal($"Request with reqId '{reqid}' already exists.", e.InnerException.Message);
                                haveException = true;
                            }
                        }
                    }));
                }

                System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
                Assert.True(haveException);
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                throw;
            }
            finally
            {
                client.Exec($"drop database if exists {db}");
                client.Dispose();
            }
        }
    }
}