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
    internal class POCOMapper
    {
        protected Func<object, object> Mapper { get; set; }

        protected POCOMapper() { }

        internal POCOMapper(Func<object, object> map)
        {
            Mapper = map;
        }

        internal Func<object, object> GetMapper()
        {
            return Mapper;
        }
    }

    internal class PromisedPOCOMapper : POCOMapper
    {
        internal PromisedPOCOMapper() { }

        internal void Fulfil(Func<object, object> mapper)
        {
            Mapper = mapper;
        }
    }

    internal class POCOMapper<From, To>
    {
        private static readonly POCOMapper Mapper;
        private static readonly PromisedPOCOMapper PromisedMapper;

        static POCOMapper()
        {
            PromisedMapper = new PromisedPOCOMapper();

            var mapper = GetMapper();
            PromisedMapper.Fulfil(mapper.GetMapper());

            Mapper = mapper;
        }

        private static POCOMapper GetDictionaryMapper()
        {
            var toKeyType = typeof(To).GetGenericArguments()[0];
            var toValType = typeof(To).GetGenericArguments()[1];

            var fromKeyType = typeof(From).GetGenericArguments()[0];
            var fromValType = typeof(From).GetGenericArguments()[1];

            var keyMapper = typeof(POCOMapper<,>).MakeGenericType(fromKeyType, toKeyType).GetMethod("Get");
            var keyMap = (POCOMapper)keyMapper.Invoke(null, new object[0]);

            var valMapper = typeof(POCOMapper<,>).MakeGenericType(fromValType, toValType).GetMethod("Get");
            var valMap = (POCOMapper)valMapper.Invoke(null, new object[0]);

            var newDictType = typeof(Dictionary<,>).MakeGenericType(toKeyType, toValType);
            var newDictCons = newDictType.GetConstructor(new Type[0]);

            return
                new POCOMapper(
                    dictX =>
                    {
                        if (dictX == null) return null;

                        var ret = (IDictionary)newDictCons.Invoke(new object[0]);

                        var asDict = (IDictionary)dictX;

                        var e = asDict.Keys.GetEnumerator();

                        while (e.MoveNext())
                        {
                            var key = e.Current;
                            var mKey = keyMap.GetMapper()(key);

                            var val = asDict[key];
                            var mVal = valMap.GetMapper()(val);

                            ret.Add(mKey, mVal);
                        }

                        return ret;
                    }
                );
        }

        private static POCOMapper GetListMapper()
        {
            var toListType = typeof(To).GetGenericArguments()[0];
            var fromListType = typeof(From).GetGenericArguments()[0];

            var mapper = typeof(POCOMapper<,>).MakeGenericType(fromListType, toListType).GetMethod("Get");
            var map = (POCOMapper)mapper.Invoke(null, new object[0]);

            var newListType = typeof(List<>).MakeGenericType(toListType);
            var newListCons = newListType.GetConstructor(new Type[0]);

            return
                new POCOMapper(
                    listX =>
                    {
                        if (listX == null) return null;

                        var ret = (IList)newListCons.Invoke(new object[0]);

                        var asEnum = (IEnumerable)listX;

                        foreach (var o in asEnum)
                        {
                            var mapped = map.GetMapper()(o);

                            ret.Add(mapped);
                        }

                        return ret;
                    }
                );
        }

        private static POCOMapper GetMapper()
        {
            if (typeof(From) == typeof(To))
            {
                return new POCOMapper(x => (To)x);
            }

            var tFrom = typeof(From);
            var tTo = typeof(To);

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization, expected a dictionary");
                }

                return GetDictionaryMapper();
            }

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IList<>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IList<>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization, expected a list");
                }

                return GetListMapper();
            }

            Dictionary<string, POCOMapper> members = null;

            members =
                tFrom
                .GetProperties()
                .ToDictionary(
                    s => s.Name,
                    s =>
                    {
                        var propType = s.PropertyType;

                        var to = tTo.GetMember(s.Name).Where(w => w is FieldInfo || w is PropertyInfo).SingleOrDefault();

                        if (to == null) return null;

                        var toPropType = to is FieldInfo ? (to as FieldInfo).FieldType : (to as PropertyInfo).PropertyType;

                        var mapper = typeof(POCOMapper<,>).MakeGenericType(propType, toPropType);

                        return (POCOMapper)mapper.GetMethod("Get").Invoke(null, new object[0]);
                    }
                ).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            var cons = tTo.GetConstructor(new Type[0]);

            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var fromType = TypeAccessor.Create(tFrom);
            var toType = TypeAccessor.Create(tTo);

            Func<object, object> retFunc =
                x =>
                {
                    if (x == null) return null;

                    var ret = (To)cons.Invoke(new object[0]);

                    foreach (var mem in members)
                    {
                        var memKey = mem.Key;
                        var memVal = mem.Value;

                        if (memVal == null) continue;

                        var from = fromType[x, memKey];

                        var fromMapped = memVal.GetMapper()(from);

                        toType[ret, memKey] = fromMapped;
                    }

                    return ret;
                };

            return new POCOMapper(retFunc);
        }

        public static POCOMapper Get()
        {
            return Mapper ?? PromisedMapper;
        }
    }
}
