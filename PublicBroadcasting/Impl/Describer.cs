using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    [ProtoInclude(3, typeof(SimpleTypeDescription))]
    [ProtoInclude(4, typeof(ClassTypeDescription))]
    [ProtoInclude(5, typeof(ListTypeDescription))]
    [ProtoInclude(6, typeof(DictionaryTypeDescription))]
    [ProtoInclude(7, typeof(BackReferenceTypeDescription))]
    [ProtoInclude(8, typeof(NoTypeDescription))]
    internal abstract class TypeDescription
    {
        /// <summary>
        /// Returns true if there could be more than 1 serialization for the type described,
        /// and thus we must include a type map when serializing.
        /// </summary>
        internal virtual bool NeedsEnvelope { get { return true; } }

        internal abstract Type GetPocoType(TypeDescription existingDescription = null);

        internal virtual void Seal(TypeDescription existing = null) { }

        internal virtual TypeDescription Flatten(Func<int> nextId) { return this; }

        private static List<TypeDescription> _Cache = new List<TypeDescription>();

        protected static void Cache(TypeDescription c)
        {
            lock (_Cache)
            {
                if (_Cache.Any(a => a.Equals(c))) return;

                _Cache.Add(c);
            }
        }

        protected static TypeDescription EquivalentFromCache(TypeDescription t)
        {
            lock (_Cache)
            {
                foreach (var c in _Cache)
                {
                    if (c.Equals(t)) return c;
                }
            }

            return null;
        }

        public abstract TypeDescription FromCache();

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }

    [ProtoContract]
    internal class NoTypeDescription : TypeDescription
    {
        internal override bool NeedsEnvelope
        {
            get
            {
                return false;
            }
        }

        internal NoTypeDescription() { }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return obj is NoTypeDescription;
        }

        public override int GetHashCode()
        {
            return -1;
        }

        public override TypeDescription FromCache()
        {
            return this;
        }
    }

    [ProtoContract]
    internal class SimpleTypeDescription : TypeDescription
    {
        internal static readonly SimpleTypeDescription Int = new SimpleTypeDescription(0);
        internal static readonly SimpleTypeDescription Long = new SimpleTypeDescription(1);
        internal static readonly SimpleTypeDescription String = new SimpleTypeDescription(2);
        internal static readonly SimpleTypeDescription Byte = new SimpleTypeDescription(3);
        internal static readonly SimpleTypeDescription Char = new SimpleTypeDescription(4);
        internal static readonly SimpleTypeDescription Short = new SimpleTypeDescription(5);
        internal static readonly SimpleTypeDescription UInt = new SimpleTypeDescription(6);
        internal static readonly SimpleTypeDescription ULong = new SimpleTypeDescription(7);
        internal static readonly SimpleTypeDescription SByte = new SimpleTypeDescription(8);
        internal static readonly SimpleTypeDescription UShort = new SimpleTypeDescription(9);
        internal static readonly SimpleTypeDescription Double = new SimpleTypeDescription(10);
        internal static readonly SimpleTypeDescription Float = new SimpleTypeDescription(11);
        internal static readonly SimpleTypeDescription Decimal = new SimpleTypeDescription(12);

        [ProtoMember(1)]
        internal int Type { get; private set; }

        internal override bool NeedsEnvelope
        {
            get
            {
                // There's only one way to serialize any of these types, so the type itself is sufficient
                //   No envelope needed
                return false;
            }
        }

        private SimpleTypeDescription() { }

        private SimpleTypeDescription(int tag)
        {
            Type = tag;
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            switch (Type)
            {
                case 0: return typeof(int);
                case 1: return typeof(long);
                case 2: return typeof(string);
                case 3: return typeof(byte);
                case 4: return typeof(char);
                case 5: return typeof(short);
                case 6: return typeof(uint);
                case 7: return typeof(ulong);
                case 8: return typeof(sbyte);
                case 9: return typeof(ushort);
                case 10: return typeof(double);
                case 11: return typeof(float);
                case 12: return typeof(decimal);
                default: throw new Exception("Unexpected Tag [" + Type + "]");
            }
        }

        public override bool Equals(object obj)
        {
            var asSimple = obj as SimpleTypeDescription;
            if (asSimple == null) return false;

            return Type == asSimple.Type;
        }

        public override int GetHashCode()
        {
            return Type;
        }

        public override TypeDescription FromCache()
        {
            switch (Type)
            {
                case 0: return Int;
                case 1: return Long;
                case 2: return String;
                case 3: return Byte;
                case 4: return Char;
                case 5: return Short;
                case 6: return UInt;
                case 7: return ULong;
                case 8: return SByte;
                case 9: return UShort;
                case 10: return Double;
                case 11: return Float;
                case 12: return Decimal;
                default: throw new Exception("Unexpected Tag [" + Type + "]");
            }
        }
    }

    [ProtoContract]
    internal class ClassTypeDescription : TypeDescription
    {
        static readonly ModuleBuilder ModuleBuilder;

        static ClassTypeDescription()
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName asmName = new AssemblyName("PublicBroadcastingDynamicAssembly");
            AssemblyBuilder asmBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);
        }

        [ProtoMember(1)]
        internal Dictionary<string, TypeDescription> Members { get; set; }

        [ProtoMember(2)]
        internal int Id { get; set; }

        private ClassTypeDescription() { }

        private ClassTypeDescription(Dictionary<string, TypeDescription> members)
        {
            Members = members;

            TypeDescription.Cache(this);
        }

        internal static ClassTypeDescription Create(Dictionary<string, TypeDescription> members)
        {
            foreach (var key in members.Keys.ToList())
            {
                members[key] = members[key].FromCache();
            }

            return (ClassTypeDescription)(new ClassTypeDescription(members).FromCache());
        }

        private TypeBuilder TypeBuilder;
        private Type PocoType;

        internal override void Seal(TypeDescription existing = null)
        {
            if (PocoType != null) return;

            var name = "POCO" + Guid.NewGuid().ToString().Replace("-", "");

            var protoMemberAttr = typeof(ProtoMemberAttribute).GetConstructor(new[] { typeof(int) });
            var protoContractAttr = typeof(ProtoContractAttribute).GetConstructor(new Type[0]);

            TypeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public);
            var ix = 1;
            foreach (var kv in Members)
            {
                var memberAttrBuilder = new CustomAttributeBuilder(protoMemberAttr, new object[] { ix });

                var propType = kv.Value.GetPocoType(existing);

                var fieldBldr = TypeBuilder.DefineField("_" + kv.Key + "_" + Guid.NewGuid().ToString().Replace("-", ""), propType, FieldAttributes.Private);

                var prop = TypeBuilder.DefineProperty(kv.Key, PropertyAttributes.None, propType, null);
                prop.SetCustomAttribute(memberAttrBuilder);

                var getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                var getPropMthdBldr = TypeBuilder.DefineMethod("get_" + kv.Key, getSetAttr, propType, Type.EmptyTypes);
                var custNameGetIL = getPropMthdBldr.GetILGenerator();

                custNameGetIL.Emit(OpCodes.Ldarg_0);
                custNameGetIL.Emit(OpCodes.Ldfld, fieldBldr);
                custNameGetIL.Emit(OpCodes.Ret);

                var setPropMthdBldr = TypeBuilder.DefineMethod("set_" + kv.Key, getSetAttr, null, new Type[] { propType });
                var custNameSetIL = setPropMthdBldr.GetILGenerator();

                custNameSetIL.Emit(OpCodes.Ldarg_0);
                custNameSetIL.Emit(OpCodes.Ldarg_1);
                custNameSetIL.Emit(OpCodes.Stfld, fieldBldr);
                custNameSetIL.Emit(OpCodes.Ret);

                prop.SetGetMethod(getPropMthdBldr);
                prop.SetSetMethod(setPropMthdBldr);

                ix++;
            }

            var contractAttrBuilder = new CustomAttributeBuilder(protoContractAttr, new object[0]);
            TypeBuilder.SetCustomAttribute(contractAttrBuilder);

            PocoType = TypeBuilder.CreateType();
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return PocoType ?? TypeBuilder;
        }

        private List<Tuple<TypeDescription, Action<TypeDescription>>> GetDescendentMemberModifiers()
        {
            var ret = new List<Tuple<TypeDescription, Action<TypeDescription>>>();

            foreach (var member in Members)
            {
                var copy = member.Key;

                ret.Add(
                    Tuple.Create(
                        member.Value,
                        (Action<TypeDescription>)(x => Members[copy] = x)
                    )
                );
            }

            var recurOn = Members.Select(m => m.Value).OfType<ClassTypeDescription>();
            recurOn = recurOn.Where(o => !ret.Any(r => r.Item1 == o));

            foreach (var clazz in recurOn.ToList())
            {
                ret.AddRange(clazz.GetDescendentMemberModifiers());
            }

            return ret;
        }

        internal override TypeDescription Flatten(Func<int> nextId)
        {
            var copy = new ClassTypeDescription();
            copy.PocoType = PocoType;
            copy.Members = new Dictionary<string, TypeDescription>(Members);

            var descendentMembers = copy.GetDescendentMemberModifiers();

            var needsReplace = descendentMembers.Where(w => w.Item1.Equals(copy)).ToList();

            if (needsReplace.Count == 0)
            {
                return copy;
            }

            copy.Id = nextId();

            var reference = new BackReferenceTypeDescription(copy.Id);

            foreach (var replace in needsReplace)
            {
                replace.Item2(reference);
            }

            foreach (var member in copy.Members)
            {
                member.Value.Flatten(nextId);
            }

            return copy;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) return true;

            var asClass = obj as ClassTypeDescription;
            if (asClass == null) return false;

            if (Members.Count != asClass.Members.Count) return false;

            foreach (var mem in Members)
            {
                TypeDescription otherVal;
                if (!asClass.Members.TryGetValue(mem.Key, out otherVal)) return false;

                if (!mem.Value.Equals(otherVal)) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Members.Select(kv => kv.Key.GetHashCode() ^ (kv.Value.GetHashCode() * -1)).Aggregate((a, b) => a ^ b);
        }

        public override TypeDescription FromCache()
        {
            var allCached = TypeDescription.EquivalentFromCache(this);
            if (allCached != null) return allCached;

            foreach (var key in Members.Keys.ToList())
            {
                Members[key] = Members[key].FromCache();
            }

            return this;
        }
    }

    [ProtoContract]
    internal class BackReferenceTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        public int ClassId { get; set; }

        private BackReferenceTypeDescription() { }

        internal BackReferenceTypeDescription(int id)
        {
            ClassId = id;
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            if (existingDescription == null) throw new ArgumentNullException("existingDescription");

            var stack = new Stack<TypeDescription>();
            stack.Push(existingDescription);

            while (stack.Count > 0)
            {
                var top = stack.Pop();

                if (top is SimpleTypeDescription) continue;

                if (top is ListTypeDescription)
                {
                    stack.Push(((ListTypeDescription)top).Contains);
                    continue;
                }

                if (top is DictionaryTypeDescription)
                {
                    var asDict = (DictionaryTypeDescription)top;

                    stack.Push(asDict.KeyType);
                    stack.Push(asDict.ValueType);

                    continue;
                }

                if (top is ClassTypeDescription)
                {
                    var asClass = (ClassTypeDescription)top;

                    if (asClass.Id == ClassId) return asClass.GetPocoType(existingDescription);

                    foreach (var member in asClass.Members)
                    {
                        stack.Push(member.Value);
                    }

                    continue;
                }

                if (top is NoTypeDescription)
                {
                    throw new Exception("How did this even happen?");
                }
            }

            throw new Exception("Couldn't find reference to ClassId = " + ClassId);
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return ClassId;
        }

        public override TypeDescription FromCache()
        {
            return this;
        }
    }

    [ProtoContract]
    internal class DictionaryTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription KeyType { get; set; }
        [ProtoMember(2)]
        internal TypeDescription ValueType { get; set; }

        internal override bool NeedsEnvelope
        {
            get
            {
                return KeyType.NeedsEnvelope || ValueType.NeedsEnvelope;
            }
        }

        private DictionaryTypeDescription() { }

        private DictionaryTypeDescription(TypeDescription keyType, TypeDescription valueType)
        {
            KeyType = keyType;
            ValueType = valueType;

            TypeDescription.Cache(this);
        }

        static internal TypeDescription Create(TypeDescription keyType, TypeDescription valueType)
        {
            keyType = keyType.FromCache();
            valueType = valueType.FromCache();

            return new DictionaryTypeDescription(keyType, valueType).FromCache();
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return typeof(Dictionary<,>).MakeGenericType(KeyType.GetPocoType(existing), ValueType.GetPocoType(existing));
        }

        internal override void Seal(TypeDescription existing = null)
        {
            KeyType.Seal(existing);
            ValueType.Seal(existing);
        }

        internal override TypeDescription Flatten(Func<int> nextId)
        {
            var clone = new DictionaryTypeDescription();
            clone.KeyType = KeyType.Flatten(nextId);
            clone.ValueType = ValueType.Flatten(nextId);

            return clone;
        }

        public override bool Equals(object obj)
        {
            var asDict = obj as DictionaryTypeDescription;
            if (asDict == null) return false;

            return KeyType.Equals(asDict.KeyType) && ValueType.Equals(asDict.ValueType);
        }

        public override int GetHashCode()
        {
            return KeyType.GetHashCode() ^ ValueType.GetHashCode();
        }

        public override TypeDescription FromCache()
        {
            var allCached = TypeDescription.EquivalentFromCache(this);
            if (allCached != null) return allCached;

            KeyType = KeyType.FromCache();
            ValueType = ValueType.FromCache();

            return this;
        }
    }

    [ProtoContract]
    internal class ListTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription Contains { get; set; }

        internal override bool NeedsEnvelope
        {
            get
            {
                return Contains.NeedsEnvelope;
            }
        }

        private ListTypeDescription() { }

        private ListTypeDescription(TypeDescription contains)
        {
            Contains = contains;

            TypeDescription.Cache(this);
        }

        static internal ListTypeDescription Create(TypeDescription contains)
        {
            contains = contains.FromCache();

            return (ListTypeDescription)(new ListTypeDescription(contains).FromCache());
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return typeof(List<>).MakeGenericType(Contains.GetPocoType(existing));
        }

        internal override void Seal(TypeDescription existing = null)
        {
            Contains.Seal(existing);
        }

        internal override TypeDescription Flatten(Func<int> nextId)
        {
            var clone = new ListTypeDescription();
            clone.Contains = Contains.Flatten(nextId);

            return clone;
        }

        public override bool Equals(object obj)
        {
            var asList = obj as ListTypeDescription;
            if (asList == null) return false;

            return Contains.Equals(asList.Contains);
        }

        public override int GetHashCode()
        {
            return Contains.GetHashCode() * -1;
        }

        public override TypeDescription FromCache()
        {
            var allCached = TypeDescription.EquivalentFromCache(this);
            if (allCached != null) return allCached;

            Contains = Contains.FromCache();

            return this;
        }
    }

    internal class Describer<T>
    {
        private static readonly TypeDescription AllPublic;

        static Describer()
        {
            Debug.WriteLine("Describer: " + typeof(T).FullName);

            AllPublic = BuildDescription();

            AllPublic = AllPublic.Flatten(GetIdProvider());
        }

        private static Func<int> GetIdProvider()
        {
            int startId = 0;

            return
                () =>
                {
                    return Interlocked.Increment(ref startId);
                };
        }

        public static TypeDescription BuildDescription(Dictionary<Type, Action<ClassTypeDescription>> inProgress = null)
        {
            const string SelfName = "BuildDescription";

            inProgress = inProgress ?? new Dictionary<Type, Action<ClassTypeDescription>>();

            var t = typeof(T);

            if (t == typeof(long)) return SimpleTypeDescription.Long;
            if (t == typeof(ulong)) return SimpleTypeDescription.ULong;
            if (t == typeof(int)) return SimpleTypeDescription.Int;
            if (t == typeof(uint)) return SimpleTypeDescription.UInt;
            if (t == typeof(short)) return SimpleTypeDescription.Short;
            if (t == typeof(ushort)) return SimpleTypeDescription.UShort;
            if (t == typeof(byte)) return SimpleTypeDescription.Byte;
            if (t == typeof(sbyte)) return SimpleTypeDescription.SByte;

            if (t == typeof(char)) return SimpleTypeDescription.Char;
            if (t == typeof(string)) return SimpleTypeDescription.String;

            if (t == typeof(decimal)) return SimpleTypeDescription.Decimal;
            if (t == typeof(double)) return SimpleTypeDescription.Double;
            if (t == typeof(float)) return SimpleTypeDescription.Float;

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                var dictI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                var keyType = dictI.GetGenericArguments()[0];
                var valueType = dictI.GetGenericArguments()[1];

                var keyDesc = typeof(Describer<>).MakeGenericType(keyType).GetMethod(SelfName);
                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod(SelfName);

                var key = (TypeDescription)keyDesc.Invoke(null, new object[] { inProgress });
                var val = (TypeDescription)valDesc.Invoke(null, new object[] { inProgress });

                return DictionaryTypeDescription.Create(key, val);
            }

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                var listI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

                var valueType = listI.GetGenericArguments()[0];

                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod(SelfName);

                var val = (TypeDescription)valDesc.Invoke(null, new object[] { inProgress });

                return ListTypeDescription.Create(val);
            }

            inProgress[t] = null;

            var get = (typeof(TypeReflectionCache<>).MakeGenericType(t)).GetMethod("Get");

            var cutdown = (CutdownType)get.Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public });

            var classMembers = new Dictionary<string, TypeDescription>(cutdown.Properties.Count + cutdown.Fields.Count);

            foreach (var prop in cutdown.Properties)
            {
                var propName = prop.Name;

                var propType = prop.PropertyType;

                if (inProgress.ContainsKey(propType))
                {
                    var tail = inProgress[propType];
                    Action<ClassTypeDescription> callback =
                        x =>
                        {
                            classMembers[propName] = x;

                            if (tail != null) tail(x);
                        };

                    inProgress[propType] = callback;
                }
                else
                {
                    var propDesc = typeof(Describer<>).MakeGenericType(propType).GetMethod(SelfName);

                    classMembers[propName] = (TypeDescription)propDesc.Invoke(null, new object[] { inProgress });
                }
            }

            foreach (var field in cutdown.Fields)
            {
                var fieldName = field.Name;
                var fieldType = field.FieldType;

                if (inProgress.ContainsKey(fieldType))
                {
                    var tail = inProgress[fieldType];
                    Action<ClassTypeDescription> callback =
                        x =>
                        {
                            classMembers[fieldName] = x;

                            if (tail != null) tail(x);
                        };

                    inProgress[fieldType] = callback;
                }
                else
                {
                    var fieldDesc = typeof(Describer<>).MakeGenericType(fieldType).GetMethod(SelfName);

                    classMembers[fieldName] = (TypeDescription)fieldDesc.Invoke(null, new object[] { inProgress });
                }
            }

            var ret = ClassTypeDescription.Create(classMembers);

            var promise = inProgress[t];
            if (promise != null) promise(ret);
            inProgress.Remove(t);

            ret.Seal();

            return ret;
        }

        internal static TypeDescription Get()
        {
            return AllPublic;
        }
    }
}
