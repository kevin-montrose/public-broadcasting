using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> : IDictionary<ToKey, ToVal>
    {
        class Enumerator : IEnumerator<KeyValuePair<ToKey, ToVal>>, IEnumerator
        {
            public KeyValuePair<ToKey, ToVal> Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            private IDictionaryEnumerator InnerEnumerable;
            private OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> Dictionary;
            public Enumerator(OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> dict)
            {
                Dictionary = dict;
            }

            public bool MoveNext()
            {
                if (InnerEnumerable == null)
                {
                    InnerEnumerable = Dictionary.InnerDictionary.GetEnumerator();
                }

                if (!InnerEnumerable.MoveNext())
                {
                    Current = default(KeyValuePair<ToKey, ToVal>);
                    return false;
                }

                var entry = InnerEnumerable.Entry;

                var key = entry.Key;
                var val = entry.Value;

                var mappedKey = (ToKey)Dictionary.KeyMapper(key);
                var mappedVal = (ToVal)Dictionary.ValueMapper(val);

                Current = new KeyValuePair<ToKey, ToVal>(mappedKey, mappedVal);

                return true;
            }

            public void Reset()
            {
                InnerEnumerable = null;
                Current = default(KeyValuePair<ToKey, ToVal>);
            }

            public void Dispose() { }
        }

        public ICollection<ToKey> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<ToVal> Values
        {
            get { throw new NotImplementedException(); }
        }

        public int Count
        {
            get { return InnerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ToVal this[ToKey key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private IDictionary InnerDictionary;
        private Func<object, object> KeyMapper;
        private Func<object, object> ValueMapper;

        public OnDemandDictionary(IDictionary innerDict, Func<object, object> keyMapper, Func<object, object> valMapper)
        {
            InnerDictionary = innerDict;
            KeyMapper = keyMapper;
            ValueMapper = valMapper;
        }

        public IEnumerator<KeyValuePair<ToKey, ToVal>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #region Not Implemented

        public void Add(ToKey key, ToVal value)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(ToKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ToKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(ToKey key, out ToVal value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<ToKey, ToVal>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
