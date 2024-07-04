using System;
using System.Collections.Generic;
using TDengine.Driver;
using TDengine.Driver.Impl.WebSocketMethods;
using TDengine.Driver.Impl.WebSocketMethods.Protocol;

namespace TDengine.TMQ.WebSocket
{
    public class TMQWSRows : ITMQRows
    {
        private readonly TMQConnection _connection;
        private readonly ulong _resultId;
        private int _currentRow;
        private List<TDengineMeta> _metas;
        private int _blockRows;
        private byte[] _block;
        private int _blockIndex;
        private TMQBlockReader.TMQBlockInfo[] _blockInfo;
        private bool _completed;
        private readonly BlockReader _blockReader;
        private readonly TMQBlockReader _tmqBlockReader;

        public TMQWSRows(WSTMQPollResp result, TMQConnection connection, TimeZoneInfo tz)
        {
            _connection = connection;
            _resultId = result.MessageId;
            _blockReader = new BlockReader(24, tz);
            _tmqBlockReader = new TMQBlockReader(38);
        }

        public object GetValue(int ordinal)
        {
            return _blockReader.Read(_currentRow, ordinal);
        }

        public bool Read()
        {
            if (_completed) return false;
            if (_block == null)
            {
                FetchBlock();
                return !_completed;
            }

            _currentRow += 1;
            if (_currentRow != _blockRows) return true;
            FetchBlock();
            return !_completed;
        }

        private void FetchBlock()
        {
            if (_block == null)
            {
                var fetchRawResp = _connection.FetchRawBlock(_resultId);
                _block = fetchRawResp;
                _blockInfo = _tmqBlockReader.Parse(_block);
                _blockIndex = 0;
            }
            else
            {
                _blockIndex += 1;
            }

            if (_blockIndex == _blockInfo.Length)
            {
                _completed = true;
                return;
            }

            _blockReader.SetTMQBlock(_block, _blockInfo[_blockIndex].precision, _blockInfo[_blockIndex].rawBlockOffset);
            _blockRows = _blockReader.GetRows();
            _currentRow = 0;

            FieldCount = _blockInfo[_blockIndex].schemas.Length;
            TableName = _blockInfo[_blockIndex].tableName;
            _metas = new List<TDengineMeta>();
            for (int i = 0; i < FieldCount; i++)
            {
                _metas.Add(new TDengineMeta
                {
                    name = _blockInfo[_blockIndex].schemas[i].name,
                    type = _blockInfo[_blockIndex].schemas[i].colType,
                    size = _blockInfo[_blockIndex].schemas[i].bytes,
                });
            }
        }

        public int FieldCount { get; private set; }

        public string TableName { get; private set; }
        public string GetName(int ordinal) => _metas[ordinal].name;
    }
}