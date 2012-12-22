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
        internal override bool NeedsMapping
        {
            get 
            { 
                return 
                    ForType == typeof(byte[]) ||
                    Contains.NeedsMapping; 
            }
        }

        [ProtoMember(1)]
        internal TypeDescription Contains { get; set; }

        internal Type ForType { get; set; }

        private ListTypeDescription() 
        {
        }

        private ListTypeDescription(TypeDescription contains, Type listType)
        {
            ForType = listType;

            Contains = contains;
        }

        static internal ListTypeDescription Create(TypeDescription contains, Type type)
        {
            return new ListTypeDescription(contains, type);
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
            ret.ForType = ForType;

            return ret;
        }
    }
}
