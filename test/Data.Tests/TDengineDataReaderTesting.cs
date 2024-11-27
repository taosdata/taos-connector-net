using System;
using System.Data.Common;
using System.Text;
using TDengine.Data.Client;
using TDengine.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Data.Tests
{
    public class TDengineDataReaderTesting : IDisposable
    {
        private DbConnection _connection;
        private DbConnection _wsConnection;
        private readonly ITestOutputHelper _output;

        public TDengineDataReaderTesting(ITestOutputHelper output)
        {
            _output = output;
            var builder = new TDengineConnectionStringBuilder("username=root;password=taosdata");
            _connection = new TDengineConnection(builder.ConnectionString);
            _connection.Open();

            var wsBuilder =
                new TDengineConnectionStringBuilder(
                    "protocol=WebSocket;host=localhost;port=6041;useSSL=false;username=root;password=taosdata");
            _wsConnection = new TDengineConnection(wsBuilder.ConnectionString);
            _wsConnection.Open();
        }

        public void Dispose()
        {
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "drop database if exists test_common";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "drop database if exists test_stmt";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "drop database if exists test_nano";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = _wsConnection.CreateCommand())
                {
                    cmd.CommandText = "drop database if exists ws_test_common";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "drop database if exists ws_test_stmt";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "drop database if exists ws_test_nano";
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                throw;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
                _wsConnection.Close();
                _wsConnection.Dispose();
            }
        }

        [Fact]
        public void CommonExec()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists test_common";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database test_common";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _connection.ChangeDatabase("test_common");
                cmd.CommandText =
                    "create table if not exists test_types(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into test_types values(now+1s, null, null, null, null, null, null, null, null, null,null,null,null,null)," +
                    "(now, 1, 2, 3000000.3, 400000000.4, '5binary', 6, 7, true, '9nchar',10,11,12,13)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(2, affected);
                cmd.CommandText =
                    "select ts, f_int, f_bigint, f_float, f_double, f_binary, f_smallint, f_tinyint, f_bool, f_nchar,f_uint,f_ubigint,f_usmallint,f_utinyint from test_types order by ts desc limit 2";
                var reader = cmd.ExecuteReader();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists test_common";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }

        [Fact]
        public void Statement()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists test_stmt";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database test_stmt";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _connection.ChangeDatabase("test_stmt");
                cmd.CommandText =
                    "create table if not exists test_stb(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned) tags(val int)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into ? using test_stb tags (?) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?,?,?,?)";
                var now = DateTime.Now;
                DateTime nextSecond = now.AddSeconds(1);
                var parameters = cmd.Parameters;
                // table name;
                parameters.Add(new TDengineParameter("#table", "test_types"));
                // tag name;
                parameters.Add(new TDengineParameter("$0", (int)123));
                // value
                parameters.Add(new TDengineParameter("@0", now));
                parameters.Add(new TDengineParameter("@1", (int)1));
                parameters.Add(new TDengineParameter("@2", (long)2));
                parameters.Add(new TDengineParameter("@3", (float)3000000.3));
                parameters.Add(new TDengineParameter("@4", (double)400000000.4));
                parameters.Add(new TDengineParameter("@5", "5binary"));
                parameters.Add(new TDengineParameter("@6", (short)6));
                parameters.Add(new TDengineParameter("@7", (sbyte)7));
                parameters.Add(new TDengineParameter("@8", true));
                parameters.Add(new TDengineParameter("@9", "9nchar"));
                parameters.Add(new TDengineParameter("@10", (uint)10));
                parameters.Add(new TDengineParameter("@11", (ulong)11));
                parameters.Add(new TDengineParameter("@12", (ushort)12));
                parameters.Add(new TDengineParameter("@13", (byte)13));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                parameters.Add(new TDengineParameter("#table", "test_types"));
                parameters.Add(new TDengineParameter("$0", (int)123));
                parameters.Add(new TDengineParameter("@0", nextSecond));
                parameters.Add(new TDengineParameter("@1", null));
                parameters.Add(new TDengineParameter("@2", null));
                parameters.Add(new TDengineParameter("@3", null));
                parameters.Add(new TDengineParameter("@4", null));
                parameters.Add(new TDengineParameter("@5", null));
                parameters.Add(new TDengineParameter("@6", null));
                parameters.Add(new TDengineParameter("@7", null));
                parameters.Add(new TDengineParameter("@8", null));
                parameters.Add(new TDengineParameter("@9", null));
                parameters.Add(new TDengineParameter("@10", null));
                parameters.Add(new TDengineParameter("@11", null));
                parameters.Add(new TDengineParameter("@12", null));
                parameters.Add(new TDengineParameter("@13", null));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                cmd.CommandText =
                    "select * from test_types where ts <= ? order by ts desc limit 2";
                parameters.Add(new TDengineParameter("@0", nextSecond.AddSeconds(1)));
                var reader = cmd.ExecuteReader();
                parameters.Clear();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal((now.ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) / 10000,
                    (reader.GetDateTime(0).ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) / 10000);
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists test_stmt";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }

        [Fact]
        public void StatementNano()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists test_nano";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database test_nano precision 'ns'";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _connection.ChangeDatabase("test_nano");
                cmd.CommandText =
                    "create table if not exists test_stb(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned) tags(val int)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into ? using test_stb tags (?) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?,?,?,?)";
                var now = DateTime.Now;
                DateTime nextSecond = now.AddSeconds(1);
                var parameters = cmd.Parameters;
                // table name;
                parameters.Add(new TDengineParameter("#table", "test_types"));
                // tag name;
                parameters.Add(new TDengineParameter("$0", (int)123));
                // value
                parameters.Add(new TDengineParameter("@0", now));
                parameters.Add(new TDengineParameter("@1", (int)1));
                parameters.Add(new TDengineParameter("@2", (long)2));
                parameters.Add(new TDengineParameter("@3", (float)3000000.3));
                parameters.Add(new TDengineParameter("@4", (double)400000000.4));
                parameters.Add(new TDengineParameter("@5", "5binary"));
                parameters.Add(new TDengineParameter("@6", (short)6));
                parameters.Add(new TDengineParameter("@7", (sbyte)7));
                parameters.Add(new TDengineParameter("@8", true));
                parameters.Add(new TDengineParameter("@9", "9nchar"));
                parameters.Add(new TDengineParameter("@10", (uint)10));
                parameters.Add(new TDengineParameter("@11", (ulong)11));
                parameters.Add(new TDengineParameter("@12", (ushort)12));
                parameters.Add(new TDengineParameter("@13", (byte)13));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                parameters.Add(new TDengineParameter("#table", "test_types"));
                parameters.Add(new TDengineParameter("$0", (int)123));
                parameters.Add(new TDengineParameter("@0", nextSecond));
                parameters.Add(new TDengineParameter("@1", null));
                parameters.Add(new TDengineParameter("@2", null));
                parameters.Add(new TDengineParameter("@3", null));
                parameters.Add(new TDengineParameter("@4", null));
                parameters.Add(new TDengineParameter("@5", null));
                parameters.Add(new TDengineParameter("@6", null));
                parameters.Add(new TDengineParameter("@7", null));
                parameters.Add(new TDengineParameter("@8", null));
                parameters.Add(new TDengineParameter("@9", null));
                parameters.Add(new TDengineParameter("@10", null));
                parameters.Add(new TDengineParameter("@11", null));
                parameters.Add(new TDengineParameter("@12", null));
                parameters.Add(new TDengineParameter("@13", null));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                cmd.CommandText =
                    "select * from test_types where ts <= ? order by ts desc limit 2";
                parameters.Add(new TDengineParameter("@0", nextSecond.AddSeconds(1)));
                var reader = cmd.ExecuteReader();
                parameters.Clear();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal((now.ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) * 100,
                    (reader.GetDateTime(0).ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) * 100);
                Assert.Equal(now, reader.GetDateTime(0));
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists test_nano";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }

        [Fact]
        public void WSCommonExec()
        {
            using (var cmd = _wsConnection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists ws_test_common";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database ws_test_common";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _wsConnection.ChangeDatabase("ws_test_common");
                cmd.CommandText =
                    "create table if not exists test_types(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into test_types values(now+1s, null, null, null, null, null, null, null, null, null,null,null,null,null)," +
                    "(now, 1, 2, 3000000.3, 400000000.4, '5binary', 6, 7, true, '9nchar',10,11,12,13)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(2, affected);
                cmd.CommandText =
                    "select ts, f_int, f_bigint, f_float, f_double, f_binary, f_smallint, f_tinyint, f_bool, f_nchar,f_uint,f_ubigint,f_usmallint,f_utinyint from test_types order by ts desc limit 2";
                var reader = cmd.ExecuteReader();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists ws_test_common";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }

        [Fact]
        public void WSStatement()
        {
            using (var cmd = _wsConnection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists ws_test_stmt";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database ws_test_stmt";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _wsConnection.ChangeDatabase("ws_test_stmt");
                cmd.CommandText =
                    "create table if not exists test_stb(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned) tags(val int)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into ? using test_stb tags (?) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?,?,?,?)";
                var now = DateTime.Now;
                DateTime nextSecond = now.AddSeconds(1);
                var parameters = cmd.Parameters;
                // table name;
                parameters.Add(new TDengineParameter("#table", "test_types"));
                // tag name;
                parameters.Add(new TDengineParameter("$0", "123"));
                // value
                parameters.Add(new TDengineParameter("@0", now));
                parameters.Add(new TDengineParameter("@1", (int)1));
                parameters.Add(new TDengineParameter("@2", (long)2));
                parameters.Add(new TDengineParameter("@3", (float)3000000.3));
                parameters.Add(new TDengineParameter("@4", (double)400000000.4));
                parameters.Add(new TDengineParameter("@5", "5binary"));
                parameters.Add(new TDengineParameter("@6", (short)6));
                parameters.Add(new TDengineParameter("@7", (sbyte)7));
                parameters.Add(new TDengineParameter("@8", true));
                parameters.Add(new TDengineParameter("@9", "9nchar"));
                parameters.Add(new TDengineParameter("@10", (uint)10));
                parameters.Add(new TDengineParameter("@11", (ulong)11));
                parameters.Add(new TDengineParameter("@12", (ushort)12));
                parameters.Add(new TDengineParameter("@13", (byte)13));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                parameters.Add(new TDengineParameter("#table", "test_types"));
                parameters.Add(new TDengineParameter("$0", "123"));
                parameters.Add(new TDengineParameter("@0", nextSecond));
                parameters.Add(new TDengineParameter("@1", null));
                parameters.Add(new TDengineParameter("@2", null));
                parameters.Add(new TDengineParameter("@3", null));
                parameters.Add(new TDengineParameter("@4", null));
                parameters.Add(new TDengineParameter("@5", null));
                parameters.Add(new TDengineParameter("@6", null));
                parameters.Add(new TDengineParameter("@7", null));
                parameters.Add(new TDengineParameter("@8", null));
                parameters.Add(new TDengineParameter("@9", null));
                parameters.Add(new TDengineParameter("@10", null));
                parameters.Add(new TDengineParameter("@11", null));
                parameters.Add(new TDengineParameter("@12", null));
                parameters.Add(new TDengineParameter("@13", null));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                cmd.CommandText =
                    "select * from test_types where ts <= ? order by ts desc limit 2";
                parameters.Add(new TDengineParameter("@0", nextSecond.AddSeconds(1)));
                var reader = cmd.ExecuteReader();
                parameters.Clear();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal((now.ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) / 10000,
                    (reader.GetDateTime(0).ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) / 10000);
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists ws_test_stmt";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }

        [Fact]
        public void WSStatementNano()
        {
            using (var cmd = _wsConnection.CreateCommand())
            {
                cmd.CommandText = "drop database if exists ws_test_nano";
                var affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText = "create database ws_test_nano precision 'ns'";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                _wsConnection.ChangeDatabase("ws_test_nano");
                cmd.CommandText =
                    "create table if not exists test_stb(" +
                    "ts timestamp, f_int int, f_bigint bigint, f_float float, f_double double, f_binary binary(16), " +
                    "f_smallint smallint, f_tinyint tinyint, f_bool bool, f_nchar nchar(16), " +
                    "f_uint int unsigned,f_ubigint bigint unsigned, f_usmallint smallint unsigned,f_utinyint tinyint unsigned) tags(val int)";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
                cmd.CommandText =
                    "insert into ? using test_stb tags (?) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?,?,?,?)";
                var now = DateTime.Now;
                DateTime nextSecond = now.AddSeconds(1);
                var parameters = cmd.Parameters;
                // table name;
                parameters.Add(new TDengineParameter("#table", "test_types"));
                // tag name;
                parameters.Add(new TDengineParameter("$0", "123"));
                // value
                parameters.Add(new TDengineParameter("@0", now));
                parameters.Add(new TDengineParameter("@1", (int)1));
                parameters.Add(new TDengineParameter("@2", (long)2));
                parameters.Add(new TDengineParameter("@3", (float)3000000.3));
                parameters.Add(new TDengineParameter("@4", (double)400000000.4));
                parameters.Add(new TDengineParameter("@5", "5binary"));
                parameters.Add(new TDengineParameter("@6", (short)6));
                parameters.Add(new TDengineParameter("@7", (sbyte)7));
                parameters.Add(new TDengineParameter("@8", true));
                parameters.Add(new TDengineParameter("@9", "9nchar"));
                parameters.Add(new TDengineParameter("@10", (uint)10));
                parameters.Add(new TDengineParameter("@11", (ulong)11));
                parameters.Add(new TDengineParameter("@12", (ushort)12));
                parameters.Add(new TDengineParameter("@13", (byte)13));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                parameters.Add(new TDengineParameter("#table", "test_types"));
                parameters.Add(new TDengineParameter("$0", "123"));
                parameters.Add(new TDengineParameter("@0", nextSecond));
                parameters.Add(new TDengineParameter("@1", null));
                parameters.Add(new TDengineParameter("@2", null));
                parameters.Add(new TDengineParameter("@3", null));
                parameters.Add(new TDengineParameter("@4", null));
                parameters.Add(new TDengineParameter("@5", null));
                parameters.Add(new TDengineParameter("@6", null));
                parameters.Add(new TDengineParameter("@7", null));
                parameters.Add(new TDengineParameter("@8", null));
                parameters.Add(new TDengineParameter("@9", null));
                parameters.Add(new TDengineParameter("@10", null));
                parameters.Add(new TDengineParameter("@11", null));
                parameters.Add(new TDengineParameter("@12", null));
                parameters.Add(new TDengineParameter("@13", null));
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(1, affected);
                parameters.Clear();
                cmd.CommandText =
                    "select * from test_types where ts <= ? order by ts desc limit 2";
                parameters.Add(new TDengineParameter("@0", nextSecond.AddSeconds(1)));
                var reader = cmd.ExecuteReader();
                parameters.Clear();
                reader.Read();
                Assert.False(reader.IsDBNull(0));
                for (int i = 1; i < 14; i++)
                {
                    Assert.True(reader.IsDBNull(i));
                    Assert.Null(reader.GetValue(i));
                }

                reader.Read();
                Assert.False(reader.IsDBNull(0));
                Assert.Equal((now.ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) * 100,
                    (reader.GetDateTime(0).ToUniversalTime().Ticks - TDengineConstant.TimeZero.Ticks) * 100);
                Assert.Equal(now, reader.GetDateTime(0));
                Assert.Equal(14, reader.FieldCount);
                Assert.Equal((int)1, reader.GetValue(1));
                Assert.Equal((int)1, reader.GetInt32(1));

                Assert.Equal((long)2, reader.GetValue(2));
                Assert.Equal((long)2, reader.GetInt64(2));

                Assert.Equal((float)3000000.3, reader.GetValue(3));
                Assert.Equal((float)3000000.3, reader.GetFloat(3));

                Assert.Equal((double)400000000.4, reader.GetValue(4));
                Assert.Equal((double)400000000.4, reader.GetDouble(4));

                Assert.Equal(Encoding.UTF8.GetBytes("5binary"), reader.GetValue(5));
                Assert.Equal("5binary", reader.GetString(5));

                Assert.Equal((short)6, reader.GetValue(6));
                Assert.Equal((short)6, reader.GetInt16(6));

                Assert.Equal((sbyte)7, reader.GetValue(7));
                Assert.Equal((sbyte)7, (sbyte)reader.GetByte(7));

                Assert.Equal(true, reader.GetValue(8));
                Assert.True(reader.GetBoolean(8));

                Assert.Equal("9nchar", reader.GetValue(9));
                Assert.Equal("9nchar", reader.GetString(9));

                Assert.Equal((uint)10, reader.GetValue(10));
                Assert.Equal((uint)10, (uint)reader.GetInt32(10));

                Assert.Equal((ulong)11, reader.GetValue(11));
                Assert.Equal((ulong)11, (ulong)reader.GetInt64(11));

                Assert.Equal((ushort)12, reader.GetValue(12));
                Assert.Equal((ushort)12, (ushort)reader.GetInt16(12));

                Assert.Equal((byte)13, reader.GetValue(13));
                Assert.Equal((byte)13, reader.GetByte(13));

                reader.Close();
                cmd.CommandText = "drop database if exists ws_test_nano";
                affected = cmd.ExecuteNonQuery();
                Assert.Equal(0, affected);
            }
        }
    }
}