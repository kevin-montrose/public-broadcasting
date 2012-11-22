using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

    internal class POCOBuilder<From, Describer>
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
            var desc = (TypeDescription)typeof(Describer).GetMethod("GetForUse").Invoke(null, new object[] { false });

            if (desc is ListTypeDescription)
            {
                return GetListMapper();
            }

            if (desc is DictionaryTypeDescription)
            {
                return GetDictionaryMapper();
            }

            if (desc is NullableTypeDescription)
            {
                return new POCOBuilder(x => x);
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
                if (desc == SimpleTypeDescription.String) return new POCOBuilder(x => (string)x);
                if (desc == SimpleTypeDescription.Bool) return new POCOBuilder(x => Convert.ToBoolean(x));
                if (desc == SimpleTypeDescription.DateTime) return new POCOBuilder(x => Convert.ToDateTime(x));
                if (desc == SimpleTypeDescription.TimeSpan) return new POCOBuilder(x => (TimeSpan)x);
                if (desc == SimpleTypeDescription.Guid) return new POCOBuilder(x => (Guid)x);
                if (desc == SimpleTypeDescription.Uri) return new POCOBuilder(x => (Uri)x);

                throw new Exception("Shouldn't be possible, found " + desc);
            }

            return GetClassMapper(desc);
        }

        private static Func<object, object> Lookup(Dictionary<string, POCOBuilder> lookup, string term)
        {
            var ret = lookup[term];

            return ret.GetMapper();
        }

        private static Func<From, Dictionary<string, POCOBuilder>, object> BuildRefRefTypeMapper(Dictionary<string, POCOBuilder> members, Type tTo)
        {
            var tFrom = typeof(From);

            var cons = tTo.GetConstructor(new Type[0]);
            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var lookup = typeof(POCOBuilder<From, Describer>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);

            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var dynMethod = new DynamicMethod("POCOBuilder" + Guid.NewGuid().ToString().Replace("-", ""), typeof(object), new[] { tFrom, typeof(Dictionary<string, POCOBuilder>) }, restrictedSkipVisibility: true);
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

                var fromMember = tFrom.GetMember(mem.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m is FieldInfo || m is PropertyInfo).Single();

                if (fromMember is PropertyInfo)
                {
                    var fromProp = (PropertyInfo)fromMember;
                    il.Emit(OpCodes.Callvirt, fromProp.GetMethod);  // [fromVal] [Func<object, object>] [ret]

                    if (fromProp.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, fromProp.PropertyType);// [fromVal] [Func<object, object>] [ret]
                    }

                    il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)] [ret]
                }
                else
                {
                    var fromField = (FieldInfo)fromMember;
                    il.Emit(OpCodes.Ldfld, fromField);              // [fromVal] [Func<object, object>] [ret]

                    if (fromField.FieldType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, fromField.FieldType);  // [fromVal] [Func<object, object>] [ret]
                    }

                    il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)] [ret]
                }

                var toMember = tTo.GetProperty(mem.Key);

                if (toMember.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, toMember.PropertyType);// [toVal] [ret]
                }
                else
                {
                    il.Emit(OpCodes.Castclass, toMember.PropertyType);    // [toVal] [ret]
                }

                il.Emit(OpCodes.Callvirt, toMember.SetMethod);        // ----
            }

            il.Emit(OpCodes.Ldloc, retLoc);
            il.Emit(OpCodes.Ret);

            var func = (Func<From, Dictionary<string, POCOBuilder>, object>)dynMethod.CreateDelegate(typeof(Func<From, Dictionary<string, POCOBuilder>, object>));

            return func;
        }

        private static Func<From, Dictionary<string, POCOBuilder>, object> BuildValueRefTypeMapper(Dictionary<string, POCOBuilder> members, Type tTo)
        {
            var tFrom = typeof(From);

            var cons = tTo.GetConstructor(new Type[0]);
            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var lookup = typeof(POCOBuilder<From, Describer>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);

            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var dynMethod = new DynamicMethod("POCOBuilder" + Guid.NewGuid().ToString().Replace("-", ""), typeof(object), new[] { tFrom, typeof(Dictionary<string, POCOBuilder>) }, restrictedSkipVisibility: true);
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

                il.Emit(OpCodes.Ldarga, 0);                       // [from] [Func<object, object>] [ret]

                var fromMember = tFrom.GetMember(mem.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m is FieldInfo || m is PropertyInfo).Single();

                if (fromMember is PropertyInfo)
                {
                    var fromProp = (PropertyInfo)fromMember;
                    il.Emit(OpCodes.Call, fromProp.GetMethod);  // [fromVal] [Func<object, object>] [ret]

                    if (fromProp.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, fromProp.PropertyType);// [fromVal] [Func<object, object>] [ret]
                    }

                    il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)] [ret]
                }
                else
                {
                    var fromField = (FieldInfo)fromMember;
                    il.Emit(OpCodes.Ldfld, fromField);              // [fromVal] [Func<object, object>] [ret]

                    if (fromField.FieldType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, fromField.FieldType);  // [fromVal] [Func<object, object>] [ret]
                    }

                    il.Emit(OpCodes.Call, invoke);                  // [toVal (as object)] [ret]
                }

                var toMember = tTo.GetProperty(mem.Key);

                if (toMember.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, toMember.PropertyType);// [toVal] [ret]
                }
                else
                {
                    il.Emit(OpCodes.Castclass, toMember.PropertyType);    // [toVal] [ret]
                }

                il.Emit(OpCodes.Callvirt, toMember.SetMethod);        // ----
            }

            il.Emit(OpCodes.Ldloc, retLoc);
            il.Emit(OpCodes.Ret);

            var func = (Func<From, Dictionary<string, POCOBuilder>, object>)dynMethod.CreateDelegate(typeof(Func<From, Dictionary<string, POCOBuilder>, object>));

            return func;
        }

        private static POCOBuilder GetClassMapper(TypeDescription desc)
        {
            var tFrom = typeof(From);

            var asClass = (ClassTypeDescription)desc;
            var pocoType = desc.GetPocoType();
            
            if(pocoType.IsValueType)
            {
                throw new Exception("POCO Type should never be a ValueType!");
            }

            Dictionary<string, POCOBuilder> members = null;

            members =
                asClass.Members.ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        var member = tFrom.GetMember(kv.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

                        var type = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;

                        var descType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(type);

                        var self = typeof(POCOBuilder<,>).MakeGenericType(type, descType).GetMethod("GetMapper");

                        return (POCOBuilder)self.Invoke(null, new object[0]);
                    }
                );

            Func<From, Dictionary<string, POCOBuilder>, object> func;
            if (!tFrom.IsValueType)
            {
                func = BuildRefRefTypeMapper(members, pocoType);
            }
            else
            {
                func = BuildValueRefTypeMapper(members, pocoType);
            }

            return
                new POCOBuilder(
                    from =>
                    {
                        if (from == null) return null;

                        var asFrom = (From)from;

                        return func(asFrom, members);
                    }
                );

            /*var from = TypeAccessor.Create(t, true);
            var to = TypeAccessor.Create(pocoType, true);

            var newPoco = pocoType.GetConstructor(EmptyTypes);

            Dictionary<string, POCOBuilder> propMaps = null;

            propMaps =
                asClass.Members.ToDictionary(
                    kv => kv.Key,
                    kv =>
                    {
                        var member = t.GetMember(kv.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

                        var type = member is FieldInfo ? (member as FieldInfo).FieldType : (member as PropertyInfo).PropertyType;

                        var descType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(type);

                        var self = typeof(POCOBuilder<,>).MakeGenericType(type, descType).GetMethod("GetMapper");

                        return (POCOBuilder)self.Invoke(null, new object[0]);
                    }
                );

            Func<object, object> mapFuncRet =
                x =>
                {
                    if (x == null) return null;

                    var ret = newPoco.Invoke(EmptyObjects);

                    foreach (var member in asClass.Members)
                    {
                        var getter = t.GetMember(member.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f is FieldInfo || f is PropertyInfo).Single();

                        object fromVal;

                        if (getter is FieldInfo)
                        {
                            fromVal = ((FieldInfo)getter).GetValue(x);
                        }
                        else
                        {
                            fromVal = ((PropertyInfo)getter).GetValue(x);
                        }

                        if (fromVal == null) continue;

                        var toVal = propMaps[member.Key].GetMapper()(fromVal);

                        to[ret, member.Key] = toVal;
                    }

                    return ret;
                };

            return new POCOBuilder(mapFuncRet);*/
        }

        private static POCOBuilder GetListMapper()
        {
            Type fromListType;

            if (typeof(From).IsArray)
            {
                fromListType = typeof(From).GetElementType();
            }else
            {
                fromListType = typeof(From).GetGenericArguments()[0];
            }

            var descType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(fromListType);

            var itemMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(fromListType, descType).GetMethod("GetMapper").Invoke(null, new object[0]));

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

            var keyDescType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(keyType);
            var valDescType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(valType);

            var keyMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(keyType, keyDescType).GetMethod("GetMapper").Invoke(null, new object[0]));
            var valMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(valType, valDescType).GetMethod("GetMapper").Invoke(null, new object[0]));

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

        public static POCOBuilder GetMapper()
        {
            return Builder ?? PromisedBuilder;
        }
    }
}
