using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using TDengine.Driver;

namespace TDengine.Data.Client
{
    public class TDengineDataReader : DbDataReader
    {
        private IRows _rows;
        private int _fieldCount;

        public TDengineDataReader(IRows rows)
        {
            _rows = rows;
            _fieldCount = rows.FieldCount;
        }

        public override DataTable GetSchemaTable()
        {
            var schemaTable = new DataTable();
            if (_fieldCount > 0)
            {
                
                var columns = schemaTable.Columns;
                var columnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
                var columnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
                var columnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
                var dataType = new DataColumn(SchemaTableColumn.DataType, typeof(Type)); 
                var dataTypeName = new DataColumn("DataTypeName", typeof(string));

                columns.Add(columnName);
                columns.Add(columnOrdinal);
                columns.Add(columnSize);
                columns.Add(dataType);
                columns.Add(dataTypeName);

                for (int i = 0; i < _fieldCount; i++)
                {
                    var schemaRow = schemaTable.NewRow();
                    schemaRow[columnName] = GetName(i);
                    schemaRow[columnOrdinal] = i;
                    schemaRow[columnSize] = GetFieldSize(i);
                    schemaRow[dataType] = GetFieldType(i);
                    schemaRow[dataTypeName] = GetDataTypeName(i);
                    schemaTable.Rows.Add(schemaRow);
                }
            }
            return schemaTable;
        }

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

        public override byte GetByte(int ordinal) {
            var value = GetValue(ordinal);
            switch (value)
            {
                case byte val:
                    return val;
                case sbyte val:
                    return (byte)val;
                default:
                    throw new NotSupportedException($"can not change to byte: {value}");
            }
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
            _rows.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        public override char GetChar(int ordinal) => _rows.GetChar(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
            _rows.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        public override string GetDataTypeName(int ordinal) => _rows.GetDataTypeName(ordinal);

        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

        public override decimal GetDecimal(int ordinal) => throw new Exception("TDengine unsupported decimal");

        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

        public override Type GetFieldType(int ordinal) => _rows.GetFieldType(ordinal);

        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

        public override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);

        public override short GetInt16(int ordinal)
        {
            var value = GetValue(ordinal);
            switch (value)
            {
                case short val:
                    return val;
                case ushort val:
                    return (short)val;
                default:
                    throw new NotSupportedException($"can not change to short: {value}");
            }
        }

        public override int GetInt32(int ordinal)
        {
            var value = GetValue(ordinal);
            switch (value)
            {
                case int val:
                    return val;
                case uint val:
                    return (int)val;
                default:
                    throw new NotSupportedException($"can not change to int: {value}");
            }
        }

        public override long GetInt64(int ordinal)
        {
            var value = GetValue(ordinal);
            switch (value)
            {
                case long val:
                    return val;
                case ulong val:
                    return (long)val;
                default:
                    throw new NotSupportedException($"can not change to long: {value}");
            }
        }

        public override string GetName(int ordinal) => _rows.GetName(ordinal);
        
        public  int  GetFieldSize(int ordinal) => _rows.GetFieldSize(ordinal);

        public override int GetOrdinal(string name) => _rows.GetOrdinal(name);

        public override string GetString(int ordinal)
        {
            var value = GetValue(ordinal);
            switch (value)
            {
                case byte[] val:
                    return System.Text.Encoding.UTF8.GetString(val);
                case string val:
                    return val;
                case char[] val:
                    return new string(val);
                default:
                    throw new NotSupportedException($"can not change to string: {value}");
            }
        }

        public override object GetValue(int ordinal) => _rows.GetValue(ordinal);

        public override int GetValues(object[] values)
        {
            for (var i = 0; i < _fieldCount; i++)
            {
                var obj = GetValue(i);
                values[i] = obj;
            }

            return _fieldCount;
        }

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) == null;

        public override int FieldCount => _fieldCount;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override int RecordsAffected => _rows.AffectRows;

        public override bool HasRows => _rows.HasRows;

        public override bool IsClosed => false;

        public override bool NextResult() => false;

        public override bool Read() => _rows.Read();

        public override int Depth => 0;

        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

        public override void Close() => Dispose(true);

        protected override void Dispose(bool disposing)
        {
            if (_rows != null)
            {
                _rows.Dispose();
                _rows = null;
            }
        }
    }
}
