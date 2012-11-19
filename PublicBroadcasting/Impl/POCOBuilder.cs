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
    internal class POCOBuilder<From>
    {
        private static readonly Type[] EmptyTypes = new Type[0];
        private static readonly object[] EmptyObjects = new object[0];

        static POCOBuilder()
        {
            Debug.WriteLine("POCOBuilder: " + typeof(From).FullName);
        }

        private static Func<object, object> GetListMapper(IncludedMembers members, IncludedVisibility visibility, Dictionary<Type, Action<Func<object, object>>> inProgress = null)
        {
            var fromListType = typeof(From).GetGenericArguments()[0];

            var itemMapper = (Func<object, object>)(typeof(POCOBuilder<>).MakeGenericType(fromListType).GetMethod("GetMapper").Invoke(null, new object[] { members, visibility, inProgress }));

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

        public static Func<object, object> GetDictionaryMapper(IncludedMembers members, IncludedVisibility visibility, Dictionary<Type, Action<Func<object, object>>> inProgress = null)
        {
            var genArgs = typeof(From).GetGenericArguments();

            var keyType = genArgs[0];
            var valType = genArgs[1];

            var keyMapper = (Func<object, object>)(typeof(POCOBuilder<>).MakeGenericType(keyType).GetMethod("GetMapper").Invoke(null, new object[] { members, visibility, inProgress }));
            var valMapper = (Func<object, object>)(typeof(POCOBuilder<>).MakeGenericType(valType).GetMethod("GetMapper").Invoke(null, new object[] { members, visibility, inProgress }));

            return
                x =>
                {
                    var asDict = x as IDictionary;

                    // Don't waste my time
                    if (asDict == null || asDict.Count == 0) return null;

                    var e = asDict.Keys.GetEnumerator();
                    e.MoveNext();

                    var firstKey = e.Current;
                    var firstVal = asDict[firstKey];

                    var fkMapped = keyMapper(firstKey);
                    var fvMapped = valMapper(firstVal);

                    var ret = (IDictionary)(typeof(Dictionary<,>).MakeGenericType(fkMapped.GetType(), fvMapped.GetType()).GetConstructor(EmptyTypes).Invoke(EmptyObjects));

                    ret.Add(fkMapped, fvMapped);

                    while (e.MoveNext())
                    {
                        var mappedKey = keyMapper(e.Current);
                        var mappedValue = valMapper(asDict[e.Current]);

                        ret.Add(mappedKey, mappedValue);
                    }

                    return ret;
                };
        }

        public static Func<object, object> GetMapper(IncludedMembers members, IncludedVisibility visibility, Dictionary<Type, Action<Func<object, object>>> inProgress = null)
        {
            inProgress = inProgress ?? new Dictionary<Type, Action<Func<object, object>>>();

            const string SelfName = "GetMapper";

            if (members != (IncludedMembers.Fields | IncludedMembers.Properties)) throw new NotSupportedException("members must be Fields | Properties");
            if (visibility != IncludedVisibility.Public) throw new NotSupportedException("visibility must be Public");

            var t = typeof(From);
            var desc = Describer<From>.Get();
            var pocoType = desc.GetPocoType();

            if (desc is ListTypeDescription)
            {
                return GetListMapper(members, visibility, inProgress);
            }

            if (desc is DictionaryTypeDescription)
            {
                return GetDictionaryMapper(members, visibility, inProgress);
            }

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

            inProgress[t] = x => { };

            var newPoco = pocoType.GetConstructor(EmptyTypes);

            Dictionary<string, Func<object, object>> propMaps = null;

            propMaps = 
                asClass.Members.ToDictionary(
                    kv => kv.Key, 
                    kv => 
                    {
                        var member = t.GetMember(kv.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

                        var type = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;

                        if (inProgress.ContainsKey(type))
                        {
                            var cur = inProgress[type];

                            inProgress[type] =
                                newMap =>
                                {
                                    propMaps[kv.Key] = newMap;

                                    if (cur != null) cur(newMap);
                                };

                            return null;
                        }

                        var self = typeof(POCOBuilder<>).MakeGenericType(type).GetMethod(SelfName);

                        return (Func<object,object>)self.Invoke(null, new object[] { members, visibility, inProgress });
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

                        var toVal = propMaps[member.Key](fromVal);

                        to[ret, member.Key] = toVal;
                    }

                    return ret;
                };

            inProgress[t](mapFuncRet);
            inProgress[t] = null;

            return mapFuncRet;
        }
    }
}
