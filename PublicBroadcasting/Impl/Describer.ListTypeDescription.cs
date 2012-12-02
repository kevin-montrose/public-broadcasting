using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class ListTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription Contains { get; set; }

        private ListTypeDescription() { }

        private ListTypeDescription(TypeDescription contains)
        {
            Contains = contains;
        }

        static internal ListTypeDescription Create(TypeDescription contains)
        {
            return new ListTypeDescription(contains);
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return typeof(IList<>).MakeGenericType(Contains.GetPocoType(existing));
        }

        internal override void Seal(TypeDescription existing = null)
        {
            Contains.Seal(existing);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            Contains = Contains.DePromise(out afterPromise);

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new ListTypeDescription();
            backRefLookup[this] = ret;

            ret.Contains = Contains.Clone(backRefLookup);

            return ret;
        }
    }
}
