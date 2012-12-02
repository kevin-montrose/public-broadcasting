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
    public class OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> : IDictionary<ToKey, ToVal>
    {
        class Enumerator : IEnumerator<KeyValuePair<ToKey, ToVal>>, IEnumerator
        {
            public KeyValuePair<ToKey, ToVal> Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            private IEnumerator KeyEnumerable;
            private OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> Dictionary;
            public Enumerator(OnDemandDictionary<FromKey, FromVal, ToKey, ToVal> dict)
            {
                Dictionary = dict;
            }

            public bool MoveNext()
            {
                if (KeyEnumerable == null)
                {
                    KeyEnumerable = Dictionary.InnerDictionary.Keys.GetEnumerator();
                }

                if (!KeyEnumerable.MoveNext())
                {
                    Current = default(KeyValuePair<ToKey, ToVal>);
                    return false;
                }

                var key = KeyEnumerable.Current;
                var val = Dictionary.InnerDictionary[key];

                var mappedKey = (ToKey)Dictionary.KeyMapper(key);
                var mappedVal = (ToVal)Dictionary.ValueMapper(val);

                Current = new KeyValuePair<ToKey, ToVal>(mappedKey, mappedVal);

                return true;
            }

            public void Reset()
            {
                KeyEnumerable = null;
                Current = default(KeyValuePair<ToKey, ToVal>);
            }

            public void Dispose() { }
        }

        public ICollection<ToKey> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<ToVal> Values
        {
            get { throw new NotImplementedException(); }
        }

        public int Count
        {
            get { return InnerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ToVal this[ToKey key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private IDictionary InnerDictionary;
        private Func<object, object> KeyMapper;
        private Func<object, object> ValueMapper;

        public OnDemandDictionary(IDictionary innerDict, Func<object, object> keyMapper, Func<object, object> valMapper)
        {
            InnerDictionary = innerDict;
            KeyMapper = keyMapper;
            ValueMapper = valMapper;
        }

        public IEnumerator<KeyValuePair<ToKey, ToVal>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #region Not Implemented

        public void Add(ToKey key, ToVal value)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(ToKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ToKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(ToKey key, out ToVal value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<ToKey, ToVal>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<ToKey, ToVal> item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class OnDemandList<From, To> : IList<To>
    {
        class Enumerator<A, B> : IEnumerator, IEnumerator<B>
        {
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public B Current { get; private set; }

            private int Index = -1;
            private OnDemandList<A, B> List;

            public Enumerator(OnDemandList<A, B> list)
            {
                List = list;
            }

            public bool MoveNext()
            {
                Index++;

                if (Index >= List.Count) return false;

                Current = List[Index];

                return true;
            }

            public void Reset()
            {
                Index = -1;
                Current = default(B);
            }

            public void Dispose() { }
        }

        public Type FromType { get { return typeof(From); } }
        public Type ToType { get { return typeof(To); } }

        public int Count
        {
            get { return UnderlyingList.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public To this[int index]
        {
            get
            {
                var raw = UnderlyingList[index];
                var mapped = Mapper(raw);

                return (To)mapped;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private IList UnderlyingList;
        private Func<object, object> Mapper;

        public OnDemandList(IList underlyingList, Func<object, object> mapper)
        {
            UnderlyingList = underlyingList;
            Mapper = mapper;
        }

        public IEnumerator<To> GetEnumerator()
        {
            return new Enumerator<From, To>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator<From, To>(this);
        }

        #region Not Implemented

        public int IndexOf(To item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, To item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(To item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(To item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(To[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(To item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class POCOBuilder
    {
        internal Type To { get; set; }
        protected Func<object, object> Mapper { get; set; }

        protected POCOBuilder() { }

        internal POCOBuilder(Func<object, object> mapper, Type to)
        {
            Mapper = mapper;
            To = to;
        }

        public Func<object, object> GetMapper()
        {
            return Mapper;
        }
    }

    internal class PromisedPOCOBuilder : POCOBuilder
    {
        internal PromisedPOCOBuilder(Type to)
        {
            To = to;
        }

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
            var desc = (TypeDescription)typeof(Describer).GetMethod("GetForUse").Invoke(null, new object[] { false });

            PromisedBuilder = new PromisedPOCOBuilder(desc.GetPocoType());

            var builder = Build(desc);
            PromisedBuilder.Fulfil(builder.GetMapper());

            Builder = builder;
        }

        private static POCOBuilder Build(TypeDescription desc)
        {
            if (desc is ListTypeDescription)
            {
                return GetListMapper();
            }

            if (desc is DictionaryTypeDescription)
            {
                return GetDictionaryMapper();
            }

            if (desc is EnumTypeDescription)
            {
                var vals = ((EnumTypeDescription)desc).Values;

                return 
                    new POCOBuilder(
                        x =>
                        {
                            return x != null ? x.ToString() : null;
                        },
                        typeof(string)
                    );
            }

            if (desc is NullableTypeDescription)
            {
                var inner = ((NullableTypeDescription)desc).InnerType;
                var innerType = inner.GetPocoType();

                var innerDescType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(innerType);

                var mapper = (POCOBuilder)typeof(POCOBuilder<,>).MakeGenericType(innerType, innerDescType).GetMethod("GetMapper").Invoke(null, new object[0]);

                return new POCOBuilder(
                    from =>
                    {
                        if (from == null) return null;

                        var asFrom = (From)from;

                        var mapped = mapper.GetMapper()(asFrom);

                        if (innerType.IsEnum)
                        {
                            mapped = ParseEnumNonGeneric(mapped, innerType);
                        }
                        
                        return mapped;
                    },
                    innerType.IsEnum ? innerType : mapper.To
                );
            }

            if (desc is SimpleTypeDescription)
            {
                if (desc == SimpleTypeDescription.Byte) return new POCOBuilder(x => x is byte ? (byte)x : Convert.ToByte(x), typeof(byte));
                if (desc == SimpleTypeDescription.SByte) return new POCOBuilder(x => x is sbyte ? (sbyte)x : Convert.ToSByte(x), typeof(sbyte));
                if (desc == SimpleTypeDescription.Short) return new POCOBuilder(x => x is short ? (short)x : Convert.ToInt16(x), typeof(short));
                if (desc == SimpleTypeDescription.UShort) return new POCOBuilder(x => x is ushort ? (ushort)x : Convert.ToUInt16(x), typeof(ushort));
                if (desc == SimpleTypeDescription.Int) return new POCOBuilder(x => x is int ? (int)x : Convert.ToInt32(x), typeof(int));
                if (desc == SimpleTypeDescription.UInt) return new POCOBuilder(x => x is uint ? (uint)x : Convert.ToUInt32(x), typeof(uint));
                if (desc == SimpleTypeDescription.Long) return new POCOBuilder(x => x is long ? (long)x : Convert.ToInt64(x), typeof(long));
                if (desc == SimpleTypeDescription.ULong) return new POCOBuilder(x => x is ulong ? (ulong)x : Convert.ToUInt64(x), typeof(ulong));
                if (desc == SimpleTypeDescription.Double) return new POCOBuilder(x => x is double ? (double)x : Convert.ToDouble(x), typeof(double));
                if (desc == SimpleTypeDescription.Float) return new POCOBuilder(x => x is float ? (float)x : Convert.ToSingle(x), typeof(float));
                if (desc == SimpleTypeDescription.Decimal) return new POCOBuilder(x => x is decimal ? (decimal)x : Convert.ToDecimal(x), typeof(decimal));
                if (desc == SimpleTypeDescription.Char) return new POCOBuilder(x => x is char ? (char)x : Convert.ToChar(x), typeof(char));
                if (desc == SimpleTypeDescription.String) return new POCOBuilder(x => x is string || x == null ? (string)x : x.ToString(), typeof(string));
                if (desc == SimpleTypeDescription.Bool) return new POCOBuilder(x => x is bool ? (bool)x : Convert.ToBoolean(x), typeof(bool));
                if (desc == SimpleTypeDescription.DateTime) return new POCOBuilder(x => x is DateTime? (DateTime)x : Convert.ToDateTime(x), typeof(DateTime));
                if (desc == SimpleTypeDescription.TimeSpan) return new POCOBuilder(x => (TimeSpan)x, typeof(TimeSpan));
                if (desc == SimpleTypeDescription.Guid) return new POCOBuilder(x => (Guid)x, typeof(Guid));
                if (desc == SimpleTypeDescription.Uri) return new POCOBuilder(x => (Uri)x, typeof(Uri));

                throw new Exception("Shouldn't be possible, found " + desc);
            }

            return GetClassMapper(desc);
        }

        private static Func<object, object> Lookup(Dictionary<string, POCOBuilder> lookup, string term)
        {
            var ret = lookup[term];

            return ret.GetMapper();
        }

        private static object ParseEnumNonGeneric(object o, Type @enum)
        {
            var asStr = (string)o;

            var ret = Enum.Parse(@enum, asStr);

            return ret;
        }

        private static T ParseEnum<T>(object o) where T : struct
        {
            var asStr = (string)o;

            var ret = Enum.Parse(typeof(T), asStr);

            return (T)ret;
        }

        private static Func<From, Dictionary<string, POCOBuilder>, object> BuildRefRefTypeMapper(Dictionary<string, POCOBuilder> members, Type tTo)
        {
            var tFrom = typeof(From);

            var cons = tTo.GetConstructor(new Type[0]);
            if (cons == null) throw new Exception("No parameterless constructor found for " + tTo.FullName);

            var lookup = typeof(POCOBuilder<From, Describer>).GetMethod("Lookup", BindingFlags.Static | BindingFlags.NonPublic);
            var parseEnum = typeof(POCOBuilder<From, Describer>).GetMethod("ParseEnum", BindingFlags.Static | BindingFlags.NonPublic);
            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var name = "POCOBuilder_RefRef";
            name += "_" + typeof(From).FullName + "_" + typeof(Describer).FullName;

            var dynMethod = new DynamicMethod(name, typeof(object), new[] { tFrom, typeof(Dictionary<string, POCOBuilder>) }, restrictedSkipVisibility: true);
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

                var toMember = tTo.GetField(mem.Key);

                if (toMember.FieldType.IsValueType)
                {
                    if (!toMember.FieldType.IsEnum)
                    {
                        il.Emit(OpCodes.Unbox_Any, toMember.FieldType);// [toVal] [ret]
                    }
                    else
                    {
                        var parse = parseEnum.MakeGenericMethod(toMember.FieldType);
                        il.Emit(OpCodes.Call, parse);                       // [toVal as object] [ret]
                    }
                }
                else
                {
                    il.Emit(OpCodes.Castclass, toMember.FieldType);     // [toVal] [ret]
                }

                il.Emit(OpCodes.Stfld, toMember);                       // -----
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
            var parseEnum = typeof(POCOBuilder<From, Describer>).GetMethod("ParseEnum", BindingFlags.Static | BindingFlags.NonPublic);
            var invoke = typeof(Func<object, object>).GetMethod("Invoke");

            var name = "POCOBuilder_ValueRef";
            name += "_" + typeof(From).FullName + "_" + typeof(Describer).FullName;

            var dynMethod = new DynamicMethod(name, typeof(object), new[] { tFrom, typeof(Dictionary<string, POCOBuilder>) }, restrictedSkipVisibility: true);
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

                var toField = tTo.GetField(mem.Key);

                if (toField.FieldType.IsValueType)
                {
                    if (!toField.FieldType.IsEnum)
                    {
                        il.Emit(OpCodes.Unbox_Any, toField.FieldType);// [toVal] [ret]
                    }
                    else
                    {
                        var parse = parseEnum.MakeGenericMethod(toField.FieldType);
                        il.Emit(OpCodes.Call, parse);                       // [toVal as object] [ret]
                    }
                }
                else
                {
                    il.Emit(OpCodes.Castclass, toField.FieldType);    // [toVal] [ret]
                }

                il.Emit(OpCodes.Stfld, toField);                        // ----
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
                    },
                    pocoType
                );
        }

        private static POCOBuilder GetListMapper()
        {
            Type fromListType;

            if (typeof(From).IsArray)
            {
                fromListType = typeof(From).GetElementType();
            }
            else
            {
                var list = typeof(From);

                if (!(list.IsGenericType && list.GetGenericTypeDefinition() == typeof(IList<>)))
                {
                    list = list.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
                }

                fromListType = list.GetGenericArguments()[0];
            }

            var descType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(fromListType);

            var itemMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(fromListType, descType).GetMethod("GetMapper").Invoke(null, new object[0]));

            Func<IList, Func<object, object>, object> mapperListDyn;
            var cons = typeof(OnDemandList<,>).MakeGenericType(typeof(From), itemMapper.To).GetConstructor(new[] { typeof(IList), typeof(Func<object, object>) });
            var dyn = new DynamicMethod("POCOBuilder_NewList_" + itemMapper.To.FullName, typeof(object), new[] { typeof(IList), typeof(Func<object, object>) }, restrictedSkipVisibility: true);
            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);       // [ilist]
            il.Emit(OpCodes.Ldarg_1);       // [func<object, object>] [ilist]
            il.Emit(OpCodes.Newobj, cons);  // [ret]
            il.Emit(OpCodes.Ret);           // -----

            mapperListDyn = (Func<IList, Func<object, object>, object>)dyn.CreateDelegate(typeof(Func<IList, Func<object, object>, object>));

            return
                new POCOBuilder(
                    x =>
                    {
                        var asList = x as IList;

                        // it's all the same, don't waste our time
                        if (asList == null || asList.Count == 0) return null;

                        var iMap = itemMapper.GetMapper();

                        var ret = mapperListDyn(asList, iMap);

                        return ret;
                    },
                    typeof(OnDemandList<,>).MakeGenericType(typeof(From), itemMapper.To)
                );
        }

        public static POCOBuilder GetDictionaryMapper()
        {
            var dict = typeof(From);

            if (!(dict.IsGenericType && dict.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                dict = dict.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            }

            var genArgs = dict.GetGenericArguments();

            var keyType = genArgs[0];
            var valType = genArgs[1];

            var keyDescType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(keyType);
            var valDescType = typeof(Describer).GetGenericTypeDefinition().MakeGenericType(valType);

            var keyMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(keyType, keyDescType).GetMethod("GetMapper").Invoke(null, new object[0]));
            var valMapper = (POCOBuilder)(typeof(POCOBuilder<,>).MakeGenericType(valType, valDescType).GetMethod("GetMapper").Invoke(null, new object[0]));

            Func<IDictionary, Func<object, object>, Func<object, object>, object> mapperDictDyn;
            var cons = typeof(OnDemandDictionary<,,,>).MakeGenericType(keyType, valType, keyMapper.To, valMapper.To).GetConstructor(new[] { typeof(IDictionary), typeof(Func<object, object>), typeof(Func<object, object>) });
            var dyn = new DynamicMethod("POCOBuilder_NewDict_" + keyType.FullName + "_" + valType.FullName + "_" + keyMapper.To.FullName + "_" + valMapper.To.FullName, typeof(object), new[] { typeof(IDictionary), typeof(Func<object, object>), typeof(Func<object, object>) }, restrictedSkipVisibility: true);
            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);       // [IDictionary]
            il.Emit(OpCodes.Ldarg_1);       // [Func<object, object>] [IDictionary]
            il.Emit(OpCodes.Ldarg_2);       // [Func<object, object>] [Func<object, object>] [IDictionary]
            il.Emit(OpCodes.Newobj, cons);  // [ret]
            il.Emit(OpCodes.Ret);           // -----

            mapperDictDyn = (Func<IDictionary, Func<object, object>, Func<object, object>, object>)dyn.CreateDelegate(typeof(Func<IDictionary, Func<object, object>, Func<object, object>, object>));

            return
                new POCOBuilder(
                    x =>
                    {
                        var asDict = x as IDictionary;

                        // Don't waste my time
                        if (asDict == null || asDict.Count == 0) return null;

                        var kMap = keyMapper.GetMapper();
                        var vMap = valMapper.GetMapper();

                        var ret = mapperDictDyn(asDict, kMap, vMap);

                        return ret;
                    },
                    typeof(OnDemandDictionary<,,,>).MakeGenericType(keyType, valType, keyMapper.To, valMapper.To)
                );
        }

        public static POCOBuilder GetMapper()
        {
            return Builder ?? PromisedBuilder;
        }
    }
}
