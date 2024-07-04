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
            _blockReader = new BlockReader(55, FieldCount, result.Precision, result.FieldsTypes, tz);
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
            _blockReader = new BlockReader(55, FieldCount, result.Precision, result.FieldsTypes, tz);
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
            var fetchRawBlockResult = _connection.FetchRawBlockBinary(_resultId);
            //Flag           uint64 //8               0
            //Action         uint64 //8               8
            //Version        uint16 //2               16
            //Time           uint64 //8               18
            //ReqID          uint64 //8               26
            //Code           uint32 //4               34
            //MessageLen     uint32 //4               38
            //Message        string //MessageLen      42
            //ResultID       uint64 //8               42 + MessageLen
            //Finished       bool   //1               50 + MessageLen
            //RawBlockLength uint32 //4               51 + MessageLen
            //RawBlock       []byte //RawBlockLength  55 + MessageLen + RawBlockLength
            var version = BitConverter.ToUInt16(fetchRawBlockResult, 16);
            if (version != 1)
                throw new Exception("Unsupported fetch raw block version " + version);
            var code = BitConverter.ToUInt32(fetchRawBlockResult, 34);
            var messageLen = BitConverter.ToUInt32(fetchRawBlockResult, 38);
            var message = _encoding.GetString(fetchRawBlockResult, 42, (int)messageLen);
            if (code != 0)
                throw new TDengineError((int)code, message);
            _completed = BitConverter.ToBoolean(fetchRawBlockResult, 50 + (int)messageLen);
            if (_completed)
                return;
            var rawBlockLength = BitConverter.ToUInt32(fetchRawBlockResult, 51 + (int)messageLen);
            if (fetchRawBlockResult.Length != 55 + (int)messageLen + rawBlockLength)
                throw new Exception("Invalid fetch raw block result length");
            _block = fetchRawBlockResult;
            _blockReader.SetBlock(_block);
            _blockSize = _blockReader.GetRows();
            _currentRow = 0;
        }
    }
}