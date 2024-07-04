using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TDengine.Driver;
using TDengine.Driver.Impl.NativeMethods;

namespace TDengine.TMQ.Native
{
    public class TMQNativeRows : ITMQRows
    {
        private readonly IntPtr _result;
        private int _currentRow;
        private List<TDengineMeta> _metas;
        private int _blockRows;
        private byte[] _block;
        private bool _completed;
        private int _blockIndex;
        private TMQBlockReader.TMQBlockInfo[] _blockInfo;
        private readonly BlockReader _blockReader;
        private readonly TMQBlockReader _tmqBlockReader;

        public TMQNativeRows(IntPtr result, TimeZoneInfo tz)
        {
            _result = result;
            _blockReader = new BlockReader(0, tz);
            _tmqBlockReader = new TMQBlockReader(0);
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

        public int FieldCount { get; private set; }
        public string TableName { get; private set; }

        public string GetName(int ordinal) => _metas[ordinal].name;

        private void FetchBlock()
        {
            if (_block == null)
            {
                int structSize = Marshal.SizeOf(typeof(TMQRawData));
                IntPtr raw = Marshal.AllocHGlobal(structSize);
                try
                {
                    var code = NativeMethods.TmqGetRaw(_result, raw);
                    if (code != 0)
                    {
                        throw new TDengineError(code, NativeMethods.Error(_result));
                    }

                    TMQRawData rawData = (TMQRawData)Marshal.PtrToStructure(raw, typeof(TMQRawData));
                    _block = new byte[rawData.rawLen];
                    Marshal.Copy(rawData.raw, _block, 0, (int)rawData.rawLen);
                    _blockInfo = _tmqBlockReader.Parse(_block);
                    _blockIndex = 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(raw);
                }
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
    }
}