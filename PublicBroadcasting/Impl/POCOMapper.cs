using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            var toDict = typeof(To);
            var fromDict = typeof(From);

            if (!(toDict.IsGenericType && toDict.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                toDict = toDict.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            }

            if(!(fromDict.IsGenericType && fromDict.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                fromDict = fromDict.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            }
            
            var toKeyType = toDict.GetGenericArguments()[0];
            var toValType = toDict.GetGenericArguments()[1];

            var fromKeyType = fromDict.GetGenericArguments()[0];
            var fromValType = fromDict.GetGenericArguments()[1];

            var keyMapper = typeof(POCOMapper<,>).MakeGenericType(fromKeyType, toKeyType).GetMethod("Get");
            var keyMap = (POCOMapper)keyMapper.Invoke(null, new object[0]);

            var valMapper = typeof(POCOMapper<,>).MakeGenericType(fromValType, toValType).GetMethod("Get");
            var valMap = (POCOMapper)valMapper.Invoke(null, new object[0]);

            var newDictType = typeof(To);

            if (newDictType.IsInterface)
            {
                newDictType = typeof(Dictionary<,>).MakeGenericType(toKeyType, toValType);
            }

            var newDictCons = newDictType.GetConstructor(new Type[0]);

            if (newDictCons == null)
            {
                throw new Exception(typeof(To).FullName + " doesn't have a parameterless constructor");
            }

            Func<IDictionary> newDictDyn;

            var dyn = new DynamicMethod("POCOMapper_NewDict_" + typeof(From).FullName + "_" + typeof(To).FullName, typeof(IDictionary), Type.EmptyTypes, restrictedSkipVisibility: true);
            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Newobj, newDictCons);   // [ret]
            il.Emit(OpCodes.Ret);                   // -----

            newDictDyn = (Func<IDictionary>)dyn.CreateDelegate(typeof(Func<IDictionary>));

            return
                new POCOMapper(
                    dictX =>
                    {
                        if (dictX == null) return null;

                        var keyFunc = keyMap.GetMapper();
                        var valFunc = valMap.GetMapper();

                        var ret = newDictDyn();

                        var asDict = (IDictionary)dictX;

                        var e = asDict.GetEnumerator();

                        while (e.MoveNext())
                        {
                            var entry = e.Entry;

                            var mKey = keyFunc(entry.Key);
                            var mVal = valFunc(entry.Value);

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
                var toList = typeof(To);

                if (!(toList.IsGenericType && toList.GetGenericTypeDefinition() == typeof(IList<>)))
                {
                    toList = toList.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
                }

                toListType = toList.GetGenericArguments()[0];
            }

            if (typeof(From).IsArray)
            {
                fromListType = typeof(From).GetElementType();
            }
            else
            {
                var fromList = typeof(From);

                if (!(fromList.IsGenericType && fromList.GetGenericTypeDefinition() == typeof(IList<>)))
                {
                    fromList = fromList.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
                }

                fromListType = fromList.GetGenericArguments()[0];
            }

            var mapper = typeof(POCOMapper<,>).MakeGenericType(fromListType, toListType).GetMethod("Get");
            var map = (POCOMapper)mapper.Invoke(null, new object[0]);

            var newListType = typeof(To);

            if (newListType.IsInterface || toToArray)
            {
                newListType = typeof(List<>).MakeGenericType(toListType);
            }

            var newListCons = newListType.GetConstructor(new Type[0]);

            if (newListCons == null) throw new Exception(newListType.FullName + " has no parameterless constructor");

            Func<IList> newListDyn;

            var dyn = new DynamicMethod("POCOMapper_NewList_" + typeof(From).FullName + "_" + typeof(To).FullName, typeof(IList), Type.EmptyTypes, restrictedSkipVisibility: true);
            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Newobj, newListCons);   // [ret]
            il.Emit(OpCodes.Ret);                   // ----

            newListDyn = (Func<IList>)dyn.CreateDelegate(typeof(Func<IList>));

            return
                new POCOMapper(
                    listX =>
                    {
                        if (listX == null) return null;

                        var func = map.GetMapper();

                        var asList = (IList)listX;

                        var ret = newListDyn();

                        for (var i = 0; i < asList.Count; i++)
                        {
                            var o = asList[i];

                            var mapped = func(o);

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

        private static Func<object, object> Lookup(Dictionary<string, POCOMapper> lookup, string term)
        {
            return lookup[term].GetMapper();
        }

        private static Func<From, Dictionary<string, POCOMapper>, To> BuildRefRefTypeMapper(Dictionary<string, POCOMapper> members)
        {
            var tTo = typeof(To);
            var tFrom = typeof(From);

            var cons = tTo.GetConstructor(new Type[0]);
            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var lookup = typeof(POCOMapper<From, To>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);

            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var name = "POCOMapper_RefRef";
            name += "_" + typeof(From).FullName + "_" + typeof(To).FullName;

            var dynMethod = new DynamicMethod(name, typeof(To), new[] { tFrom, typeof(Dictionary<string, POCOMapper>) }, restrictedSkipVisibility: true);
            var il = dynMethod.GetILGenerator();
            var retLoc = il.DeclareLocal(tTo);

            il.Emit(OpCodes.Newobj, cons);                      // [ret]
            il.Emit(OpCodes.Stloc, retLoc);                     // ----

            foreach (var mem in members)
            {
                il.Emit(OpCodes.Ldloc, retLoc);                 // [ret]

                il.Emit(OpCodes.Ldarg_1);                       // [members] [ret]
                il.Emit(OpCodes.Ldstr, mem.Key);                // [memKey] [members] [ret]
                il.Emit(OpCodes.Call, lookup);                  // [Func<object, object>] [ret]

                il.Emit(OpCodes.Ldarg_0);                       // [from] [Func<object, object>] [ret]

                var fromField = tFrom.GetField(mem.Key);
                il.Emit(OpCodes.Ldfld, fromField);              // [fromVal] [Func<object, object>] [ret]

                if (fromField.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fromField.FieldType);// [fromVal] [Func<object, object>] [ret]
                }

                il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)] [ret]

                var toMember = tTo.GetMember(mem.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m is FieldInfo || m is PropertyInfo).Single();

                if (toMember is FieldInfo)
                {
                    var asField = (FieldInfo)toMember;

                    if (asField.FieldType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, asField.FieldType);  // [toVal] [ret]
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, asField.FieldType);  // [toVal] [ret]
                    }

                    il.Emit(OpCodes.Stfld, asField);                // ----
                }
                else
                {
                    var asProp = (PropertyInfo)toMember;

                    if (asProp.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, asProp.PropertyType);// [toVal] [ret]
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, asProp.PropertyType);    // [toVal] [ret]
                    }

                    il.Emit(OpCodes.Callvirt, asProp.SetMethod);        // ----
                }
            }

            il.Emit(OpCodes.Ldloc, retLoc);
            il.Emit(OpCodes.Ret);

            var func = (Func<From, Dictionary<string, POCOMapper>, To>)dynMethod.CreateDelegate(typeof(Func<From, Dictionary<string, POCOMapper>, To>));

            return func;
        }

        private static Func<From, Dictionary<string, POCOMapper>, To> BuildRefValueTypeMapper(Dictionary<string, POCOMapper> members)
        {
            var tTo = typeof(To);
            var tFrom = typeof(From);

            var lookup = typeof(POCOMapper<From, To>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);

            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var name = "POCOMapper_RefValue";
            name += "_" + typeof(From).FullName + "_" + typeof(To).FullName;

            var dynMethod = new DynamicMethod(name, typeof(To), new[] { tFrom, typeof(Dictionary<string, POCOMapper>) }, restrictedSkipVisibility: true);
            var il = dynMethod.GetILGenerator();
            var retLoc = il.DeclareLocal(tTo);

            il.Emit(OpCodes.Ldloca, retLoc);                    // [*ret]
            il.Emit(OpCodes.Initobj, tTo);                      // ----

            foreach (var mem in members)
            {
                il.Emit(OpCodes.Ldloca, retLoc);                 // [ret]

                il.Emit(OpCodes.Ldarg_1);                       // [members] [ret]
                il.Emit(OpCodes.Ldstr, mem.Key);                // [memKey] [members] [ret]
                il.Emit(OpCodes.Call, lookup);                  // [Func<object, object>] [ret]

                il.Emit(OpCodes.Ldarg_0);                       // [from] [Func<object, object>] [ret]

                var fromField = tFrom.GetField(mem.Key);
                il.Emit(OpCodes.Ldfld, fromField);              // [fromVal] [Func<object, object>] [ret]

                if (fromField.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fromField.FieldType);// [fromVal] [Func<object, object>] [ret]
                }

                il.Emit(OpCodes.Callvirt, invoke);                  // [toVal (as object)] [ret]

                var toMember = tTo.GetMember(mem.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m is FieldInfo || m is PropertyInfo).Single();

                if (toMember is FieldInfo)
                {
                    var asField = (FieldInfo)toMember;

                    if (asField.FieldType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, asField.FieldType);  // [toVal] [ret]
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, asField.FieldType);  // [toVal] [ret]
                    }

                    il.Emit(OpCodes.Stfld, asField);                // ----
                }
                else
                {
                    var asProp = (PropertyInfo)toMember;

                    if (asProp.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, asProp.PropertyType);// [toVal] [ret]
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, asProp.PropertyType);    // [toVal] [ret]
                    }

                    il.Emit(OpCodes.Call, asProp.SetMethod);        // ----
                }
            }

            il.Emit(OpCodes.Ldloc, retLoc);
            il.Emit(OpCodes.Ret);

            var func = (Func<From, Dictionary<string, POCOMapper>, To>)dynMethod.CreateDelegate(typeof(Func<From, Dictionary<string, POCOMapper>, To>));

            return func;
        }

        private static POCOMapper GetClassMapper()
        {
            var tFrom = typeof(From);
            var tTo = typeof(To);

            Dictionary<string, POCOMapper> members = null;

            members =
                tFrom
                .GetFields()
                .ToDictionary(
                    s => s.Name,
                    s =>
                    {
                        var fieldType = s.FieldType;

                        var to = tTo.GetMember(s.Name).Where(w => w is FieldInfo || w is PropertyInfo).SingleOrDefault();

                        if (to == null) return null;
                        if (to is PropertyInfo && !((PropertyInfo)to).CanWrite) return null;

                        var toPropType = to is FieldInfo ? (to as FieldInfo).FieldType : (to as PropertyInfo).PropertyType;

                        var mapper = typeof(POCOMapper<,>).MakeGenericType(fieldType, toPropType);

                        return (POCOMapper)mapper.GetMethod("Get").Invoke(null, new object[0]);
                    }
                ).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            Func<From, Dictionary<string, POCOMapper>, To> func;

            if (tFrom.IsValueType)
            {
                throw new Exception("From should *never* be a value type!");
            }
            else
            {
                if (!tTo.IsValueType)
                {
                    func = BuildRefRefTypeMapper(members);
                }
                else
                {
                    func = BuildRefValueTypeMapper(members);
                }
            }

            Func<object, object> retFunc =
                from =>
                {
                    if (from == null) return null;

                    var asFrom = (From)from;

                    var asTo = func(asFrom, members);

                    return (object)asTo;
                };

            return new POCOMapper(retFunc);
        }

        private static POCOMapper GetAnonymouseClassMapper()
        {
            var tFrom = typeof(From);
            var tTo = typeof(To);

            Dictionary<string, POCOMapper> members = null;

            members =
                tFrom
                .GetFields()
                .ToDictionary(
                    s => s.Name,
                    s =>
                    {
                        var fieldType = s.FieldType;

                        var to = tTo.GetMember(s.Name).Where(w => w is FieldInfo || w is PropertyInfo).SingleOrDefault();

                        if (to == null) return null;

                        var toPropType = to is FieldInfo ? (to as FieldInfo).FieldType : (to as PropertyInfo).PropertyType;

                        var mapper = typeof(POCOMapper<,>).MakeGenericType(fieldType, toPropType);

                        var mapperRet = (POCOMapper)mapper.GetMethod("Get").Invoke(null, new object[0]);

                        return mapperRet;
                    }
                ).Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);

            var cons = tTo.GetConstructors().Single();

            var consParams = cons.GetParameters();

            var dyn = new DynamicMethod("POCOMapperAnon_" + typeof(From).FullName + "_" + typeof(To).FullName, typeof(To), new[] { typeof(From), typeof(Dictionary<string, POCOMapper>) }, restrictedSkipVisibility: true);
            var il = dyn.GetILGenerator();

            var createInstance = typeof(Activator).GetMethod("CreateInstance", Type.EmptyTypes);
            var lookup = typeof(POCOMapper<From, To>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);
            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            for (var i = 0; i < consParams.Length; i++)
            {
                var type = consParams[i].ParameterType;

                // HACK: We're relying on anonymous types using their Property names as Constructor parameter names.
                //       This isn't guaranteed, so we may be boned randomly.
                var memKey = consParams[i].Name;

                il.Emit(OpCodes.Ldarg_1);                       // [members]
                il.Emit(OpCodes.Ldstr, memKey);                 // [memKey] [members]
                il.Emit(OpCodes.Call, lookup);                  // [Func<object, objet>]

                il.Emit(OpCodes.Ldarg_0);                       // [from] [Func<object, object>]

                var fromField = tFrom.GetField(memKey);
                il.Emit(OpCodes.Ldfld, fromField);              // [fromVal] [Func<object, object>]

                if (fromField.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fromField.FieldType);// [fromVal] [Func<object, object>]
                }

                il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)]

                if (type.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, type);           // [toVal]
                }
                else
                {
                    il.Emit(OpCodes.Castclass, type);           // [toVal]
                }
            }

            // Stack is: [params for consParams]
            il.Emit(OpCodes.Newobj, cons);                  // [ret]
            il.Emit(OpCodes.Ret);                           // ----

            var func = (Func<From, Dictionary<string, POCOMapper>, To>)dyn.CreateDelegate(typeof(Func<From, Dictionary<string, POCOMapper>, To>));

            Func<object, object> retFunc =
                x =>
                {
                    if (x == null) return x;

                    var from = (From)x;

                    var ret = func(from, members);

                    return (To)ret;
                };

            return new POCOMapper(retFunc);
        }

        internal static bool Widens(Type from, Type to, out Func<object, object> map)
        {
            if (!IsPrimitive(from) || !IsPrimitive(to) || from == to)
            {
                map = null;
                return false;
            }

            // Everything *but* SByte
            if(from == typeof(byte))
            {
                map = null;

                if (to == typeof(sbyte))
                {
                    return false;
                }

                if (to == typeof(short)) map = x => (short)((byte)x);
                if (to == typeof(ushort)) map = x => (ushort)((byte)x);
                if (to == typeof(int)) map = x => (int)((byte)x);
                if (to == typeof(uint)) map = x => (uint)((byte)x);
                if (to == typeof(long)) map = x => (long)((byte)x);
                if (to == typeof(ulong)) map = x => (ulong)((byte)x);
                if (to == typeof(float)) map = x => (float)((byte)x);
                if (to == typeof(double)) map = x => (double)((byte)x);
                if (to == typeof(decimal)) map = x => (decimal)((byte)x);

                return true;
            }
            
            // Short, Integer, Long, Decimal, Single, Double
            if(from == typeof(sbyte))
            {
                map = null;

                if (to == typeof(byte) || IsUnsigned(to))
                {
                    return false;
                }

                if (to == typeof(short)) map = x => (short)((sbyte)x);
                if (to == typeof(int)) map = x => (int)((sbyte)x);
                if (to == typeof(long)) map = x => (long)((sbyte)x);
                if (to == typeof(float)) map = x => (float)((sbyte)x);
                if (to == typeof(double)) map = x => (double)((sbyte)x);
                if (to == typeof(decimal)) map = x => (decimal)((sbyte)x);

                return true;
            }

            // Integer, Long, Decimal, Single, Double
            if(from == typeof(short))
            {
                map = null;

                if (to == typeof(sbyte) || IsUnsigned(to))
                {
                    return false;
                }

                if (to == typeof(int)) map = x => (int)((short)x);
                if (to == typeof(long)) map = x => (long)((short)x);
                if (to == typeof(float)) map = x => (float)((short)x);
                if (to == typeof(double)) map = x => (double)((short)x);
                if (to == typeof(decimal)) map = x => (decimal)((short)x);

                return true;
            }

            // Integer, UInteger, Long, ULong, Decimal, Single, Double
            if(from == typeof(ushort))
            {
                map = null;

                if (to == typeof(byte) || to == typeof(sbyte) || to == typeof(short))
                {
                    return false;
                }

                if (to == typeof(int)) map = x => (int)((ushort)x);
                if (to == typeof(uint)) map = x => (uint)((ushort)x);
                if (to == typeof(long)) map = x => (long)((ushort)x);
                if (to == typeof(ulong)) map = x => (ulong)((ushort)x);
                if (to == typeof(float)) map = x => (float)((ushort)x);
                if (to == typeof(double)) map = x => (double)((ushort)x);
                if (to == typeof(decimal)) map = x => (decimal)((ushort)x);

                return true;
            }

            // Long, Decimal, Single, Double
            if(from == typeof(int))
            {
                map = null;

                if (to == typeof(sbyte) || to == typeof(short) || IsUnsigned(to))
                {
                    return false;
                }

                if (to == typeof(long)) map = x => (long)((int)x);
                if (to == typeof(float)) map = x => (float)((int)x);
                if (to == typeof(double)) map = x => (double)((int)x);
                if (to == typeof(decimal)) map = x => (decimal)((int)x);

                return true;
            }

            // Long, ULong, Decimal, Single, Double
            if(from == typeof(uint))
            {
                map = null;

                if (to == typeof(byte) || to == typeof(sbyte) || to == typeof(short) || to == typeof(ushort) || to == typeof(int))
                {
                    return false;
                }

                if (to == typeof(long)) map = x => (long)((uint)x);
                if (to == typeof(ulong)) map = x => (ulong)((uint)x);
                if (to == typeof(float)) map = x => (float)((uint)x);
                if (to == typeof(double)) map = x => (double)((uint)x);
                if (to == typeof(decimal)) map = x => (decimal)((uint)x);

                return true;
            }

            // Decimal, Single, Double
            if(from == typeof(long))
            {
                map = null;

                if (to == typeof(sbyte) || to == typeof(short) || to == typeof(int) || IsUnsigned(to))
                {
                    return false;
                }

                if (to == typeof(float)) map = x => (float)((long)x);
                if (to == typeof(double)) map = x => (double)((long)x);
                if (to == typeof(decimal)) map = x => (decimal)((long)x);

                return true;
            }

            // Decimal, Single, Double
            if (from == typeof(ulong))
            {
                map = null;

                if (to == typeof(byte) || to == typeof(sbyte) || to == typeof(short) || to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long))
                {
                    return false;
                }

                if (to == typeof(float)) map = x => (float)((ulong)x);
                if (to == typeof(double)) map = x => (double)((ulong)x);
                if (to == typeof(decimal)) map = x => (decimal)((ulong)x);

                return true;
            }

            // Single, Double
            if(from == typeof(decimal))
            {
                map = null;

                if (to != typeof(float) && to != typeof(double))
                {
                    return false;
                }

                if (to == typeof(float)) map = x => (float)((decimal)x);
                if (to == typeof(double)) map = x => (double)((decimal)x);

                return true;
            }

            // Double
            if(from == typeof(float))
            {
                map = null;

                if (to != typeof(double))
                {
                    return false;
                }

                map = x => (double)((float)x);

                return true;
            }

            if (from == typeof(double))
            {
                map = null;

                return false;
            }

            throw new Exception("Shouldn't be possible, found "+from.FullName+" to "+to.FullName);
        }

        private static readonly HashSet<Type> PrimitiveTypes = 
            new HashSet<Type> 
            { 
                typeof(byte), typeof(sbyte), 
                typeof(short), typeof(ushort),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal)
            };
        private static bool IsPrimitive(Type t)
        {
            return PrimitiveTypes.Contains(t);
        }

        private static bool IsUnsigned(Type t)
        {
            return
                t == typeof(byte) ||
                t == typeof(ushort) ||
                t == typeof(uint) ||
                t == typeof(ulong);
        }

        private static POCOMapper GetClassToDictMapper(Type fromClass, Type toDict)
        {
            var dictI = toDict.GetDictionaryInterface();
            var valType = dictI.GetGenericArguments()[1];

            var dictCons = toDict.GetConstructor(Type.EmptyTypes);
            var dictAdd = toDict.GetMethod("Add");

            var createDictDyn = new DynamicMethod("POCOMapper_GetClassToDictMapper_" + fromClass.FullName + "_" + toDict.FullName + "_createDict", typeof(object), Type.EmptyTypes, restrictedSkipVisibility: true);
            var il = createDictDyn.GetILGenerator();

            il.Emit(OpCodes.Newobj, dictCons);    // [ret]
            il.Emit(OpCodes.Ret);               // -----

            var createDict = (Func<object>)createDictDyn.CreateDelegate(typeof(Func<object>));

            var dictBuilder = new Dictionary<string, Tuple<Func<object, object>, Func<object, object>>>();

            foreach (var mem in fromClass.GetFields())
            {
                Func<object, object> get, map;

                if (mem.FieldType != valType)
                {
                    if (valType == typeof(object))
                    {
                        map = null;
                    }
                    else
                    {
                        var mapper = (POCOMapper)((typeof(POCOMapper<,>).MakeGenericType(mem.FieldType, valType)).GetMethod("Get").Invoke(null, new object[0]));
                        map = mapper.GetMapper();
                    }
                }
                else
                {
                    map = null;
                }

                var dynGet = new DynamicMethod("POCOMapper_GetClassToDictMapper_" + fromClass.FullName + "_" + toDict.FullName + "_" + mem.Name + "_get", typeof(object), new[] { typeof(object) }, restrictedSkipVisibility: true);
                il = dynGet.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);               // [obj]
                il.Emit(OpCodes.Castclass, fromClass);  // [obj]
                il.Emit(OpCodes.Ldfld, mem);            // [field]
                if (mem.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, mem.FieldType);// [field]
                }
                il.Emit(OpCodes.Ret);                   // -----

                get = (Func<object, object>)dynGet.CreateDelegate(typeof(Func<object, object>));

                dictBuilder[mem.Name] = Tuple.Create(get, map);
            }

            return
                new POCOMapper(
                    obj =>
                    {
                        var dict = createDict();

                        foreach (var kv in dictBuilder)
                        {
                            var toMap = kv.Value.Item1(obj);
                            var mapped = kv.Value.Item2 != null ? kv.Value.Item2(toMap) : toMap;

                            ((IDictionary)dict).Add(kv.Key, mapped);
                        }

                        return dict;
                    }
                );
        }

        private static POCOMapper GetMapper()
        {
            var tFrom = typeof(From);
            var tTo = typeof(To);

            var fIsNullable = Nullable.GetUnderlyingType(tFrom) != null;
            var tIsNullable = Nullable.GetUnderlyingType(tTo) != null;

            if (tFrom == tTo)
            {
                return new POCOMapper(x => (To)x);
            }

            if (tFrom == typeof(string))
            {
                if (tTo.IsEnum)
                {
                    return
                        new POCOMapper(
                            x =>
                            {
                                if (x == null)
                                {
                                    return Activator.CreateInstance(tTo);
                                }

                                return Enum.Parse(tTo, x.ToString());
                            }
                        );
                }

                if (tIsNullable)
                {
                    var tNonNull = Nullable.GetUnderlyingType(tTo);
                    if (tNonNull.IsEnum)
                    {
                        return
                            new POCOMapper(
                                x =>
                                {
                                    if (x == null) return null;

                                    return Enum.Parse(tNonNull, x.ToString());
                                }
                            );
                    }
                }
            }

            if (tFrom.IsEnum)
            {
                if (tTo == typeof(string))
                {
                    return
                        new POCOMapper(
                            x =>
                            {
                                return x.ToString();
                            }
                        );
                }

                if (tTo.IsEnum)
                {
                    return
                        new POCOMapper(
                            x =>
                            {
                                if (x == null || ((int)x) == -1) return null;

                                var asStr = x.ToString();

                                var enumRet = Enum.Parse(tTo, asStr, ignoreCase: true);

                                return (To)enumRet;
                            }
                        );
                }

                if (!tIsNullable)
                {
                    throw new Exception("Enumerations can be mapped to strings or other enumerations, found " + tTo.FullName);
                }
            }

            if (tFrom.IsValueType && tTo.IsValueType)
            {
                Func<object, object> widenRet;
                if (Widens(tFrom, tTo, out widenRet))
                {
                    if (widenRet == null) throw new Exception("No widening mapper for " + tFrom.FullName + " to " + tTo.FullName);

                    return new POCOMapper(widenRet);
                }
                else
                {
                    if (IsPrimitive(tFrom) && IsPrimitive(tTo))
                    {
                        throw new Exception("Illegal narrowing conversion from " + tFrom.FullName + " to " + tTo.FullName);
                    }
                }
            }

            if (fIsNullable && tIsNullable)
            {
                var fNonNull = Nullable.GetUnderlyingType(tFrom);
                var tNonNull = Nullable.GetUnderlyingType(tTo);

                var nonNullMapper = typeof(POCOMapper<,>).MakeGenericType(fNonNull, tNonNull);
                var nonNullFunc = (POCOMapper)nonNullMapper.GetMethod("Get").Invoke(null, new object[0]);

                if (!tNonNull.IsEnum)
                {
                    return
                        new POCOMapper(
                            from =>
                            {
                                if (from == null) return null;

                                var ret = nonNullFunc.GetMapper()(from);

                                return ret;
                            }
                        );
                }
                else
                {
                    return
                        new POCOMapper(
                            from =>
                            {
                                if (from == null) return null;

                                var ret = nonNullFunc.GetMapper()(from);

                                var asTo = (To)ret;

                                return asTo;
                            }
                        );
                }
            }

            if (fIsNullable && !tIsNullable)
            {
                var fNonNull = Nullable.GetUnderlyingType(tFrom);

                var nullMapper = typeof(POCOMapper<,>).MakeGenericType(fNonNull, tTo);
                var nullFunc = (POCOMapper)nullMapper.GetMethod("Get").Invoke(null, new object[0]);

                if (tTo.IsValueType)
                {
                    return
                        new POCOMapper(
                            from =>
                            {
                                if (from == null)
                                {
                                    return Activator.CreateInstance(tTo);
                                }

                                return nullFunc.GetMapper()(from);
                            }
                        );
                }

                return
                    new POCOMapper(
                        from =>
                        {
                            if (from == null)
                            {
                                return null;
                            }

                            return nullFunc.GetMapper()(from);
                        }
                    );
            }

            if (!fIsNullable && tIsNullable)
            {
                var tNonNull = Nullable.GetUnderlyingType(tTo);

                var nonNullMapper = typeof(POCOMapper<,>).MakeGenericType(tFrom, tNonNull);
                var nonNullFunc = (POCOMapper)nonNullMapper.GetMethod("Get").Invoke(null, new object[0]);

                // Since we map enums to ints, we need some extra magic when dealing with nullable enums
                if (tNonNull.IsEnum)
                {
                    return
                        new POCOMapper(
                            from =>
                            {
                                if (from == null) return null;

                                var ret = nonNullFunc.GetMapper()(from);

                                return (To)ret;
                            }
                        );
                }

                return
                    new POCOMapper(
                        from =>
                        {
                            var ret = nonNullFunc.GetMapper()(from);

                            return (To)ret;
                        }
                    );
            }

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization target, expected an IDictionary<Key, Value>");
                }

                return GetDictionaryMapper();
            }

            if ((tFrom.IsGenericType && tFrom.GetGenericTypeDefinition() == typeof(IList<>)) ||
               tFrom.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                if (!((tTo.IsGenericType && tTo.GetGenericTypeDefinition() == typeof(IList<>)) ||
                    tTo.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))))
                {
                    throw new Exception(tTo.FullName + " is not a valid deserialization target, expected an IList<T>");
                }

                return GetListMapper();
            }

            if (tTo.IsAnonymouseClass())
            {
                return GetAnonymouseClassMapper();
            }

            if (tTo.IsDictionary() && tFrom.IsMappableToDictionary(tTo))
            {
                return GetClassToDictMapper(tFrom, tTo);
            }

            return GetClassMapper();
        }

        public static POCOMapper Get()
        {
            return Mapper ?? PromisedMapper;
        }
    }
}
