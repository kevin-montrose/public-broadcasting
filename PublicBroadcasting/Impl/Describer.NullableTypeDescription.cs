using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class NullableTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription InnerType { get; set; }

        private NullableTypeDescription() { }

        internal NullableTypeDescription(TypeDescription inner)
        {
            InnerType = inner;
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            var inner = InnerType.GetPocoType(existingDescription);

            return typeof(Nullable<>).MakeGenericType(inner);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            InnerType = InnerType.DePromise(out afterPromise);

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new NullableTypeDescription();
            backRefLookup[this] = ret;

            ret.InnerType = InnerType.Clone(backRefLookup);

            return ret;
        }
    }
}
