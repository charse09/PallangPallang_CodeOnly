using System.Collections.Generic;

namespace _Project.Scripts.Data
{
    public class RowEntity
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();
        private Dictionary<string, string> _columnTypes = new Dictionary<string, string>(); // 타입 정보 저장

        public void SetField(string columnName, object value, string columnType)
        {
            _data[columnName] = value;
            _columnTypes[columnName] = columnType;
        }

        public T Get<T>(string columnName)
        {
            if (_data.TryGetValue(columnName, out object value))
            {
                // object를 T로 안전하게 형변환
                try { return (T)System.Convert.ChangeType(value, typeof(T)); }
                catch { return (T)value; }
            }
            return default;
        }

        public string GetColumnType(string columnName) => _columnTypes.GetValueOrDefault(columnName, "");
        public IEnumerable<string> GetColumns() => _data.Keys;
        public bool ContainsColumn(string columnName) => _data.ContainsKey(columnName);
    }
}