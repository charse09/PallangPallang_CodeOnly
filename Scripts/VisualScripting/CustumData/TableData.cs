using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public class TableData
    {
        private Dictionary<string, RowEntity> _rows = new Dictionary<string, RowEntity>();

        public void AddRow(string id, RowEntity row) => _rows[id] = row;

        public RowEntity GetRow(string id)
        {
            if (_rows.TryGetValue(id, out var row)) return row;
            return null;
        }

        // [기획자 편의 기능] 이름이나 특정 값으로 행을 검색
        public RowEntity FindRowByColumn(string columnName, string value)
        {
            return _rows.Values.FirstOrDefault(r => r.Get<string>(columnName) == value);
        }

        public IEnumerable<RowEntity> AllRows => _rows.Values;
    }
}