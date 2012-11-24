using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class EnumTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal List<string> Values { get; set; }

        private EnumTypeDescription() { }

        internal EnumTypeDescription(Type t)
        {
            var vals = Enum.GetValues(t);

            Values = new List<string>(vals.Length);
            foreach (var v in vals)
            {
                Values.Add(v.ToString());
            }
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            return typeof(string);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };
            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            return this;
        }
    }
}
