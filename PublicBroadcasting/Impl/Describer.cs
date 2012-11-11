using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    [ProtoInclude(3, typeof(SimpleTypeDescription))]
    [ProtoInclude(4, typeof(ClassTypeDescription))]
    [ProtoInclude(5, typeof(ListTypeDescription))]
    [ProtoInclude(6, typeof(DictionaryTypeDescription))]
    internal abstract class TypeDescription
    {
        /// <summary>
        /// Returns true if there could be more than 1 serialization for the type described,
        /// and thus we must include a type map when serializing.
        /// </summary>
        public virtual bool NeedsEnvelope { get { return true; } }
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
        internal int Tag { get; private set; }

        public override bool NeedsEnvelope
        {
            get
            {
                // There's only one way to serialize any of these types, so the type itself is sufficient
                //   No envelope needed
                return false;
            }
        }

        private SimpleTypeDescription(int tag)
        {
            Tag = tag;
        }
    }

    [ProtoContract]
    internal class ClassTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal Dictionary<string, TypeDescription> Members { get; set; }

        internal ClassTypeDescription(Dictionary<string, TypeDescription> members)
        {
            Members = members;
        }
    }

    [ProtoContract]
    internal class DictionaryTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription KeyType { get; set; }
        [ProtoMember(2)]
        internal TypeDescription ValueType { get; set; }

        public override bool NeedsEnvelope
        {
            get
            {
                return KeyType.NeedsEnvelope || ValueType.NeedsEnvelope;
            }
        }

        internal DictionaryTypeDescription(TypeDescription keyType, TypeDescription valueType)
        {
            KeyType = keyType;
            ValueType = valueType;
        }
    }

    [ProtoContract]
    internal class ListTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        internal TypeDescription Contains { get; set; }

        public override bool NeedsEnvelope
        {
            get
            {
                return Contains.NeedsEnvelope;
            }
        }

        internal ListTypeDescription(TypeDescription contains)
        {
            Contains = contains;
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

        public static TypeDescription BuildDescription(IncludedMembers members, IncludedVisibility visibility)
        {
            const string SelfName = "BuildDescription";

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
                        (TypeDescription)keyDesc.Invoke(null, new object[] { members, visibility }),
                        (TypeDescription)valDesc.Invoke(null, new object[] { members, visibility })
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
                        (TypeDescription)valDesc.Invoke(null, new object[] { members, visibility })
                    );
            }

            var get = (typeof(TypeReflectionCache<>).MakeGenericType(t)).GetMethod("Get");

            var cutdown = (CutdownType)get.Invoke(null, new object[] { members, visibility });

            var classMembers = new Dictionary<string, TypeDescription>(cutdown.Properties.Count + cutdown.Fields.Count);

            foreach (var prop in cutdown.Properties)
            {
                var propDesc = typeof(Describer<>).MakeGenericType(prop.PropertyType).GetMethod(SelfName);

                classMembers[prop.Name] = (TypeDescription)propDesc.Invoke(null, new object[] { members, visibility });
            }

            foreach (var field in cutdown.Fields)
            {
                var fieldDesc = typeof(Describer<>).MakeGenericType(field.FieldType).GetMethod(SelfName);

                classMembers[field.Name] = (TypeDescription)fieldDesc.Invoke(null, new object[] { members, visibility });
            }

            return new ClassTypeDescription(classMembers);
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
