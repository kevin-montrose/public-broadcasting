using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    [ProtoInclude(10, typeof(SimpleTypeDescription))]
    [ProtoInclude(11, typeof(ClassTypeDescription))]
    [ProtoInclude(12, typeof(ListTypeDescription))]
    [ProtoInclude(13, typeof(DictionaryTypeDescription))]
    [ProtoInclude(14, typeof(NoTypeDescription))]
    internal abstract class TypeDescription
    {
        /// <summary>
        /// Returns true if there could be more than 1 serialization for the type described,
        /// and thus we must include a type map when serializing.
        /// </summary>
        internal virtual bool NeedsEnvelope { get { return true; } }

        internal abstract Type GetPocoType();

        internal virtual void Seal() { }
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

        internal override Type GetPocoType()
        {
            throw new NotImplementedException();
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

        internal override Type GetPocoType()
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

        private ClassTypeDescription() { }

        internal ClassTypeDescription(Dictionary<string, TypeDescription> members)
        {
            Members = members;
        }

        private TypeBuilder TypeBuilder;
        private Type PocoType;

        internal override void Seal()
        {
            var name = "POCO" + Guid.NewGuid().ToString().Replace("-", "");

            var protoMemberAttr = typeof(ProtoMemberAttribute).GetConstructor(new []{typeof(int)});
            var protoContractAttr = typeof(ProtoContractAttribute).GetConstructor(new Type[0]);

            TypeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public);
            var ix = 1;
            foreach (var kv in Members)
            {
                var memberAttrBuilder = new CustomAttributeBuilder(protoMemberAttr, new object[] { ix });

                var propType = kv.Value.GetPocoType();

                var fieldBldr = TypeBuilder.DefineField("_" + kv.Key + "_" + Guid.NewGuid().ToString().Replace("-", ""), propType, FieldAttributes.Private);

                var prop = TypeBuilder.DefineProperty(kv.Key, PropertyAttributes.None, propType, null);
                prop.SetCustomAttribute(memberAttrBuilder);

                var getSetAttr =MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
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

        internal override Type GetPocoType()
        {
            return PocoType ?? TypeBuilder;
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

        internal DictionaryTypeDescription(TypeDescription keyType, TypeDescription valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }

        internal override Type GetPocoType()
        {
            return typeof(Dictionary<,>).MakeGenericType(KeyType.GetPocoType(), ValueType.GetPocoType());
        }

        internal override void Seal()
        {
            KeyType.Seal();
            ValueType.Seal();
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

        internal ListTypeDescription(TypeDescription contains)
        {
            Contains = contains;
        }

        internal override Type GetPocoType()
        {
            return typeof(List<>).MakeGenericType(Contains.GetPocoType());
        }

        internal override void Seal()
        {
            Contains.Seal();
        }
    }

    internal class Describer<T>
    {
        private static readonly TypeDescription All;
        
        private static readonly TypeDescription AllPublic;
        private static readonly TypeDescription AllPublicPrivate;
        private static readonly TypeDescription AllPublicInternal;
        private static readonly TypeDescription AllPublicProtected;
        private static readonly TypeDescription AllPublicPrivateInternal;
        private static readonly TypeDescription AllPublicPrivateProtected;
        private static readonly TypeDescription AllPublicInternalProtected;

        private static readonly TypeDescription AllPrivate;
        private static readonly TypeDescription AllPrivateInternal;
        private static readonly TypeDescription AllPrivateProtected;
        private static readonly TypeDescription AllPrivateInternalProtected;

        private static readonly TypeDescription AllInternal;
        private static readonly TypeDescription AllInternalProtected;

        private static readonly TypeDescription AllProtected;

        private static readonly TypeDescription Fields;

        private static readonly TypeDescription FieldsPublic;
        private static readonly TypeDescription FieldsPrivate;
        private static readonly TypeDescription FieldsInternal;
        private static readonly TypeDescription FieldsProtected;

        private static readonly TypeDescription FieldsPublicPrivate;
        private static readonly TypeDescription FieldsPublicInternal;
        private static readonly TypeDescription FieldsPublicProtected;

        private static readonly TypeDescription FieldsPublicPrivateInternal;
        private static readonly TypeDescription FieldsPublicPrivateProtected;

        private static readonly TypeDescription FieldsPublicInternalProtected;

        private static readonly TypeDescription FieldsPrivateInternal;
        private static readonly TypeDescription FieldsPrivateProtected;

        private static readonly TypeDescription FieldsPrivateInternalProtected;

        private static readonly TypeDescription FieldsInternalProtected;

        private static readonly TypeDescription Properties;

        private static readonly TypeDescription PropertiesPublic;
        private static readonly TypeDescription PropertiesPrivate;
        private static readonly TypeDescription PropertiesInternal;
        private static readonly TypeDescription PropertiesProtected;

        private static readonly TypeDescription PropertiesPublicPrivate;
        private static readonly TypeDescription PropertiesPublicInternal;
        private static readonly TypeDescription PropertiesPublicProtected;

        private static readonly TypeDescription PropertiesPublicPrivateInternal;
        private static readonly TypeDescription PropertiesPublicPrivateProtected;

        private static readonly TypeDescription PropertiesPublicInternalProtected;

        private static readonly TypeDescription PropertiesPrivateInternal;
        private static readonly TypeDescription PropertiesPrivateProtected;

        private static readonly TypeDescription PropertiesPrivateInternalProtected;

        private static readonly TypeDescription PropertiesInternalProtected;

        static Describer()
        {
            All = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected | IncludedVisibility.Private);

            AllPublic = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
            AllPublicPrivate = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private);
            AllPublicInternal = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal);
            AllPublicProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Protected);
            AllPublicPrivateInternal = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Internal);
            AllPublicPrivateProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Protected);
            AllPublicInternalProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected);

            AllPrivate = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Private);
            AllPrivateInternal = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Internal);
            AllPrivateProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Protected);
            AllPrivateInternalProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Internal | IncludedVisibility.Protected);

            AllInternal = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Internal);
            AllInternalProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Internal | IncludedVisibility.Protected);

            AllProtected = BuildDescription(IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Protected);

            //----//

            Properties = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected | IncludedVisibility.Private);

            PropertiesPublic = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public);
            PropertiesPublicPrivate = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Private);
            PropertiesPublicInternal = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Internal);
            PropertiesPublicProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Protected);
            PropertiesPublicPrivateInternal = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Internal);
            PropertiesPublicPrivateProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Protected);
            PropertiesPublicInternalProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected);

            PropertiesPrivate = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Private);
            PropertiesPrivateInternal = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Private | IncludedVisibility.Internal);
            PropertiesPrivateProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Private | IncludedVisibility.Protected);
            PropertiesPrivateInternalProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Private | IncludedVisibility.Internal | IncludedVisibility.Protected);

            PropertiesInternal = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Internal);
            PropertiesInternalProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Internal | IncludedVisibility.Protected);

            PropertiesProtected = BuildDescription(IncludedMembers.Properties, IncludedVisibility.Protected);

            //----//

            Fields = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected | IncludedVisibility.Private);

            FieldsPublic = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public);
            FieldsPublicPrivate = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private);
            FieldsPublicInternal = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal);
            FieldsPublicProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Protected);
            FieldsPublicPrivateInternal = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Internal);
            FieldsPublicPrivateProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Private | IncludedVisibility.Protected);
            FieldsPublicInternalProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected);

            FieldsPrivate = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Private);
            FieldsPrivateInternal = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Internal);
            FieldsPrivateProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Protected);
            FieldsPrivateInternalProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Private | IncludedVisibility.Internal | IncludedVisibility.Protected);

            FieldsInternal = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Internal);
            FieldsInternalProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Internal | IncludedVisibility.Protected);

            FieldsProtected = BuildDescription(IncludedMembers.Fields, IncludedVisibility.Protected);
        }

        public static TypeDescription BuildDescription(IncludedMembers members, IncludedVisibility visibility, Dictionary<Type, Action<ClassTypeDescription>> inProgress = null)
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

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IDictionary<,>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IDictionary<,>)))
            {
                var dictI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                var keyType = dictI.GetGenericArguments()[0];
                var valueType = dictI.GetGenericArguments()[1];

                var keyDesc = typeof(Describer<>).MakeGenericType(keyType).GetMethod(SelfName);
                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod(SelfName);

                return
                    new DictionaryTypeDescription(
                        (TypeDescription)keyDesc.Invoke(null, new object[] { members, visibility, inProgress }),
                        (TypeDescription)valDesc.Invoke(null, new object[] { members, visibility, inProgress })
                    );
            }

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)))
            {
                var listI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

                var valueType = listI.GetGenericArguments()[0];

                var valDesc = typeof(Describer<>).MakeGenericType(valueType).GetMethod(SelfName);

                return
                    new ListTypeDescription(
                        (TypeDescription)valDesc.Invoke(null, new object[] { members, visibility, inProgress })
                    );
            }

            inProgress[t] = null;

            var get = (typeof(TypeReflectionCache<>).MakeGenericType(t)).GetMethod("Get");

            var cutdown = (CutdownType)get.Invoke(null, new object[] { members, visibility });

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

                    classMembers[propName] = (TypeDescription)propDesc.Invoke(null, new object[] { members, visibility, inProgress });
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

                    classMembers[fieldName] = (TypeDescription)fieldDesc.Invoke(null, new object[] { members, visibility, inProgress });
                }
            }

            var ret = new ClassTypeDescription(classMembers);
            
            var promise = inProgress[t];
            if (promise != null) promise(ret);
            inProgress.Remove(t);

            ret.Seal();

            return ret;
        }

        internal static TypeDescription Get(IncludedMembers members, IncludedVisibility visibility)
        {
            if (members.HasFlag(IncludedMembers.Properties) && members.HasFlag(IncludedMembers.Fields))
            {
                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) && 
                    visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return All;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return AllPublicInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPublicPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPublicPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return AllPublicProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPublicPrivate;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return AllPublicInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public))
                {
                    return AllPublic;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPrivateInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return AllInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Private))
                {
                   return AllPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return AllInternal;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return AllProtected;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Private))
                {
                    return AllPrivate;
                }

                throw new Exception("Shouldn't be possible, didn't return for [" + members + "] [" + visibility + "]");
            }

            if (members.HasFlag(IncludedMembers.Properties))
            {
                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return Properties;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return PropertiesPublicInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPublicPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPublicPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return PropertiesPublicProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPublicPrivate;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return PropertiesPublicInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public))
                {
                    return PropertiesPublic;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPrivateInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return PropertiesInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return PropertiesInternal;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return PropertiesProtected;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Private))
                {
                    return PropertiesPrivate;
                }

                throw new Exception("Shouldn't be possible, didn't return for [" + members + "] [" + visibility + "]");
            }

            if (members.HasFlag(IncludedMembers.Fields))
            {
                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return Fields;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return FieldsPublicInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPublicPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPublicPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return FieldsPublicProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPublicPrivate;
                }

                if (visibility.HasFlag(IncludedVisibility.Public) && visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return FieldsPublicInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Public))
                {
                    return FieldsPublic;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected) &&
                    visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPrivateInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return FieldsInternalProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPrivateInternal;
                }

                if (visibility.HasFlag(IncludedVisibility.Internal))
                {
                    return FieldsInternal;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Protected) && visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPrivateProtected;
                }

                if (visibility.HasFlag(IncludedVisibility.Protected))
                {
                    return FieldsProtected;
                }

                //--//
                if (visibility.HasFlag(IncludedVisibility.Private))
                {
                    return FieldsPrivate;
                }

                throw new Exception("Shouldn't be possible, didn't return for [" + members + "] [" + visibility + "]");
            }

            throw new Exception("Shouldn't be possible, didn't return for [" + members + "] [" + visibility + "]");
        }
    }
}
