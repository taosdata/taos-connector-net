using System;
using TDengine.Driver;
using TDengine.Driver.Client;

namespace GeometryStmt
{
    internal class StmtInsertExample
    {
        public static void Main(string[] args)
        {
            // ws connection string
            var connectionString = "protocol=WebSocket;host=127.0.0.1;port=6041;useSSL=false;username=root;password=taosdata";

            // natives connection string
            // var connectionString = "protocol=Native;host=127.0.0.1;port=6030;username=root;password=taosdata";

            try
            {
                var builder = new ConnectionStringBuilder(connectionString);
                using (var client = DbDriver.Open(builder))
                {
                    // create database
                    client.Exec("CREATE DATABASE IF NOT EXISTS example_geometry_stmt");
                    // use database
                    client.Exec("USE example_geometry_stmt");
                    // create table
                    client.Exec(
                        "CREATE TABLE IF NOT EXISTS ntb (ts TIMESTAMP, val GEOMETRY(100))");
                    using (var stmt = client.StmtInit())
                    {
                        String sql = "INSERT INTO ntb VALUES (?,?)";
                        stmt.Prepare(sql);

                        var current = DateTime.Now;
                        // bind rows

                        stmt.BindRow(new object[]
                        {
                            current,
                            new byte[]
                            {
                                0x01, 0x01, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x59,
                                0x40, 0x00, 0x00, 0x00,
                                0x00, 0x00, 0x00, 0x59, 0x40
                            },
                        });

                        // add batch
                        stmt.AddBatch();
                        // execute
                        stmt.Exec();
                        // get affected rows
                        var affectedRows = stmt.Affected();
                        Console.WriteLine($"Successfully inserted {affectedRows} rows to example_geometry_stmt.ntb");
                    }
                }
            }
            catch (TDengineError e)
            {
                // handle TDengine error
                Console.WriteLine("Failed to insert to table meters using stmt, ErrCode: " + e.Code + ", ErrMessage: " +
                                  e.Error);
                throw;
            }
            catch (Exception e)
            {
                // handle other exceptions
                Console.WriteLine("Failed to insert to table meters using stmt, ErrMessage: " + e.Message);
                throw;
            }
        }
    }
}