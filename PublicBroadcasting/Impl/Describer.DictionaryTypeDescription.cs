using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class DictionaryTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription KeyType { get; set; }
        [ProtoMember(2)]
        internal TypeDescription ValueType { get; set; }

        private DictionaryTypeDescription() { }

        private DictionaryTypeDescription(TypeDescription keyType, TypeDescription valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        static internal TypeDescription Create(TypeDescription keyType, TypeDescription valueType)
        {
            return new DictionaryTypeDescription(keyType, valueType);
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return typeof(Dictionary<,>).MakeGenericType(KeyType.GetPocoType(existing), ValueType.GetPocoType(existing));
        }

        internal override void Seal(TypeDescription existing = null)
        {
            KeyType.Seal(existing);
            ValueType.Seal(existing);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            Action act1, act2;

            KeyType = KeyType.DePromise(out act1);
            ValueType = ValueType.DePromise(out act2);

            afterPromise = () => { act1(); act2(); };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new DictionaryTypeDescription();
            backRefLookup[this] = ret;

            ret.KeyType = KeyType.Clone(backRefLookup);
            ret.ValueType = ValueType.Clone(backRefLookup);

            return ret;
        }
    }
}
