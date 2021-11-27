using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppMetamanage.Library.Data.Model
{
    public class SQLEntryPromise
    {
        public SQLEntry Value {
            get
            {
                if (!IsLoaded)
                    throw new Exception("Value is not loaded.");
                return _value;
            }
            
            set
            {
                _value = value;
                _isLoaded = true;
            }
        }

        public bool IsLoaded { get => _isLoaded; }
        
        private SQLEntry _value;
        private bool _isLoaded;

        public SQLEntryPromise()
        {
            _value = default(SQLEntry);
            _isLoaded = false;
        }
    }
}
