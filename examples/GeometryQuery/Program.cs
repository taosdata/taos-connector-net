using System;
using System.Text;
using TDengine.Driver;
using TDengine.Driver.Client;

namespace GeometryQuery
{
    public class GeometryExample
    {
        public static void Main(string[] args)
        {
            try
            {
                // ws connection string
                // var connectionString = "protocol=WebSocket;host=127.0.0.1;port=6041;useSSL=false;username=root;password=taosdata";
                
                // native connection string
                var connectionString = "protocol=Native;host=127.0.0.1;port=6030;username=root;password=taosdata";
                
                var builder = new ConnectionStringBuilder(connectionString);
                using (var client = DbDriver.Open(builder))
                {
                    // create database
                    var affected = client.Exec("CREATE DATABASE IF NOT EXISTS example_geometry_query");
                    Console.WriteLine($"Create database example_geometry_query successfully, rowsAffected: {affected}");
                    // create table
                    affected = client.Exec(
                        "CREATE TABLE IF NOT EXISTS example_geometry_query.ntb (ts TIMESTAMP, val GEOMETRY(100))");
                    Console.WriteLine(
                        $"Create table example_geometry_query.ntb successfully, rowsAffected: {affected}");
                    var insertQuery = "INSERT INTO example_geometry_query.ntb values(now, 'POINT(100 100)')";
                    var affectedRows = client.Exec(insertQuery);
                    Console.WriteLine("Successfully inserted " + affectedRows +
                                      " rows to example_geometry_query.ntb.");
                    var query = "SELECT ts, val FROM example_geometry_query.ntb";

                    using (var rows = client.Query(query))
                    {
                        while (rows.Read())
                        {
                            // Add your data processing logic here
                            var ts = (DateTime)rows.GetValue(0);
                            var val = (byte[])rows.GetValue(1);
                            Console.WriteLine(
                                $"ts: {ts:yyyy-MM-dd HH:mm:ss.fff}, val: {BitConverter.ToString(val)}");
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