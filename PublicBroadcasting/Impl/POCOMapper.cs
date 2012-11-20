﻿using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            bool toToArray = false;
            Type toListType, fromListType;

            if (typeof(To).IsArray)
            {
                toListType = typeof(To).GetElementType();
                toToArray = true;
            }
            else
            {
                toListType = typeof(To).GetGenericArguments()[0];
            }

            if (typeof(From).IsArray)
            {
                fromListType = typeof(From).GetElementType();
            }
            else
            {
                fromListType = typeof(From).GetGenericArguments()[0];
            }

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

                        if (toToArray)
                        {
                            return ret.ToArray(toListType);
                        }

                        return ret;
                    }
                );
        }

        private static POCOMapper GetClassMapper()
        {
            var tFrom = typeof(From);
            var tTo = typeof(To);

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

            Func<object> allocT;

            if (tTo.IsValueType)
            {
                allocT = () => { return Activator.CreateInstance(tTo); };
            }
            else
            {
                var cons = tTo.GetConstructor(new Type[0]);

                if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

                allocT = () => { return cons.Invoke(new object[0]); };
            }

            var fromType = TypeAccessor.Create(tFrom);

            Func<object, object> retFunc =
                x =>
                {
                    if (x == null) return null;

                    var ret = allocT();

                    foreach (var mem in members)
                    {
                        var memKey = mem.Key;
                        var memVal = mem.Value;

                        if (memVal == null) continue;

                        var from = fromType[x, memKey];

                        var fromMapped = memVal.GetMapper()(from);

                        var toMember = tTo.GetMember(memKey, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m is FieldInfo || m is PropertyInfo).Single();

                        if (toMember is FieldInfo)
                        {
                            ((FieldInfo)toMember).SetValue(ret, fromMapped);
                        }
                        else
                        {
                            ((PropertyInfo)toMember).SetValue(ret, fromMapped);
                        }
                    }

                    return (To)ret;
                };

            return new POCOMapper(retFunc);
        }

        private static POCOMapper GetAnonymouseClassMapper()
        {
            var tFrom = typeof(From);
            var tTo = typeof(To);

            Dictionary<string, Tuple<FieldInfo, POCOMapper>> members = null;

            var toFields = tTo.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

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

                        var mapperRet = (POCOMapper)mapper.GetMethod("Get").Invoke(null, new object[0]);

                        // OH MY GOD THIS IS A GIANT HACK
                        //  it looks like anonymous type properties always have a backing field that contains <PropName> in their name
                        //  note that that isn't a legal C# name, so collision is basically impossible.
                        //  However, if that behavior changes... oy.
                        var field = toFields.Single(f => f.Name.Contains("<" + s.Name + ">"));

                        return Tuple.Create(field, mapperRet);
                    }
                ).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            var cons = tTo.GetConstructors().Single();

            var consParams = cons.GetParameters();
            var consParamsDefaults = new object[consParams.Length];

            for (var i = 0; i < consParams.Length; i++)
            {
                var type = consParams[i].ParameterType;
                consParamsDefaults[i] = type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            var fromType = TypeAccessor.Create(tFrom);

            Func<object, object> retFunc =
                x =>
                {
                    if (x == null) return null;

                    var ret = (To)cons.Invoke(consParamsDefaults);

                    foreach (var mem in members)
                    {
                        var memKey = mem.Key;
                        var memVal = mem.Value;

                        if (memVal == null) continue;

                        var from = fromType[x, memKey];

                        var toField = memVal.Item1;
                        var mapper = memVal.Item2.GetMapper();

                        var fromMapped = mapper(from);

                        toField.SetValue(ret, fromMapped);
                    }

                    return ret;
                };

            return new POCOMapper(retFunc);
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

            if (IsAnonymouseClass(tTo))
            {
                return GetAnonymouseClassMapper();
            }

            return GetClassMapper();
        }

        private static bool IsOverride(MethodInfo method)
        {
            return method.GetBaseDefinition() != method;
        }

        /// <summary>
        /// HACK: This is a best effort attempt to divine if a type is anonymous based on the language spec.
        /// 
        /// Reference section 7.6.10.6 of the C# language spec as of 2012/11/19
        /// 
        /// It checks:
        ///     - is a class
        ///     - descends directly from object
        ///     - has [CompilerGenerated]
        ///     - has a single constructor
        ///     - that constructor takes exactly the same parameters as its public properties
        ///     - all public properties are not writable
        ///     - has a private field for every public property
        ///     - overrides Equals(object)
        ///     - overrides GetHashCode()
        /// </summary>
        private static bool IsAnonymouseClass(Type type) // don't fix the typo, it's fitting.
        {
            if (type.IsValueType) return false;
            if (type.BaseType != typeof(object)) return false;
            if (!Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))) return false;

            var allCons = type.GetConstructors();
            if (allCons.Length != 1) return false;

            var cons = allCons[0];
            if(!cons.IsPublic) return false;

            var props = type.GetProperties();
            if (props.Any(p => p.CanWrite)) return false;

            var propTypes = props.Select(t => t.PropertyType).ToList();

            foreach (var param in cons.GetParameters())
            {
                if (!propTypes.Contains(param.ParameterType)) return false;

                propTypes.Remove(param.ParameterType);
            }

            if (propTypes.Count != 0) return false;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            if (fields.Any(f => !f.IsPrivate)) return false;

            propTypes = props.Select(t => t.PropertyType).ToList();
            foreach (var field in fields)
            {
                if (!propTypes.Contains(field.FieldType)) return false;

                propTypes.Remove(field.FieldType);
            }

            if (propTypes.Count != 0) return false;

            var equals = type.GetMethod("Equals",new Type[] { typeof(object) });
            var hashCode = type.GetMethod("GetHashCode", new Type[0]);

            if (!IsOverride(equals) || !IsOverride(hashCode)) return false;

            return true;
        }

        public static POCOMapper Get()
        {
            return Mapper ?? PromisedMapper;
        }
    }
}
