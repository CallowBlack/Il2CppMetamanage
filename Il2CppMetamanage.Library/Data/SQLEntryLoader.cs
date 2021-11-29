using System;
using System.Collections.Generic;
using Il2CppMetamanage.Library.Data.Model;
using Microsoft.Data.Sqlite;

namespace Il2CppMetamanage.Library.Data
{
    public class SQLEntryLoader<T> where T : SQLEntry
    {

        public int Count { get => _count.Item; }

        public int CachedSize { get => _cachedElements.Count; }

        public int MaxCachedSize { get => _maxCachedSize; 
            set {
                if (CachedSize > value)
                    ClearCached();
                _maxCachedSize = value;
            } 
        }

        public T this[int i]
        {
            get
            {
                if (!IsCached(i))
                {
                    var promise = new SQLEntryPromise();
                    var dict = new Dictionary<int, SQLEntryPromise>
                    {
                        { i, promise }
                    };
                    LoadElements(dict);
                    return promise.Value as T;
                }
                return _cachedElements[i];
            }
        }
        
        public bool IsCached(int i)
        {
            return _cachedElements.ContainsKey(i);
        }

        private readonly Dictionary<int, T> _cachedElements;

        private readonly Stack<Tuple<int, SQLEntryPromise>> _promisedItems;
        private readonly Dictionary<int, SQLEntryPromise> _promisedItemsDict;

        private readonly LoadableObject<int> _count;
        private int _maxCachedSize = 1000;
        private readonly string _tableName;
        public SQLEntryLoader(string tableName)
        {
            _cachedElements = new Dictionary<int, T>();
            _promisedItems = new Stack<Tuple<int, SQLEntryPromise>>();
            _promisedItemsDict = new Dictionary<int, SQLEntryPromise>();
            _count = new(GetCount);
            _tableName = tableName;
        }

        protected virtual int GetCount()
        {
            return SQLDataManager.GetCountTableElements(_tableName);
        }

        public virtual T ReadElement(SqliteDataReader reader)
        {
            throw new NotImplementedException();
        }

        protected virtual void LoadElements(Dictionary<int, SQLEntryPromise> promises)
        {
            using var reader = SQLDataManager.GetDataByIds(promises.Keys, _tableName);

            while (reader.Read())
            {
                var element = ReadElement(reader);
                var promise = promises[element.Id];
                promise.Value = element;
            }
        }

        public virtual List<T> GetNextElements(int id, int count)
        {
            var command = SQLDataManager.Connection.CreateCommand();
            command.CommandText = $"SELECT * FROM [{_tableName}] WHERE [id] > {id} LIMIT {count};";
            using var reader = command.ExecuteReader();

            var elements = new List<T>();
            while (reader.Read())
            {
                var element = ReadElement(reader);
                elements.Add(element);
                Add(element);
            }
            return elements;
        }

        public SQLEntryPromise GetPromise(int id)
        {
            if (_promisedItemsDict.ContainsKey(id))
                return _promisedItemsDict[id];

            var promiseObject = new SQLEntryPromise();
            if (_cachedElements.ContainsKey(id))
                promiseObject.Value = this[id];
            else
            {
                _promisedItems.Push(new(id, promiseObject));
                _promisedItemsDict.Add(id, promiseObject);
            }
            return promiseObject;
        }

        public void ClearCached()
        {
            _cachedElements.Clear();
        }

        public void Add(T element)
        {
            if (!_cachedElements.ContainsKey(element.Id))
            {
                if (MaxCachedSize <= CachedSize)
                    ClearCached();
                _cachedElements.Add(element.Id, element);
            }   
        }

        public void LoadPromised()
        {
            if (_promisedItems.Count == 0)
                return;

            var dictPromises = new Dictionary<int, SQLEntryPromise>();
            while (_promisedItems.Count > 0)
            {
                var item = _promisedItems.Pop();
                dictPromises.Add(item.Item1, item.Item2);
            }

            LoadElements(dictPromises);

            foreach (var entry in dictPromises)
            {
                _promisedItemsDict.Remove(entry.Key);

                var promise = entry.Value;
                Add((T)promise.Value);
            }
        }
    }
}
