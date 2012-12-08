using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal static class TypeModelBuilder
    {

        public static RuntimeTypeModel BuildTypeModel(TypeDescription desc, RuntimeTypeModel existing = null)
        {
            existing = existing ?? RuntimeTypeModel.Create();

            if (desc is SimpleTypeDescription) return existing;
            if (desc is NullableTypeDescription) return existing;
            if (desc is BackReferenceTypeDescription) return existing;
            if (desc is DictionaryTypeDescription)
            {
                var key = ((DictionaryTypeDescription)desc).KeyType;
                var val = ((DictionaryTypeDescription)desc).ValueType;

                BuildTypeModel(key, existing);
                BuildTypeModel(val, existing);

                return existing;
            }
            if (desc is ListTypeDescription)
            {
                var contains = ((ListTypeDescription)desc).Contains;

                BuildTypeModel(contains, existing);

                return existing;
            }

            if (desc is EnumTypeDescription)
            {
                throw new Exception("Enums cannot be serialized with TypeModelBuilder, they must be mapped");
            }

            if (!(desc is ClassTypeDescription))
            {
                throw new Exception("Unexpected desc: " + desc);
            }

            var asClass = desc as ClassTypeDescription;

            if (asClass.ForType.IsAnonymouseClass())
            {
                throw new Exception("Anonymous classes cannot be serialized with TypeModelBuilder, they must be mapped");
            }

            var type = existing.Add(asClass.ForType, applyDefaultBehaviour: false);

            int ix = 1;
            foreach (var mem in asClass.Members.OrderBy(o => o.Key, StringComparer.Ordinal))
            {
                var valMem = type.AddField(ix, mem.Key);

                BuildTypeModel(mem.Value, existing);
                ix++;
            }

            return existing;
        }
    }
}
