using System;
using Xunit;
using TDengine.Data.Client;
using TDengine.Driver;

namespace Data.Tests
{
    public class TDenginePrepareCommandTests
    {
        [Fact]
        public void PrepareCommandTest()
        {
            using var command = new TDengineCommand();

            var connection =
                new TDengineConnection(
                    "host=localhost;port=6030;username=root;password=taosdata;protocol=Native;db=test;");
            connection.Open();

            command.CommandText = "select * from `test`.`test_table01` ";
            command.Connection = connection;

            var dbDataReader = command.ExecuteReader();

            Assert.True(dbDataReader.HasRows);
        }

        
        [Fact]
        public void ConnectionInitCommandTest()
        {
            var connection =
                new TDengineConnection(
                    "host=localhost;port=6030;username=root;password=taosdata;protocol=Native;db=test;");
            connection.Open();
            using var command = new TDengineCommand(connection);
            command.CommandText = "select * from `test`.`test_table01` ";
            command.Connection = connection;

            var dbDataReader = command.ExecuteReader();

            Assert.True(dbDataReader.HasRows);
        }
    }
}