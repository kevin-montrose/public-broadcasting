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

        internal abstract TypeDescription DePromise(out Action afterPromise);

        internal abstract TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup);
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

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            throw new NotImplementedException();
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            throw new NotImplementedException();
        }
    }

    [ProtoContract]
    internal class SimpleTypeDescription : TypeDescription
    {
        private const int IntTag = 0;
        private const int StringTag = 1;
        private const int BoolTag = 2;
        private const int DoubleTag = 3;
        private const int LongTag = 4;
        private const int ByteTag = 5;
        private const int CharTag = 6;
        private const int UIntTag = 7;
        private const int ULongTag = 8;
        private const int ShortTag = 9;
        private const int UShortTag = 10;
        private const int FloatTag = 11;
        private const int DecimalTag = 12;
        private const int SByteTag = 13;
        
        internal static readonly SimpleTypeDescription Int = new SimpleTypeDescription(IntTag);
        internal static readonly SimpleTypeDescription UInt = new SimpleTypeDescription(UIntTag);
        internal static readonly SimpleTypeDescription Long = new SimpleTypeDescription(LongTag);
        internal static readonly SimpleTypeDescription ULong = new SimpleTypeDescription(ULongTag);
        internal static readonly SimpleTypeDescription Short = new SimpleTypeDescription(ShortTag);
        internal static readonly SimpleTypeDescription UShort = new SimpleTypeDescription(UShortTag);
        internal static readonly SimpleTypeDescription Byte = new SimpleTypeDescription(ByteTag);
        internal static readonly SimpleTypeDescription SByte = new SimpleTypeDescription(SByteTag);

        internal static readonly SimpleTypeDescription Bool = new SimpleTypeDescription(BoolTag);

        internal static readonly SimpleTypeDescription Double = new SimpleTypeDescription(DoubleTag);
        internal static readonly SimpleTypeDescription Float = new SimpleTypeDescription(FloatTag);
        internal static readonly SimpleTypeDescription Decimal = new SimpleTypeDescription(DecimalTag);
        
        internal static readonly SimpleTypeDescription String = new SimpleTypeDescription(StringTag);
        internal static readonly SimpleTypeDescription Char = new SimpleTypeDescription(CharTag);

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
                case LongTag: return typeof(long);
                case ULongTag: return typeof(ulong);
                case IntTag: return typeof(int);
                case UIntTag: return typeof(uint);
                case ShortTag: return typeof(short);
                case UShortTag: return typeof(ushort);
                case ByteTag: return typeof(byte);
                case SByteTag: return typeof(sbyte);

                case BoolTag: return typeof(bool);

                case DoubleTag: return typeof(double);
                case FloatTag: return typeof(float);
                case DecimalTag: return typeof(decimal);

                case StringTag: return typeof(string);
                case CharTag: return typeof(char);

                default: throw new Exception("Unexpected Tag [" + Type + "]");
            }
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            return this;
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

        private Type ForType { get; set; }

        private ClassTypeDescription() { }

        internal ClassTypeDescription(Dictionary<string, TypeDescription> members, Type forType)
        {
            Members = members;
            ForType = forType;
        }

        private TypeBuilder TypeBuilder;
        private Type PocoType;

        internal override void Seal(TypeDescription existing = null)
        {
            if (PocoType != null || TypeBuilder != null) return;

            Debug.WriteLine("ClassTypeDescription.Seal: [" + ForType + "]");

            var name = "POCO" + Guid.NewGuid().ToString().Replace("-", "");

            var protoMemberAttr = typeof(ProtoMemberAttribute).GetConstructor(new[] { typeof(int) });
            var protoContractAttr = typeof(ProtoContractAttribute).GetConstructor(new Type[0]);

            TypeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public);
            var ix = 1;
            foreach (var kv in Members)
            {
                var memberAttrBuilder = new CustomAttributeBuilder(protoMemberAttr, new object[] { ix });

                kv.Value.Seal(existing);
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

        private bool DePromised { get; set; }
        internal override TypeDescription DePromise(out Action afterPromise)
        {
            if (!DePromised)
            {
                var postMembers = new List<Action>();

                foreach (var key in Members.Keys.ToList())
                {
                    Action act;
                    Members[key] = Members[key].DePromise(out act);
                    postMembers.Add(act);
                }

                afterPromise = () => { postMembers.ForEach(a => a()); };
            }
            else
            {
                afterPromise = () => { };
            }

            DePromised = true;

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var clone = new ClassTypeDescription();

            backRefLookup[this] = clone;

            var members = new Dictionary<string, TypeDescription>();

            foreach (var kv in Members)
            {
                members[kv.Key] = kv.Value.Clone(backRefLookup);
            }

            clone.Members = members;
            clone.ForType = ForType;
            clone.PocoType = PocoType;

            return clone;
        }
    }

    internal class ClassTypeDescription<ForType>
    {
        public static readonly ClassTypeDescription Singleton;

        static ClassTypeDescription()
        {
            var cutdown = TypeReflectionCache<ForType>.Get(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
            var members = new Dictionary<string, TypeDescription>();
            foreach (var field in cutdown.Fields)
            {
                var descType = typeof(Describer<>).MakeGenericType(field.FieldType);
                var descGet = descType.GetMethod("Get");
                var desc = (TypeDescription)descGet.Invoke(null, new object[0]);

                if (desc == null) throw new Exception("No description for [" + field.FieldType + "]");

                members.Add(field.Name, desc);
            }

            foreach (var prop in cutdown.Properties)
            {
                var descType = typeof(Describer<>).MakeGenericType(prop.PropertyType);
                var descGet = descType.GetMethod("Get");
                var desc = (TypeDescription)descGet.Invoke(null, new object[0]);

                if (desc == null) throw new Exception("No description for [" + prop.PropertyType + "]");

                members.Add(prop.Name, desc);
            }

            Singleton = new ClassTypeDescription(members, typeof(ForType));
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

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new BackReferenceTypeDescription(ClassId);

            backRefLookup[this] = ret;

            return ret;
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
        }

        static internal TypeDescription Create(TypeDescription keyType, TypeDescription valueType)
        {
            return new DictionaryTypeDescription(keyType, valueType);
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

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            Action act1, act2;

            KeyType = KeyType.DePromise(out act1);
            ValueType = ValueType.DePromise(out act2);

            afterPromise = () => { act1(); act2(); };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new DictionaryTypeDescription();
            backRefLookup[this] = ret;

            ret.KeyType = KeyType.Clone(backRefLookup);
            ret.ValueType = ValueType.Clone(backRefLookup);

            return ret;
        }
    }

    class PromisedTypeDescription : TypeDescription
    {
        private TypeDescription Fulfilment { get; set; }

        private Type ForType { get; set; }

        internal PromisedTypeDescription(Type forType) 
        {
            ForType = forType;
        }

        public void Fulfil(TypeDescription desc)
        {
            Fulfilment = desc;
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            return Fulfilment.GetPocoType(existingDescription);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise =
                delegate
                {
                    Action act;
                    Fulfilment.DePromise(out act);

                    act();
                };

            return Fulfilment;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            throw new NotImplementedException();
        }
    }

    class PromisedTypeDescription<ForType>
    {
        public static readonly PromisedTypeDescription Singleton;

        static PromisedTypeDescription()
        {
            Singleton = new PromisedTypeDescription(typeof(ForType));
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
        }

        static internal ListTypeDescription Create(TypeDescription contains)
        {
            return new ListTypeDescription(contains);
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return typeof(List<>).MakeGenericType(Contains.GetPocoType(existing));
        }

        internal override void Seal(TypeDescription existing = null)
        {
            Contains.Seal(existing);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            Contains = Contains.DePromise(out afterPromise);

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new ListTypeDescription();
            backRefLookup[this] = ret;

            ret.Contains = Contains.Clone(backRefLookup);

            return ret;
        }
    }

    internal class Describer<T>
    {
        private static readonly PromisedTypeDescription AllPublicPromise;
        private static readonly TypeDescription AllPublic;

        static Describer()
        {
            Debug.WriteLine("Describer: " + typeof(T).FullName);

            AllPublicPromise = PromisedTypeDescription<T>.Singleton;
            
            var allPublic = BuildDescription();

            AllPublicPromise.Fulfil(allPublic);

            AllPublic = allPublic;
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

        public static TypeDescription BuildDescription()
        {
            var t = typeof(T);

            if (t == typeof(long)) return SimpleTypeDescription.Long;
            if (t == typeof(ulong)) return SimpleTypeDescription.ULong;
            if (t == typeof(int)) return SimpleTypeDescription.Int;
            if (t == typeof(uint)) return SimpleTypeDescription.UInt;
            if (t == typeof(short)) return SimpleTypeDescription.Short;
            if (t == typeof(ushort)) return SimpleTypeDescription.UShort;
            if (t == typeof(byte)) return SimpleTypeDescription.Byte;
            if (t == typeof(sbyte)) return SimpleTypeDescription.SByte;

            if (t == typeof(bool)) return SimpleTypeDescription.Bool;

            if (t == typeof(char)) return SimpleTypeDescription.Char;
            if (t == typeof(string)) return SimpleTypeDescription.String;

            if (t == typeof(decimal)) return SimpleTypeDescription.Decimal;
            if (t == typeof(double)) return SimpleTypeDescription.Double;
            if (t == typeof(float)) return SimpleTypeDescription.Float;

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                var dictI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                var dictPromiseType = typeof(PromisedTypeDescription<>).MakeGenericType(dictI);
                var dictPromiseSingle = dictPromiseType.GetField("Singleton");
                var dictPromise = (PromisedTypeDescription)dictPromiseSingle.GetValue(null);

                var keyType = dictI.GetGenericArguments()[0];
                var valueType = dictI.GetGenericArguments()[1];

                var keyDesc = typeof(Describer<>).MakeGenericType(keyType).GetMethod("Get");
                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod("Get");

                TypeDescription key, val;

                key = (TypeDescription)keyDesc.Invoke(null, new object[0]);
                val = (TypeDescription)valDesc.Invoke(null, new object[0]);

                var dictRet = DictionaryTypeDescription.Create(key, val);

                dictPromise.Fulfil(dictRet);

                return dictRet;
            }

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                var listI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

                var valueType = listI.GetGenericArguments()[0];

                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod("Get");

                TypeDescription val;

                val = (TypeDescription)valDesc.Invoke(null, new object[0]);


                return ListTypeDescription.Create(val);
            }

            var get = (typeof(TypeReflectionCache<>).MakeGenericType(t)).GetMethod("Get");

            var cutdown = (CutdownType)get.Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public });

            var classMembers = new Dictionary<string, TypeDescription>(cutdown.Properties.Count + cutdown.Fields.Count);

            foreach (var prop in cutdown.Properties)
            {
                var propName = prop.Name;

                var propType = prop.PropertyType;

                var propDesc = typeof(Describer<>).MakeGenericType(propType).GetMethod("Get");

                classMembers[propName] = (TypeDescription)propDesc.Invoke(null, new object[0]);
            }

            foreach (var field in cutdown.Fields)
            {
                var fieldName = field.Name;
                var fieldType = field.FieldType;

                var fieldDesc = typeof(Describer<>).MakeGenericType(fieldType).GetMethod("Get");

                classMembers[fieldName] = (TypeDescription)fieldDesc.Invoke(null, new object[0]);
            }

            var retType = typeof(ClassTypeDescription<>).MakeGenericType(t);
            var retSingle = retType.GetField("Singleton");

            var ret = (TypeDescription)retSingle.GetValue(null);

            return ret;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return AllPublic ?? AllPublicPromise;
        }

        public static TypeDescription GetForUse(bool flatten)
        {
            var ret = Get();

            Action postPromise;
            ret = ret.DePromise(out postPromise);
            postPromise();

            ret.Seal();

            if (flatten)
            {
                ret = ret.Clone(new Dictionary<TypeDescription, TypeDescription>());

                Flattener.Flatten(ret, GetIdProvider());
            }

            return ret;
        }
    }
}
