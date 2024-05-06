using TDengine.Data.Client;
using TDengine.Driver;
using Xunit;

namespace Data.Tests
{
    public class TDengineConnectionStringBuilderTests
    {
        [Fact]
        public void DefaultNative_ShouldSetDefaultValues()
        {
            var builder = new TDengineConnectionStringBuilder("");

            builder.DefaultNative();

            Assert.Equal(6030, builder.Port);
            Assert.Equal("localhost", builder.Host);
            Assert.Equal(TDengineConstant.ProtocolNative, builder.Protocol);
        }

        [Fact]
        public void DefaultWebSocket_ShouldSetDefaultValues()
        {
            var builder = new TDengineConnectionStringBuilder("");

            builder.DefaultWebSocket();

            Assert.Equal(6041, builder.Port);
            Assert.Equal("localhost", builder.Host);
            Assert.Equal(TDengineConstant.ProtocolWebSocket, builder.Protocol);
        }

        [Fact]
        public void Parse()
        {
            var builder =
                new TDengineConnectionStringBuilder(
                    "host=127.0.0.1;port=6030;username=root;password=taosdata;protocol=Native;db=test");
            Assert.Equal("127.0.0.1", builder.Host);
            Assert.Equal(6030, builder.Port);
            Assert.Equal("root", builder.Username);
            Assert.Equal("taosdata", builder.Password);
            Assert.Equal("test", builder.Database);
            Assert.Equal(TDengineConstant.ProtocolNative, builder.Protocol);
            builder.Clear();
            Assert.Equal(string.Empty, builder.Host);
            Assert.Equal(0, builder.Port);
            Assert.Equal(string.Empty, builder.Username);
            Assert.Equal(string.Empty, builder.Password);
            Assert.Equal(string.Empty, builder.Database);
            Assert.Equal(TDengineConstant.ProtocolNative, builder.Protocol);
            builder.Database = "test2";
            Assert.Equal("test2", builder.Database);
            builder.Remove("db");
            Assert.Equal(string.Empty, builder.Database);
        }

        [Fact]
        public void ParseWebSocket()
        {
            var builder = new TDengineConnectionStringBuilder(
                "host=127.0.0.1;" +
                "port=6041;" +
                "username=root;" +
                "password=taosdata;" +
                "protocol=WebSocket;" +
                "db=test;" +
                "enableCompression=true;" +
                "connTimeout=00:00:10;" +
                "readTimeout=00:00:20;" +
                "writeTimeout=00:00:30;" +
                "timezone=UTC;" +
                "useSSL=true;" +
                "token=123456;" +
                "autoReconnect=true;" +
                "reconnectIntervalMs=10;" +
                "reconnectRetryCount=5");
            Assert.Equal("127.0.0.1", builder.Host);
            Assert.Equal(6041, builder.Port);
            Assert.Equal("root", builder.Username);
            Assert.Equal("taosdata", builder.Password);
            Assert.Equal("test", builder.Database);
            Assert.Equal(TDengineConstant.ProtocolWebSocket, builder.Protocol);
            Assert.True(builder.EnableCompression);
            Assert.Equal(10, builder.ConnTimeout.TotalSeconds);
            Assert.Equal(20, builder.ReadTimeout.TotalSeconds);
            Assert.Equal(30, builder.WriteTimeout.TotalSeconds);
            Assert.Equal("UTC", builder.Timezone.Id);
            Assert.True(builder.UseSSL);
            Assert.Equal("123456", builder.Token);
            Assert.True(builder.AutoReconnect);
            Assert.Equal(10, builder.ReconnectIntervalMs);
            Assert.Equal(5, builder.ReconnectRetryCount);
        }
    }
}