using TDengine.Driver;
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
    }
}