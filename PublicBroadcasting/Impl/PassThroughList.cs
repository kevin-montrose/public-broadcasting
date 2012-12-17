using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class PassThroughList<T> : IList<T>
    {
        private T[] Inner { get; set; }

        public T this[int index]
        {
            get
            {
                return Inner[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get { return Inner.Length; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public PassThroughList(T[] arr)
        {
            Inner = arr;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Inner).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Inner.GetEnumerator();
        }

        #region Not Implemented

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
