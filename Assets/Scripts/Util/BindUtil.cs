using System;
using System.Collections;
using System.Collections.Generic;

public class BindUtil
{
    private static Dictionary<string, List<Type>> _pathAndTypeMap = new Dictionary<string, List<Type>>();
    private static Dictionary<Type, int> _priorityMap = new Dictionary<Type, int>();

    public static void Bind(BindAttribute data, Type type)
    {
        string path = data.Path;

        if (!_pathAndTypeMap.ContainsKey(path))
        {
            _pathAndTypeMap[path] = new List<Type>();
        }

        if (!_pathAndTypeMap[data.Path].Contains(type))
        {
            _pathAndTypeMap[path].Add(type);
            _priorityMap.Add(type, data.Priority);
            _pathAndTypeMap[path].Sort(new PriorityCompare());
        }
    }

    public static List<Type> GetType(string path)
    {
        return _pathAndTypeMap[path];
    }

    public class PriorityCompare : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            return _priorityMap[x] - _priorityMap[y];
        }
    }

}