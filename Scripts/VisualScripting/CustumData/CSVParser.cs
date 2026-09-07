using System;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public static class CSVParser
    {
        public static object ParseValue(string type, string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            return type.ToLower() switch
            {
                "int32" => int.Parse(value),
                "int64" => long.Parse(value),
                "float" => float.Parse(value),
                "double" => double.Parse(value),
                "bool" => bool.Parse(value),
                "string" => value,
                "struct" => value, // struct나 ref는 대상 ID(문자열)를 그대로 반환
                "ref" => value,
                _ => value
            };
        }
    }
}