using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    class POCOBuilder
    {
        protected Func<object, object> Mapper { get; set; }

        protected POCOBuilder() { }

        internal POCOBuilder(Func<object, object> mapper)
        {
            Mapper = mapper;
        }

        public Func<object, object> GetMapper()
        {
            return Mapper;
        }
    }

    class PromisedPOCOBuilder : POCOBuilder
    {
        internal PromisedPOCOBuilder() { }

        public void Fulfil(Func<object, object> finalMapper)
        {
            Mapper = finalMapper;
        }
    }

    internal class POCOBuilder<From>
    {
        private static readonly Type[] EmptyTypes = new Type[0];
        private static readonly object[] EmptyObjects = new object[0];

        private static POCOBuilder Builder { get; set; }
        private static PromisedPOCOBuilder PromisedBuilder { get; set; }

        static POCOBuilder()
        {
            Debug.WriteLine("POCOBuilder: " + typeof(From).FullName);

            PromisedBuilder = new PromisedPOCOBuilder();

            var builder = Build();
            PromisedBuilder.Fulfil(builder.GetMapper());

            Builder = builder;
        }

        private static POCOBuilder Build()
        {
            const string SelfName = "GetMapper";

            var t = typeof(From);
            var desc = Describer<From>.GetForUse(false);
            var pocoType = desc.GetPocoType();

            if (desc is ListTypeDescription)
            {
                return GetListMapper();
            }

            if (desc is DictionaryTypeDescription)
            {
                return GetDictionaryMapper();
            }

            if (desc is SimpleTypeDescription)
            {
                if (desc == SimpleTypeDescription.Byte) return new POCOBuilder(x => Convert.ToByte(x));
                if (desc == SimpleTypeDescription.SByte) return new POCOBuilder(x => Convert.ToSByte(x));
                if (desc == SimpleTypeDescription.Short) return new POCOBuilder(x => Convert.ToInt16(x));
                if (desc == SimpleTypeDescription.UShort) return new POCOBuilder(x => Convert.ToUInt16(x));
                if (desc == SimpleTypeDescription.Int) return new POCOBuilder(x => Convert.ToInt32(x));
                if (desc == SimpleTypeDescription.UInt) return new POCOBuilder(x => Convert.ToUInt32(x));
                if (desc == SimpleTypeDescription.Long) return new POCOBuilder(x => Convert.ToInt64(x));
                if (desc == SimpleTypeDescription.ULong) return new POCOBuilder(x => Convert.ToUInt64(x));
                if (desc == SimpleTypeDescription.Double) return new POCOBuilder(x => Convert.ToDouble(x));
                if (desc == SimpleTypeDescription.Float) return new POCOBuilder(x => Convert.ToSingle(x));
                if (desc == SimpleTypeDescription.Decimal) return new POCOBuilder(x => Convert.ToDecimal(x));
                if (desc == SimpleTypeDescription.Char) return new POCOBuilder(x => Convert.ToChar(x));
                if (desc == SimpleTypeDescription.String) return new POCOBuilder(x => Convert.ToString(x));

                throw new Exception("Shouldn't be possible, found " + desc);
            }

            var asClass = (ClassTypeDescription)desc;

            var from = TypeAccessor.Create(t);
            var to = TypeAccessor.Create(pocoType);

            var newPoco = pocoType.GetConstructor(EmptyTypes);

            Dictionary<string, POCOBuilder> propMaps = null;

            propMaps =
                asClass.Members.ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        var member = t.GetMember(kv.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

                        var type = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;

                        var self = typeof(POCOBuilder<>).MakeGenericType(type).GetMethod(SelfName);

                        return (POCOBuilder)self.Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public });
                    }
                );

            Func<object, object> mapFuncRet =
                x =>
                {
                    if (x == null) return null;

                    var ret = newPoco.Invoke(EmptyObjects);

                    foreach (var member in asClass.Members)
                    {
                        var fromVal = from[x, member.Key];

                        if (fromVal == null) continue;

                        var toVal = propMaps[member.Key].GetMapper()(fromVal);

                        to[ret, member.Key] = toVal;
                    }

                    return ret;
                };

            return new POCOBuilder(mapFuncRet);
        }

        private static POCOBuilder GetListMapper()
        {
            var fromListType = typeof(From).GetGenericArguments()[0];

            var itemMapper = (POCOBuilder)(typeof(POCOBuilder<>).MakeGenericType(fromListType).GetMethod("GetMapper").Invoke(null,new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public }));

            return
                new POCOBuilder(
                    x =>
                    {
                        var asList = x as IList;

                        // it's all the same, don't waste our time
                        if (asList == null || asList.Count == 0) return null;

                        var first = itemMapper.GetMapper()(asList[0]);

                        var listType = typeof(List<>).MakeGenericType(first.GetType());

                        var ret = (IList)listType.GetConstructor(EmptyTypes).Invoke(EmptyObjects);

                        ret.Add(first);
                        for (var i = 1; i < asList.Count; i++)
                        {
                            var mapped = itemMapper.GetMapper()(asList[i]);
                            ret.Add(mapped);
                        }

                        return ret;
                    }
                );
        }

        public static POCOBuilder GetDictionaryMapper()
        {
            var genArgs = typeof(From).GetGenericArguments();

            var keyType = genArgs[0];
            var valType = genArgs[1];

            var keyMapper = (POCOBuilder)(typeof(POCOBuilder<>).MakeGenericType(keyType).GetMethod("GetMapper").Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public }));
            var valMapper = (POCOBuilder)(typeof(POCOBuilder<>).MakeGenericType(valType).GetMethod("GetMapper").Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public }));

            return
                new POCOBuilder(
                    x =>
                    {
                        var asDict = x as IDictionary;

                        // Don't waste my time
                        if (asDict == null || asDict.Count == 0) return null;

                        var e = asDict.Keys.GetEnumerator();
                        e.MoveNext();

                        var firstKey = e.Current;
                        var firstVal = asDict[firstKey];

                        var fkMapped = keyMapper.GetMapper()(firstKey);
                        var fvMapped = valMapper.GetMapper()(firstVal);

                        var ret = (IDictionary)(typeof(Dictionary<,>).MakeGenericType(fkMapped.GetType(), fvMapped.GetType()).GetConstructor(EmptyTypes).Invoke(EmptyObjects));

                        ret.Add(fkMapped, fvMapped);

                        while (e.MoveNext())
                        {
                            var mappedKey = keyMapper.GetMapper()(e.Current);
                            var mappedValue = valMapper.GetMapper()(asDict[e.Current]);

                            ret.Add(mappedKey, mappedValue);
                        }

                        return ret;
                    }
                );
        }

        public static POCOBuilder GetMapper(IncludedMembers members, IncludedVisibility visibility)
        {
            if (members != (IncludedMembers.Fields | IncludedMembers.Properties)) throw new NotSupportedException("members must be Fields | Properties");
            if (visibility != IncludedVisibility.Public) throw new NotSupportedException("visibility must be Public");

            return Builder ?? PromisedBuilder;
        }
    }
}
