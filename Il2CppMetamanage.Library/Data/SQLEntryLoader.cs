using System;
using System.Collections.Generic;
using Il2CppMetamanage.Library.Data.Model;

namespace Il2CppMetamanage.Library.Data
{
    public class SQLEntryLoader<T> where T : SQLEntry
    {
        public delegate void SQLFillElements(Dictionary<int, SQLEntryPromise> elements);

        public T this[int i]
        {
            get
            {
                return _cachedDictionary[i];
            }
        }
        
        public bool Contains(int i)
        {
            return _cachedDictionary.ContainsKey(i);
        }

        private readonly Dictionary<int, T> _cachedDictionary;
        private readonly Stack<Tuple<int, SQLEntryPromise>> _cachedStack;
        private readonly Dictionary<int, SQLEntryPromise> _cachedStackDict;

        private readonly SQLFillElements _loaderCallback;

        public SQLEntryLoader(SQLFillElements loaderCallback)
        {
            _loaderCallback = loaderCallback;
            _cachedDictionary = new Dictionary<int, T>();
            _cachedStack = new Stack<Tuple<int, SQLEntryPromise>>();
            _cachedStackDict = new Dictionary<int, SQLEntryPromise>();
        }

        public SQLEntryPromise AddToOrder(int id)
        {
            if (_cachedStackDict.ContainsKey(id))
                return _cachedStackDict[id];

            var promiseObject = new SQLEntryPromise();
            if (_cachedDictionary.ContainsKey(id))
                promiseObject.Value = this[id];
            else
            {
                _cachedStack.Push(new(id, promiseObject));
                _cachedStackDict.Add(id, promiseObject);
            }
            return promiseObject;
        }

        public void Add(T element)
        {
            if (!_cachedDictionary.ContainsKey(element.Id))
                _cachedDictionary.Add(element.Id, element);
        }

        public void LoadOrdered()
        {
            if (_cachedStack.Count == 0)
                return;

            var dictPromises = new Dictionary<int, SQLEntryPromise>();
            while (_cachedStack.Count > 0)
            {
                var item = _cachedStack.Pop();
                dictPromises.Add(item.Item1, item.Item2);
            }

            _loaderCallback(dictPromises);

            foreach (var entry in dictPromises)
            {
                _cachedStackDict.Remove(entry.Key);

                var promise = entry.Value;
                Add((T)promise.Value);
            }
        }
    }
}
