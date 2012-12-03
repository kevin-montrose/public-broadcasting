using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class OnDemandList<From, To> : IList<To>
    {
        class Enumerator<A, B> : IEnumerator, IEnumerator<B>
        {
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public B Current { get; private set; }

            private int Index = -1;
            private OnDemandList<A, B> List;

            public Enumerator(OnDemandList<A, B> list)
            {
                List = list;
            }

            public bool MoveNext()
            {
                Index++;

                if (Index >= List.Count) return false;

                Current = List[Index];

                return true;
            }

            public void Reset()
            {
                Index = -1;
                Current = default(B);
            }

            public void Dispose() { }
        }

        public Type FromType { get { return typeof(From); } }
        public Type ToType { get { return typeof(To); } }

        public int Count
        {
            get { return UnderlyingList.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public To this[int index]
        {
            get
            {
                var raw = UnderlyingList[index];
                var mapped = Mapper(raw);

                return (To)mapped;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private IList UnderlyingList;
        private Func<object, object> Mapper;

        public OnDemandList(IList underlyingList, Func<object, object> mapper)
        {
            UnderlyingList = underlyingList;
            Mapper = mapper;
        }

        public IEnumerator<To> GetEnumerator()
        {
            return new Enumerator<From, To>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator<From, To>(this);
        }

        #region Not Implemented

        public int IndexOf(To item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, To item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(To item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(To item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(To[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(To item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
