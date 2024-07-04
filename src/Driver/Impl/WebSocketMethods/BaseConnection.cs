using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;

namespace TDengine.Driver.Impl.WebSocketMethods
{
    public class BaseConnection
    {
        private readonly ClientWebSocket _client;

        private readonly TimeSpan _readTimeout;
        private readonly TimeSpan _writeTimeout;

        private ulong _reqId;
        private readonly TimeSpan _defaultConnTimeout = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _defaultReadTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _defaultWriteTimeout = TimeSpan.FromSeconds(10);

        protected BaseConnection(string addr, TimeSpan connectTimeout = default,
            TimeSpan readTimeout = default, TimeSpan writeTimeout = default, bool enableCompression = false)
        {
            _client = new ClientWebSocket();
            _client.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
#if NET6_0_OR_GREATER
            if (enableCompression)
            {
                _client.Options.DangerousDeflateOptions = new WebSocketDeflateOptions()
                {
                    ClientMaxWindowBits = 15, // Default value
                    ServerMaxWindowBits = 15, // Default value
                    ClientContextTakeover = true, // Default value
                    ServerContextTakeover = true // Default value
                };
            }
#endif
            if (connectTimeout == default)
            {
                connectTimeout = _defaultConnTimeout;
            }

            if (readTimeout == default)
            {
                readTimeout = _defaultReadTimeout;
            }

            if (writeTimeout == default)
            {
                writeTimeout = _defaultWriteTimeout;
            }

            var connTimeout = connectTimeout;
            _readTimeout = readTimeout;
            _writeTimeout = writeTimeout;
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(connTimeout);
                _client.ConnectAsync(new Uri(addr), cts.Token).Wait(connTimeout);
            }

            if (_client.State != WebSocketState.Open)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_CONNEC_FAILED,
                    $"connect to {addr} fail");
            }
        }

        protected ulong _GetReqId()
        {
            _reqId += 1;
            return _reqId;
        }


        protected static void WriteUInt64ToBytes(byte[] byteArray, ulong value, int offset)
        {
            byteArray[offset + 0] = (byte)value;
            byteArray[offset + 1] = (byte)(value >> 8);
            byteArray[offset + 2] = (byte)(value >> 16);
            byteArray[offset + 3] = (byte)(value >> 24);
            byteArray[offset + 4] = (byte)(value >> 32);
            byteArray[offset + 5] = (byte)(value >> 40);
            byteArray[offset + 6] = (byte)(value >> 48);
            byteArray[offset + 7] = (byte)(value >> 56);
        }

        protected static void WriteUInt32ToBytes(byte[] byteArray, UInt32 value, int offset)
        {
            byteArray[offset + 0] = (byte)value;
            byteArray[offset + 1] = (byte)(value >> 8);
            byteArray[offset + 2] = (byte)(value >> 16);
            byteArray[offset + 3] = (byte)(value >> 24);
        }

        protected static void WriteUInt16ToBytes(byte[] byteArray, UInt16 value, int offset)
        {
            byteArray[offset + 0] = (byte)value;
            byteArray[offset + 1] = (byte)(value >> 8);
        }

        protected byte[] SendBinaryBackBytes(byte[] request)
        {
            SendBinary(request);
            var respBytes = Receive(out var messageType);
            if (messageType == WebSocketMessageType.Binary)
            {
                return respBytes;
            }

            var resp = JsonConvert.DeserializeObject<IWSBaseResp>(Encoding.UTF8.GetString(respBytes));
            throw new TDengineError(resp.Code, resp.Message, request, Encoding.UTF8.GetString(respBytes));
        }


        protected T SendBinaryBackJson<T>(byte[] request) where T : IWSBaseResp
        {
            SendBinary(request);
            var respBytes = Receive(out var messageType);
            if (messageType != WebSocketMessageType.Text)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected binary message");
            }

            var resp = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(respBytes));
            if (resp.Code == 0) return resp;
            throw new TDengineError(resp.Code, resp.Message);
        }

        protected T2 SendJsonBackJson<T1, T2>(string action, T1 req) where T2 : IWSBaseResp
        {
            var reqStr = SendJson(action, req);
            var respBytes = Receive(out var messageType);
            if (messageType != WebSocketMessageType.Text)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected binary message", respBytes, reqStr);
            }

            var resp = JsonConvert.DeserializeObject<T2>(Encoding.UTF8.GetString(respBytes));
            // Console.WriteLine(Encoding.UTF8.GetString(respBytes));
            if (resp.Action != action)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    $"receive unexpected action {resp.Action},req:{reqStr}",
                    Encoding.UTF8.GetString(respBytes));
            }

            if (resp.Code == 0) return resp;
            throw new TDengineError(resp.Code, resp.Message);
        }

        protected byte[] SendJsonBackBytes<T>(string action, T req)
        {
            var reqStr = SendJson(action, req);
            var respBytes = Receive(out var messageType);
            if (messageType == WebSocketMessageType.Binary)
            {
                return respBytes;
            }

            var resp = JsonConvert.DeserializeObject<IWSBaseResp>(Encoding.UTF8.GetString(respBytes));
            throw new TDengineError(resp.Code, resp.Message, reqStr);
        }

        protected string SendJson<T>(string action, T req)
        {
            var request = JsonConvert.SerializeObject(new WSActionReq<T>
            {
                Action = action,
                Args = req
            });
            SendText(request);
            return request;
        }

        private async Task SendAsync(ArraySegment<byte> data, WebSocketMessageType messageType)
        {
            if (!IsAvailable())
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_CONNECTION_CLOSED,
                    "websocket connection is closed");
            }

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(_writeTimeout);
                try
                {
                    await _client.SendAsync(data, messageType, true, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw new TDengineError((int)TDengineError.InternalErrorCode.WS_WRITE_TIMEOUT,
                        "write message timeout");
                }
            }
        }

        private void SendText(string request)
        {
            var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(request));
            Task.Run(async () => { await SendAsync(data, WebSocketMessageType.Text).ConfigureAwait(true); }).Wait();
        }

        private void SendBinary(byte[] request)
        {
            var data = new ArraySegment<byte>(request);
            Task.Run(async () => { await SendAsync(data, WebSocketMessageType.Binary).ConfigureAwait(true); }).Wait();
        }

        private byte[] Receive(out WebSocketMessageType messageType)
        {
            var task = Task.Run(async () => await ReceiveAsync().ConfigureAwait(true));
            task.Wait();
            messageType = task.Result.Item2;
            return task.Result.Item1;
        }

        private async Task<Tuple<byte[], WebSocketMessageType>> ReceiveAsync()
        {
            if (!IsAvailable())
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_CONNECTION_CLOSED,
                    "websocket connection is closed");
            }

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(_readTimeout);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    int bufferSize = 1024 * 4;
                    byte[] buffer = new byte[bufferSize];
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token)
                            .ConfigureAwait(false);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _client
                                .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                                .ConfigureAwait(false);
                            throw new TDengineError((int)TDengineError.InternalErrorCode.WS_RECEIVE_CLOSE_FRAME,
                                "receive websocket close frame");
                        }

                        memoryStream.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    return Tuple.Create(memoryStream.ToArray(), result.MessageType);
                }
            }
        }

        public void Close()
        {
            try
            {
                _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public bool IsAvailable(Exception e = null)
        {
            if (_client.State != WebSocketState.Open)
                return false;

            switch (e)
            {
                case null:
                    return true;
                case WebSocketException _:
                    return false;
                case AggregateException ae:
                    return !(ae.InnerException is WebSocketException);
                case TDengineError te:
                    return te.Code != (int)TDengineError.InternalErrorCode.WS_CONNECTION_CLOSED &&
                           te.Code != (int)TDengineError.InternalErrorCode.WS_RECEIVE_CLOSE_FRAME &&
                           te.Code != (int)TDengineError.InternalErrorCode.WS_WRITE_TIMEOUT;
                default:
                    return true;
            }
        }
    }
}