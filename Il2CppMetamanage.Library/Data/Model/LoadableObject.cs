using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class LoadableObject<T>
    {
        public delegate T LoadItem();

        private LoadItem LoadItemCallback { get; }

        public T Item 
        {
            get 
            {
                if (!IsLoaded)
                {
                    _item = LoadItemCallback();
                    _isLoaded = true;

                }
                return _item;
            }

            set
            {
                _item = value;
                _isLoaded = true;
            }
        }

        public bool IsLoaded {
            get => _isLoaded;
        }

        private bool _isLoaded;
        private T _item;

        public LoadableObject(LoadItem loadItemCallback)
        {
            LoadItemCallback = loadItemCallback;
        }

        public void Unload()
        {
            _item = default;
            _isLoaded = false;
        }
    }
}
