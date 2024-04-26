using System;
using System.Collections.Generic;
using System.Text;
using TDengine.Driver.Impl.WebSocketMethods;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;

namespace TDengine.Driver.Client.Websocket
{
    public class WSRows : IRows
    {
        private readonly Connection _connection;
        private readonly ulong _resultId;
        private bool _freed;
        private int _currentRow;
        private readonly bool _isUpdate;
        private readonly List<TDengineMeta> _metas;
        private readonly Encoding _encoding;
        private int _blockSize;
        private byte[] _block;
        private bool _completed;
        private readonly BlockReader _blockReader;

        public WSRows(int affectedRows)
        {
            _isUpdate = true;
            AffectRows = affectedRows;
        }

        public WSRows(WSQueryResp result, Connection connection, TimeZoneInfo tz)
        {
            _connection = connection;
            _resultId = result.ResultId;
            _isUpdate = result.IsUpdate;
            if (_isUpdate)
            {
                AffectRows = result.AffectedRows;
                return;
            }

            AffectRows = -1;
            FieldCount = result.FieldsCount;

            _metas = ParseMetas(result);
            _encoding = Encoding.UTF8;
            _blockReader = new BlockReader(16, FieldCount, result.Precision, result.FieldsTypes, tz);
        }

        public WSRows(WSStmtUseResultResp result, Connection connection, TimeZoneInfo tz)
        {
            _connection = connection;
            _resultId = result.ResultId;
            _isUpdate = false;
            AffectRows = -1;
            FieldCount = result.FieldsCount;
            _metas = ParseMetas(result);
            _encoding = Encoding.UTF8;
            _blockReader = new BlockReader(16, FieldCount, result.Precision, result.FieldsTypes, tz);
        }

        private List<TDengineMeta> ParseMetas(WSQueryResp result)
        {
            List<TDengineMeta> metaList = new List<TDengineMeta>();
            for (int i = 0; i < FieldCount; i++)
            {
                TDengineMeta meta = new TDengineMeta
                {
                    name = result.FieldsNames[i],
                    type = result.FieldsTypes[i],
                    size = (int)result.FieldsLengths[i]
                };
                metaList.Add(meta);
            }

            return metaList;
        }

        private List<TDengineMeta> ParseMetas(WSStmtUseResultResp result)
        {
            List<TDengineMeta> metaList = new List<TDengineMeta>();
            for (int i = 0; i < FieldCount; i++)
            {
                TDengineMeta meta = new TDengineMeta
                {
                    name = result.FieldsNames[i],
                    type = result.FieldsTypes[i],
                    size = (int)result.FieldsLengths[i]
                };
                metaList.Add(meta);
            }

            return metaList;
        }

        public bool HasRows => _isUpdate == false;
        public int AffectRows { get; }
        public int FieldCount { get; }

        public void Dispose()
        {
            if (_freed)
            {
                return;
            }

            _freed = true;
            if (_connection != null && _connection.IsAvailable())
            {
                _connection.FreeResult(_resultId);
            }
        }

        public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _blockReader.GetBytes(_currentRow, ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public char GetChar(int ordinal)
        {
            return _blockReader.GetChar(_currentRow, ordinal);
        }

        public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _blockReader.GetChars(_currentRow, ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public string GetDataTypeName(int ordinal) => _metas[ordinal].TypeName();

        public object GetValue(int ordinal)
        {
            return _blockReader.Read(_currentRow, ordinal);
        }

        public Type GetFieldType(int ordinal) => _metas[ordinal].ScanType();

        public int GetFieldSize(int ordinal) => _metas[ordinal].size;

        public string GetName(int ordinal) => _metas[ordinal].name;

        public int GetOrdinal(string name) => _metas.FindIndex(m => m.name == name);

        public bool Read()
        {
            if (_completed) return false;
            if (_block == null)
            {
                FetchBlock();
                return !_completed;
            }

            _currentRow += 1;
            if (_currentRow != _blockSize) return true;
            FetchBlock();
            return !_completed;
        }

        private void FetchBlock()
        {
            var fetchResult = _connection.Fetch(_resultId);
            _completed = fetchResult.Completed;
            _blockSize = fetchResult.Rows;
            _currentRow = 0;
            if (_completed)
            {
                return;
            }

            _block = _connection.FetchBlock(_resultId);
            _blockReader.SetBlock(_block, _blockSize);
        }
    }
}