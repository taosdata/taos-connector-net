﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;

namespace TDengine.Driver.Impl.WebSocketMethods
{
    internal class WsMessage
    {
        internal readonly byte[] Message;
        internal readonly WebSocketMessageType MessageType;
        internal readonly Exception Exception;

        internal WsMessage(byte[] message, WebSocketMessageType messageType, Exception exception)
        {
            Message = message;
            MessageType = messageType;
            Exception = exception;
        }
    }

    public class BaseConnection
    {
        private readonly ClientWebSocket _client;

        private readonly TimeSpan _readTimeout;
        private readonly TimeSpan _writeTimeout;

        private readonly TimeSpan _defaultConnTimeout = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _defaultReadTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _defaultWriteTimeout = TimeSpan.FromSeconds(10);

        private readonly ConcurrentDictionary<ulong, TaskCompletionSource<WsMessage>> _pendingRequests =
            new ConcurrentDictionary<ulong, TaskCompletionSource<WsMessage>>();

        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private bool _exit = false;
        private readonly ReaderWriterLockSlim _exitLock = new ReaderWriterLockSlim();
        private bool _isManualShutdown = false;
        private readonly ReaderWriterLockSlim _shutdownLock = new ReaderWriterLockSlim();
        private int _isShutdown = 0;

        public bool IsManualShutdown
        {
            get
            {
                _shutdownLock.EnterReadLock();
                try
                {
                    return _isManualShutdown;
                }
                finally
                {
                    _shutdownLock.ExitReadLock();
                }
            }
            private set
            {
                _shutdownLock.EnterWriteLock();
                try
                {
                    _isManualShutdown = value;
                }
                finally
                {
                    _shutdownLock.ExitWriteLock();
                }
            }
        }

        private bool IsExit
        {
            get
            {
                _exitLock.EnterReadLock();
                try
                {
                    return _exit;
                }
                finally
                {
                    _exitLock.ExitReadLock();
                }
            }
        }

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

            Task.Run(async () => { await ReceiveLoop(); });
        }

        protected static ulong _GetReqId()
        {
            return (ulong)ReqId.GetReqId();
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

        protected static void WriteUInt32ToBytes(byte[] byteArray, uint value, int offset)
        {
            byteArray[offset + 0] = (byte)value;
            byteArray[offset + 1] = (byte)(value >> 8);
            byteArray[offset + 2] = (byte)(value >> 16);
            byteArray[offset + 3] = (byte)(value >> 24);
        }

        protected static void WriteUInt16ToBytes(byte[] byteArray, ushort value, int offset)
        {
            byteArray[offset + 0] = (byte)value;
            byteArray[offset + 1] = (byte)(value >> 8);
        }


        protected byte[] SendBinaryBackBytes(byte[] request, ulong reqId)
        {
            var task = Task.Run(async () => await AsyncSendBinaryBackByte(request, reqId));
            task.Wait();
            return task.Result;
        }

        private async Task<byte[]> AsyncSendBinaryBackByte(byte[] request, ulong reqId)
        {
            var tcs = AddTask(reqId);
            // send request
            try
            {
                await AsyncSendBinary(request);
            }
            catch (Exception)
            {
                _pendingRequests.TryRemove(reqId, out _);
                throw;
            }

            // wait for timeout
            var timeoutTask = Task.Delay(_readTimeout);
            // wait for response
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            // timeout
            if (completedTask == timeoutTask)
            {
                if (_pendingRequests.TryRemove(reqId, out var removedTcs))
                {
                    removedTcs.TrySetCanceled();
                }

                throw new TimeoutException("Request timed out. reqId:" + reqId);
            }

            // get response
            var responseMessage = await tcs.Task;
            if (responseMessage.Exception != null)
            {
                throw responseMessage.Exception;
            }

            var respBytes = responseMessage.Message;
            var messageType = responseMessage.MessageType;
            if (messageType == WebSocketMessageType.Binary)
            {
                return respBytes;
            }

            WSBaseResp resp;
            try
            {
                resp = JsonConvert.DeserializeObject<WSBaseResp>(Encoding.UTF8.GetString(respBytes));
            }
            catch (Exception e)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected message", e.Message);
            }

            throw new TDengineError(resp.Code, resp.Message, request, Encoding.UTF8.GetString(respBytes));
        }


        protected T SendBinaryBackJson<T>(byte[] request, ulong reqId) where T : IWSBaseResp
        {
            var task = Task.Run(async () => await AsyncSendBinaryBackJson<T>(request, reqId));
            task.Wait();
            return task.Result;
        }

        private async Task<T> AsyncSendBinaryBackJson<T>(byte[] request, ulong reqId) where T : IWSBaseResp
        {
            var tcs = AddTask(reqId);
            // send request
            try
            {
                await AsyncSendBinary(request);
            }
            catch (Exception)
            {
                _pendingRequests.TryRemove(reqId, out _);
                throw;
            }

            // wait for timeout
            var timeoutTask = Task.Delay(_readTimeout);
            // wait for response
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            // timeout
            if (completedTask == timeoutTask)
            {
                if (_pendingRequests.TryRemove(reqId, out var removedTcs))
                {
                    removedTcs.TrySetCanceled();
                }

                throw new TimeoutException("Request timed out.");
            }

            // get response
            var responseMessage = await tcs.Task;
            if (responseMessage.Exception != null)
            {
                throw responseMessage.Exception;
            }

            var respBytes = responseMessage.Message;
            var messageType = responseMessage.MessageType;
            if (messageType != WebSocketMessageType.Text)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected binary message");
            }

            var resp = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(respBytes));
            if (resp.Code == 0) return resp;
            throw new TDengineError(resp.Code, resp.Message);
        }

        protected T2 SendJsonBackJson<T1, T2>(string action, T1 req, ulong reqId) where T2 : IWSBaseResp
        {
            var task = Task.Run(async () => await AsyncSendJsonBackJson<T1, T2>(action, req, reqId));
            task.Wait();
            return task.Result;
        }

        private async Task<T2> AsyncSendJsonBackJson<T1, T2>(string action, T1 req, ulong reqId) where T2 : IWSBaseResp
        {
            var tcs = AddTask(reqId);
            // send request
            string reqStr;
            try
            {
                reqStr = await AsyncSendJson(action, req);
            }
            catch (Exception)
            {
                _pendingRequests.TryRemove(reqId, out _);
                throw;
            }

            // wait for timeout
            var timeoutTask = Task.Delay(_readTimeout);
            // wait for response
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            // timeout
            if (completedTask == timeoutTask)
            {
                if (_pendingRequests.TryRemove(reqId, out var removedTcs))
                {
                    removedTcs.TrySetCanceled();
                }

                throw new TimeoutException("Request timed out.");
            }

            // get response
            var responseMessage = await tcs.Task;
            if (responseMessage.Exception != null)
            {
                throw responseMessage.Exception;
            }

            var respBytes = responseMessage.Message;
            var messageType = responseMessage.MessageType;
            if (messageType != WebSocketMessageType.Text)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected binary message", respBytes, reqStr);
            }

            T2 resp;
            try
            {
                resp = JsonConvert.DeserializeObject<T2>(Encoding.UTF8.GetString(respBytes));
            }
            catch (Exception e)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected message", "req:" + reqStr + ";resp:" + Encoding.UTF8.GetString(respBytes));
            }

            if (resp.Action != action)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    $"receive unexpected action {resp.Action},req:{reqStr}",
                    Encoding.UTF8.GetString(respBytes));
            }

            if (resp.Code == 0) return resp;
            throw new TDengineError(resp.Code, resp.Message);
        }


        protected byte[] SendJsonBackBytes<T>(string action, T req, ulong reqId)
        {
            var task = Task.Run(async () => await AsyncSendJsonBackBytes(action, req, reqId));
            task.Wait();
            return task.Result;
        }

        private async Task<byte[]> AsyncSendJsonBackBytes<T>(string action, T req, ulong reqId)
        {
            var tcs = AddTask(reqId);
            // send request
            string reqStr;
            try
            {
                reqStr = await AsyncSendJson(action, req);
            }
            catch (Exception)
            {
                _pendingRequests.TryRemove(reqId, out _);
                throw;
            }

            // wait for timeout
            var timeoutTask = Task.Delay(_readTimeout);
            // wait for response
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            // timeout
            if (completedTask == timeoutTask)
            {
                if (_pendingRequests.TryRemove(reqId, out var removedTcs))
                {
                    removedTcs.TrySetCanceled();
                }

                throw new TimeoutException("Request timed out.");
            }

            // get response
            var responseMessage = await tcs.Task;
            if (responseMessage.Exception != null)
            {
                throw responseMessage.Exception;
            }

            var respBytes = responseMessage.Message;
            var messageType = responseMessage.MessageType;
            if (messageType == WebSocketMessageType.Binary)
            {
                return respBytes;
            }

            WSBaseResp resp;
            try
            {
                resp = JsonConvert.DeserializeObject<WSBaseResp>(Encoding.UTF8.GetString(respBytes));
            }
            catch (Exception)
            {
                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                    "receive unexpected message", "req:" + reqStr + ";resp:" + Encoding.UTF8.GetString(respBytes));
            }

            throw new TDengineError(resp.Code, resp.Message, Encoding.UTF8.GetString(respBytes));
        }

        protected string SendJson<T>(string action, T req, ulong reqId)
        {
            var task = Task.Run(async () => await AsyncSendJson(action, req));
            task.Wait();
            return task.Result;
        }

        private TaskCompletionSource<WsMessage> AddTask(ulong reqId)
        {
            _exitLock.EnterReadLock();
            try
            {
                if (_exit)
                {
                    throw new TDengineError((int)TDengineError.InternalErrorCode.WS_CONNECTION_CLOSED,
                        "websocket connection is closed");
                }

                var tcs = new TaskCompletionSource<WsMessage>();
                if (!_pendingRequests.TryAdd(reqId, tcs))
                {
                    throw new InvalidOperationException($"Request with reqId '{reqId}' already exists.");
                }

                return tcs;
            }
            finally
            {
                _exitLock.ExitReadLock();
            }
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

        private async Task<string> AsyncSendJson<T>(string action, T req)
        {
            var request = JsonConvert.SerializeObject(new WSActionReq<T>
            {
                Action = action,
                Args = req
            });
            await AsyncSendText(request);
            return request;
        }

        private async Task AsyncSendText(string request)
        {
            await _sendSemaphore.WaitAsync();
            try
            {
                var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(request));
                await SendAsync(data, WebSocketMessageType.Text);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task AsyncSendBinary(byte[] request)
        {
            await _sendSemaphore.WaitAsync();
            try
            {
                var data = new ArraySegment<byte>(request);
                await SendAsync(data, WebSocketMessageType.Binary);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task ReceiveLoop()
        {
            Exception exception = null;
            try
            {
                var buffer = new byte[1024 * 4];
                while (_client.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    var message = new List<byte>();
                    do
                    {
                        result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new TDengineError((int)TDengineError.InternalErrorCode.WS_RECEIVE_CLOSE_FRAME,
                                "receive websocket close frame");
                        }

                        message.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    var bs = message.ToArray();
                    TaskCompletionSource<WsMessage> tcs;
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                            if (bs.Length < 16)
                            {
                                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                                    $"binary message length is less than 16, length:{bs.Length}");
                            }

                            var flag = BitConverter.ToUInt64(bs, 0);
                            var reqId = BitConverter.ToUInt64(bs, 8);
                            if (flag == 0xffffffffffffffff)
                            {
                                reqId = BitConverter.ToUInt64(bs, 26);
                            }

                            if (_pendingRequests.TryRemove(reqId, out tcs))
                            {
                                tcs.TrySetResult(new WsMessage(bs, result.MessageType, null));
                            }

                            break;

                        case WebSocketMessageType.Text:
                            WSBaseResp resp;
                            try
                            {
                                resp = JsonConvert.DeserializeObject<WSBaseResp>(
                                    Encoding.UTF8.GetString(bs));
                            }
                            catch (Exception e)
                            {
                                throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                                    "receive unexpected message", e.Message);
                            }

                            if (_pendingRequests.TryRemove(resp.ReqId, out tcs))
                            {
                                tcs.TrySetResult(new WsMessage(bs, result.MessageType, null));
                            }

                            break;
                        default:
                            throw new TDengineError((int)TDengineError.InternalErrorCode.WS_UNEXPECTED_MESSAGE,
                                "receive unexpected message type");
                    }
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                DoClose(exception);
            }
        }

        private void DoClose(Exception e = null)
        {
            if (Interlocked.CompareExchange(ref _isShutdown, 1, 0) != 0) return;
            try
            {
                _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _exitLock.EnterWriteLock();
                try
                {
                    _exit = true;
                    foreach (var kvp in _pendingRequests)
                    {
                        if (e != null)
                        {
                            kvp.Value.TrySetResult(new WsMessage(null, WebSocketMessageType.Close, e));
                        }
                        else
                        {
                            kvp.Value.TrySetCanceled();
                        }
                    }

                    _pendingRequests.Clear();
                }
                finally
                {
                    _exitLock.ExitWriteLock();
                }
            }
        }

        public void Close()
        {
            IsManualShutdown = true;
            DoClose();
        }

        public bool IsAvailable(Exception e = null)
        {
            if (IsManualShutdown) return false;
            if (IsExit) return false;
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