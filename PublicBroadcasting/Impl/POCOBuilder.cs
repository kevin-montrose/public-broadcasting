using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class POCOBuilder<From>
    {
        private static readonly Type[] EmptyTypes = new Type[0];
        private static readonly object[] EmptyObjects = new object[0];

        private static Func<object, object> GetListMapper(ListTypeDescription desc, IncludedMembers members, IncludedVisibility visibility)
        {
            var fromListType = typeof(From).GetGenericArguments()[0];

            var itemMapper = (Func<object, object>)(typeof(POCOBuilder<>).MakeGenericType(fromListType).GetMethod("GetMapper").Invoke(null, new object[] { members, visibility }));

            return
                x =>
                {
                    var asList = x as IList;

                    // it's all the same, don't waste our time
                    if (asList == null || asList.Count == 0) return null;

                    var first = itemMapper(asList[0]);

                    var listType = typeof(List<>).MakeGenericType(first.GetType());

                    var ret = (IList)listType.GetConstructor(EmptyTypes).Invoke(EmptyObjects);

                    ret.Add(first);
                    for (var i = 1; i < asList.Count; i++)
                    {
                        var mapped = itemMapper(asList[i]);
                        ret.Add(mapped);
                    }

                    return ret;
                };
        }

        public static Func<object, object> GetMapper(IncludedMembers members, IncludedVisibility visibility)
        {
            const string SelfName = "GetMapper";

            var t = typeof(From);
            var desc = Describer<From>.Get(members, visibility);
            var pocoType = desc.GetPocoType();

            if (desc is ListTypeDescription)
            {
                return GetListMapper((ListTypeDescription)desc, members, visibility);
            }

            if (desc is DictionaryTypeDescription || desc is ListTypeDescription) throw new NotImplementedException();

            if (desc is SimpleTypeDescription)
            {
                if (desc == SimpleTypeDescription.Byte) return x => Convert.ToByte(x);
                if (desc == SimpleTypeDescription.SByte) return x => Convert.ToSByte(x);
                if (desc == SimpleTypeDescription.Short) return x => Convert.ToInt16(x);
                if (desc == SimpleTypeDescription.UShort) return x => Convert.ToUInt16(x);
                if (desc == SimpleTypeDescription.Int) return x => Convert.ToInt32(x);
                if (desc == SimpleTypeDescription.UInt) return x => Convert.ToUInt32(x);
                if (desc == SimpleTypeDescription.Long) return x => Convert.ToInt64(x);
                if (desc == SimpleTypeDescription.ULong) return x => Convert.ToUInt64(x);
                if (desc == SimpleTypeDescription.Double) return x => Convert.ToDouble(x);
                if (desc == SimpleTypeDescription.Float) return x => Convert.ToSingle(x);
                if (desc == SimpleTypeDescription.Decimal) return x => Convert.ToDecimal(x);
                if (desc == SimpleTypeDescription.Char) return x => Convert.ToChar(x);
                if (desc == SimpleTypeDescription.String) return x => Convert.ToString(x);

                throw new Exception("Shouldn't be possible, found " + desc);
            }

            var asClass = (ClassTypeDescription)desc;

            var from = TypeAccessor.Create(t);
            var to = TypeAccessor.Create(pocoType);

            var newPoco = pocoType.GetConstructor(EmptyTypes);

            var propMaps = 
                asClass.Members.ToDictionary(
                    kv => kv.Key, 
                    kv => 
                    {
                        var member = t.GetMember(kv.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

                        var type = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;

                        var self = typeof(POCOBuilder<>).MakeGenericType(type).GetMethod(SelfName);

                        return (Func<object,object>)self.Invoke(null, new object[] { members, visibility });
                    }
                );

            return
                x =>
                {
                    var ret = newPoco.Invoke(EmptyObjects);

                    foreach (var member in asClass.Members)
                    {
                        to[ret, member.Key] = propMaps[member.Key](from[x, member.Key]);
                    }

                    return ret;
                };
        }
    }
}
