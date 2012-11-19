﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    [ProtoInclude(3, typeof(SimpleTypeDescription))]
    [ProtoInclude(4, typeof(ClassTypeDescription))]
    [ProtoInclude(5, typeof(ListTypeDescription))]
    [ProtoInclude(6, typeof(DictionaryTypeDescription))]
    [ProtoInclude(7, typeof(NullableTypeDescription))]
    [ProtoInclude(8, typeof(BackReferenceTypeDescription))]
    [ProtoInclude(9, typeof(NoTypeDescription))]
    internal abstract class TypeDescription
    {
        internal abstract Type GetPocoType(TypeDescription existingDescription = null);

        internal virtual void Seal(TypeDescription existing = null) { }

        internal abstract TypeDescription DePromise(out Action afterPromise);

        internal abstract TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup);
    }

    [ProtoContract]
    internal class NoTypeDescription : TypeDescription
    {
        internal NoTypeDescription() { }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            throw new NotImplementedException();
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            throw new NotImplementedException();
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            throw new NotImplementedException();
        }
    }
}
