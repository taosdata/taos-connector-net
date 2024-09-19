using System;
using System.Text;
using TDengine.Driver;
using TDengine.Driver.Client;

namespace AllTypeQuery
{
    public class Example
    {
        public static void Main(string[] args)
        {
            try
            {
                // ws connection string
                var connectionString = "protocol=WebSocket;host=127.0.0.1;port=6041;useSSL=false;username=root;password=taosdata";

                // native connection string
                // var connectionString = "protocol=Native;host=127.0.0.1;port=6030;username=root;password=taosdata";

                var builder = new ConnectionStringBuilder(connectionString);
                using (var client = DbDriver.Open(builder))
                {
                    // create database
                    var affected = client.Exec("CREATE DATABASE IF NOT EXISTS example_all_type_query");
                    Console.WriteLine($"Create database example_all_type_query successfully, rowsAffected: {affected}");
                    // create table
                    affected = client.Exec(
                        "CREATE STABLE IF NOT EXISTS example_all_type_query.stb (" +
                        "ts TIMESTAMP, " +
                        "int_col INT, " +
                        "double_col DOUBLE, " +
                        "bool_col BOOL, " +
                        "binary_col BINARY(100), " +
                        "nchar_col NCHAR(100), " +
                        "varbinary_col VARBINARY(100), " +
                        "geometry_col GEOMETRY(100)) " +
                        "tags (json_tag json)");
                    Console.WriteLine(
                        $"Create stable example_all_type_query.stb successfully, rowsAffected: {affected}");
                    var insertQuery =
                        "INSERT INTO example_all_type_query.ntb using example_all_type_query.stb tags('{\"device\":\"device_1\"}') " +
                        "values(now, 1, 1.1, true, 'binary_value', 'nchar_value', '\\x98f46e', 'POINT(100 100)')";
                    var affectedRows = client.Exec(insertQuery);
                    Console.WriteLine("Successfully inserted " + affectedRows +
                                      " rows to example_all_type_query.ntb.");
                    var query = "SELECT * FROM example_all_type_query.stb";

                    using (var rows = client.Query(query))
                    {
                        while (rows.Read())
                        {
                            // Add your data processing logic here
                            var ts = (DateTime)rows.GetValue(0);
                            var intVal = (int)rows.GetValue(1);
                            var doubleVal = (double)rows.GetValue(2);
                            var boolVal = (bool)rows.GetValue(3);
                            var binaryVal = (byte[])rows.GetValue(4);
                            var ncharVal = (string)rows.GetValue(5);
                            var varbinaryVal = (byte[])rows.GetValue(6);
                            var geometryVal = (byte[])rows.GetValue(7);
                            var jsonVal = (byte[])rows.GetValue(8);
                            Console.WriteLine(
                                $"ts: {ts:yyyy-MM-dd HH:mm:ss.fff}, " +
                                $"int_col: {intVal}, " +
                                $"double_col: {doubleVal}, " +
                                $"bool_col: {boolVal}, " +
                                $"binary_col: {Encoding.UTF8.GetString(binaryVal)}, " +
                                $"nchar_col: {ncharVal}, " +
                                $"varbinary_col: {BitConverter.ToString(varbinaryVal)}, " +
                                $"geometry_col: {BitConverter.ToString(geometryVal)}, " +
                                $"json_tag: {Encoding.UTF8.GetString(jsonVal)}"
                            );
                        }
                    }
                }
            }
            catch (TDengineError e)
            {
                // handle TDengine error
                Console.WriteLine(e.Message);
                throw;
            }
            catch (Exception e)
            {
                // handle other exceptions
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}