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
    internal class POCOMapper<To>
    {
        public static Func<object, object> GetDictionaryMapper(Type tFrom)
        {
            throw new NotImplementedException();
        }

        public static Func<object, object> GetListMapper(Type tFrom)
        {
            var toListType = typeof(To).GetGenericArguments()[0];
            var fromListType = tFrom.GetGenericArguments()[0];

            var mapper = typeof(POCOMapper<>).MakeGenericType(toListType).GetMethod("GetMapper");
            var map = (Func<object, object>)mapper.Invoke(null, new object[] { fromListType });


            var newListType = typeof(List<>).MakeGenericType(toListType);
            var newListCons = newListType.GetConstructor(new Type[0]);

            var newListAdd = newListType.GetMethod("Add");

            return
                listX =>
                {
                    var ret = (IList)newListCons.Invoke(new object[0]);

                    var asEnum = (IEnumerable)listX;

                    foreach (var o in asEnum)
                    {
                        ret.Add(map(o));
                    }

                    return ret;
                };
        }

        private static bool Trivial<V>(Type toType, Type fromType, out Func<object, object> convert)
        {
            if (fromType == typeof(V))
            {
                if (toType != typeof(V)) throw new Exception("Type mismatch, expected " + typeof(V));

                convert = x => (V)x;

                return true;
            }

            convert = null;

            return false;
        }

        public static Func<object, object> GetMapper(Type tFrom)
        {
            var tTo = typeof(To);

            Func<object, object> simpleRet;
            if (Trivial<long>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<ulong>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<int>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<uint>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<short>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<ushort>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<byte>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<sbyte>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<char>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<string>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<double>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<float>(tTo, tFrom, out simpleRet)) return simpleRet;
            if (Trivial<decimal>(tTo, tFrom, out simpleRet)) return simpleRet;

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization, expected a dictionary");
                }

                return GetDictionaryMapper(tFrom);
            }

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IList<>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IList<>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization, expected a list");
                }

                return GetListMapper(tFrom);
            }

            var members =
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

                        var mapper = typeof(POCOMapper<>).MakeGenericType(toPropType);

                        return (Func<object, object>)mapper.GetMethod("GetMapper").Invoke(null, new object[] { propType });
                    }
                ).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            var cons = tTo.GetConstructor(new Type[0]);

            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var fromType = TypeAccessor.Create(tFrom);
            var toType = TypeAccessor.Create(tTo);

            return
                x =>
                {
                    var ret = (To)cons.Invoke(new object[0]);

                    foreach (var mem in members)
                    {
                        var memName = mem.Key;
                        var memMap = mem.Value;

                        toType[ret, memName] = memMap(fromType[x, memName]);
                    }

                    return ret;
                };
        }
    }
}
