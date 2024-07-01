using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TDengine.Driver
{
    public class BlockReader
    {
        private static readonly int ColInfoSize = TDengineConstant.Int8Size + TDengineConstant.Int32Size;
        private static readonly int RawBlockVersionOffset = 0;
        private static readonly int RawBlockLengthOffset = RawBlockVersionOffset + TDengineConstant.Int32Size;
        private static readonly int NumOfRowsOffset = RawBlockLengthOffset + TDengineConstant.Int32Size;
        private static readonly int NumOfColsOffset = NumOfRowsOffset + TDengineConstant.Int32Size;
        private static readonly int HasColumnSegmentOffset = NumOfColsOffset + TDengineConstant.Int32Size;
        private static readonly int GroupIdOffset = HasColumnSegmentOffset + TDengineConstant.Int32Size;
        private static readonly int ColInfoOffset = GroupIdOffset + TDengineConstant.UInt64Size;

        private byte[] _block;
        private int _rows;
        private int _lengthOffset;
        private int _headerOffset;
        private int _nullBitMapOffset;

        private int _precision;
        private int[] _colHeadOffset;
        private int _cols;
        private byte[] _colType;
        private TimeZoneInfo _tz;

        private int _offset;
        private readonly bool _disableParseTime;


        private readonly Dictionary<TDengineDataType, Func<int, int, object>> _methodMap =
            new Dictionary<TDengineDataType, Func<int, int, object>>();


        public BlockReader(int offset, int cols, int precision, byte[] colType, TimeZoneInfo tz = default) :
            this(offset, tz)
        {
            _cols = cols;
            _precision = precision;
            _colHeadOffset = new int[cols];
            _colType = colType;
        }

        public BlockReader(int offset, TimeZoneInfo tz = default)
        {
            _offset = offset;
            if (tz == default)
            {
                tz = TimeZoneInfo.Local;
            }

            _tz = tz;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_BOOL] = ConvertBool;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_TINYINT] = ConvertTinyint;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_SMALLINT] = ConvertSmallint;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_INT] = ConvertInt;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_BIGINT] = ConvertBigInt;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_FLOAT] = ConvertFloat;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_DOUBLE] = ConvertDouble;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_BINARY] = ConvertBinary;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_TIMESTAMP] = ConvertTime;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_NCHAR] = ConvertNchar;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_UTINYINT] = ConvertUTinyint;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_USMALLINT] = ConvertUSmallint;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_UINT] = ConvertUInt;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_UBIGINT] = ConvertUBigInt;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_JSONTAG] = ConvertJson;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_VARBINARY] = ConvertBinary;
            _methodMap[TDengineDataType.TSDB_DATA_TYPE_GEOMETRY] = ConvertBinary;
        }

        public BlockReader(int offset, int cols, byte[] colType) : this(offset, cols, 0, colType)
        {
            _disableParseTime = true;
        }

        public void SetBlockPtr(IntPtr pBlock, int rows)
        {
            var blockSize = GetBlockSize(pBlock);
            byte[] dataArray = new byte[blockSize];
            Marshal.Copy(pBlock, dataArray, 0, blockSize);
            SetBlock(dataArray);
        }

        private Int32 GetBlockSize(IntPtr pBlock)
        {
            return Marshal.ReadInt32(pBlock + _offset + RawBlockLengthOffset);
        }

        public void SetBlock(byte[] block)
        {
            _block = block;
            _rows = GetRowCount();
            _nullBitMapOffset = TDengineConstant.BitmapLen(_rows);
            _lengthOffset = _offset + ColInfoOffset + _cols * ColInfoSize;
            _headerOffset = _offset + ColInfoOffset + _cols * ColInfoSize + _cols * TDengineConstant.Int32Size;
            _colHeadOffset[0] = _headerOffset;
            if (_cols == 1) return;
            for (int i = 0; i < _cols - 1; i++)
            {
                var colLength = BitConverter.ToInt32(block, _lengthOffset + TDengineConstant.Int32Size * i);
                if (IsVarDataType(_colType[i]))
                {
                    _colHeadOffset[i + 1] = _colHeadOffset[i] + TDengineConstant.Int32Size * _rows + colLength;
                }
                else
                {
                    _colHeadOffset[i + 1] = _colHeadOffset[i] + _nullBitMapOffset + colLength;
                }
            }
        }

        public void SetTMQBlock(byte[] block, int precision, int offset)
        {
            _block = block;
            _offset = offset;
            _precision = precision;
            _cols = GetColumnCount();
            _colHeadOffset = new int[_cols];
            _colType = GetColTypes();
            SetBlock(_block);
        }

        public int GetRows()
        {
            return _rows;
        }

        private int GetColumnCount()
        {
            return BitConverter.ToInt32(_block, _offset + NumOfColsOffset);
        }

        private int GetRowCount()
        {
            return BitConverter.ToInt32(_block, _offset + NumOfRowsOffset);
        }

        private byte[] GetColTypes()
        {
            var cols = GetColumnCount();
            var result = new byte[cols];
            for (int i = 0; i < cols; i++)
            {
                result[i] = _block[_offset + ColInfoOffset + i * ColInfoSize];
            }

            return result;
        }

        private bool ItemIsNull(int headOffset, int row) =>
            TDengineConstant.BitmapIsNull(_block[headOffset + TDengineConstant.CharOffset(row)], row);

        private static bool IsVarDataType(byte colType)
        {
            switch ((TDengineDataType)colType)
            {
                case TDengineDataType.TSDB_DATA_TYPE_BINARY:
                case TDengineDataType.TSDB_DATA_TYPE_NCHAR:
                case TDengineDataType.TSDB_DATA_TYPE_JSONTAG:
                case TDengineDataType.TSDB_DATA_TYPE_VARBINARY:
                case TDengineDataType.TSDB_DATA_TYPE_GEOMETRY:
                    return true;
                default:
                    return false;
            }
        }

        public object Read(int row, int col)
        {
            if (IsVarDataType(_colType[col]))
            {
                return _methodMap[(TDengineDataType)_colType[col]](row, col);
            }

            return ItemIsNull(_colHeadOffset[col], row) ? null : _methodMap[(TDengineDataType)_colType[col]](row, col);
        }

        private object ConvertBool(int row, int col) => _block[_colHeadOffset[col] + _nullBitMapOffset + row] != 0;

        private object ConvertTinyint(int row, int col)
        {
            return (sbyte)_block[_colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Int8Size];
        }

        private object ConvertSmallint(int row, int col) =>
            BitConverter.ToInt16(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Int16Size);

        private object ConvertInt(int row, int col) =>
            BitConverter.ToInt32(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Int32Size);

        private object ConvertBigInt(int row, int col) =>
            BitConverter.ToInt64(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Int64Size);

        private object ConvertUTinyint(int row, int col) =>
            _block[_colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.UInt8Size];

        private object ConvertUSmallint(int row, int col) =>
            BitConverter.ToUInt16(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.UInt16Size);

        private object ConvertUInt(int row, int col) =>
            BitConverter.ToUInt32(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.UInt32Size);

        private object ConvertUBigInt(int row, int col) =>
            BitConverter.ToUInt64(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.UInt64Size);

        private object ConvertFloat(int row, int col) =>
            BitConverter.ToSingle(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Float32Size);

        private object ConvertDouble(int row, int col) =>
            BitConverter.ToDouble(_block, _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Float64Size);

        private object ConvertTime(int row, int col)
        {
            var ts = BitConverter.ToInt64(_block,
                _colHeadOffset[col] + _nullBitMapOffset + row * TDengineConstant.Int64Size);
            if (_disableParseTime)
            {
                return ts;
            }

            return TDengineConstant.ConvertTimeToDatetime(ts, (TDenginePrecision)_precision, _tz);
        }

        private object ConvertBinary(int row, int col)
        {
            var offset = BitConverter.ToInt32(_block, _colHeadOffset[col] + row * 4);
            if (offset == -1)
            {
                return null;
            }

            var start = _colHeadOffset[col] + TDengineConstant.Int32Size * _rows;
            var currentRow = start + offset;
            var clen = BitConverter.ToUInt16(_block, currentRow);
            currentRow += 2;
            byte[] subarray = new byte[clen];
            Array.Copy(_block, currentRow, subarray, 0, clen);
            return subarray;
        }

        private object ConvertNchar(int row, int col)
        {
            var offset = BitConverter.ToInt32(_block, _colHeadOffset[col] + row * 4);
            if (offset == -1)
            {
                return null;
            }

            var start = _colHeadOffset[col] + TDengineConstant.Int32Size * _rows;
            var currentRow = start + offset;
            var clen = BitConverter.ToUInt16(_block, currentRow);
            currentRow += 2;
            return ConvertUcs4BytesToUtf8String(_block, currentRow, clen);
        }

        private static string ConvertUcs4BytesToUtf8String(byte[] ucs4Bytes, int offset, int count)
        {
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.UTF32, Encoding.UTF8, ucs4Bytes, offset, count));
        }

        private object ConvertJson(int row, int col)
        {
            var offset = BitConverter.ToInt32(_block, _colHeadOffset[col] + row * 4);
            if (offset == -1)
            {
                return null;
            }

            var start = _colHeadOffset[col] + TDengineConstant.Int32Size * _rows;
            var currentRow = start + offset;
            var clen = BitConverter.ToUInt16(_block, currentRow);
            currentRow += 2;
            byte[] subarray = new byte[clen];
            Array.Copy(_block, currentRow, subarray, 0, clen);
            return subarray;
        }

        public long GetChars(int row, int col, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            if (!IsVarDataType(_colType[col]))
            {
                throw new Exception("GetBytes cannot be used on non-character columns");
            }

            var data = Read(row, col);

            char[] value = null;
            switch (data)
            {
                case string val:
                    value = val.ToCharArray();
                    break;
                case byte[] val:
                    value = Encoding.UTF8.GetChars(val);
                    break;
            }

            if (value == null)
            {
                return 0;
            }

            var dataLength = value.Length - (int)dataOffset;
            var bufferLength = buffer.Length - bufferOffset;
            var minLength = dataLength > bufferLength ? bufferLength : dataLength;
            minLength = minLength > length ? length : minLength;
            Array.Copy(value, (int)dataOffset, buffer, bufferOffset, minLength);
            return minLength;
        }

        public char GetChar(int row, int col)
        {
            if (!IsVarDataType(_colType[col]))
            {
                throw new Exception("GetChar cannot be used on non-character columns");
            }

            var data = Read(row, col);

            switch (data)
            {
                case string val:
                    return val[0];
                case byte[] val:
                    return Encoding.UTF8.GetChars(val)[0];
            }

            return (char)0;
        }

        public long GetBytes(int row, int col, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            if (!IsVarDataType(_colType[col]))
            {
                throw new Exception("GetBytes cannot be used on non-character columns");
            }

            var data = Read(row, col);

            byte[] value = null;
            switch (data)
            {
                case string val:
                    value = Encoding.UTF8.GetBytes(val);
                    break;
                case byte[] val:
                    value = val;
                    break;
            }

            if (value == null)
            {
                return 0;
            }

            var dataLength = value.Length - (int)dataOffset;
            var bufferLength = buffer.Length - bufferOffset;
            var minLength = Math.Min(Math.Min(dataLength, bufferLength), length);
            Array.Copy(value, (int)dataOffset, buffer, bufferOffset, minLength);
            return minLength;
        }
    }
}