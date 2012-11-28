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
    internal class StringOrdinalComparer : IComparer<string>
    {
        public static readonly StringOrdinalComparer Singleton = new StringOrdinalComparer();

        private StringOrdinalComparer() { }

        public int Compare(string x, string y)
        {
            return string.CompareOrdinal(x, y);
        }
    }

    [ProtoContract]
    internal class EnumTypeDescription : TypeDescription
    {
        static readonly ModuleBuilder ModuleBuilder;

        static EnumTypeDescription()
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName asmName = new AssemblyName("PublicBroadcastingDynamicEnumAssembly");
            AssemblyBuilder asmBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);
        }

        [ProtoMember(1)]
        internal List<string> Values { get; set; }
        [ProtoMember(2)]
        internal int Id { get; set; }

        private EnumTypeDescription() { }

        private EnumTypeDescription(Type t)
        {
            Values = Enum.GetNames(t).OrderBy(o => o, StringOrdinalComparer.Singleton).ToList();
        }

        internal static EnumTypeDescription Create(Type t)
        {
            return new EnumTypeDescription(t);
        }

        private Type EnumType;
        internal override void Seal(TypeDescription existing = null)
        {
            if (EnumType != null) return;

            var builder = ModuleBuilder.DefineEnum("PBEnum" + Guid.NewGuid().ToString().Replace("-", ""), TypeAttributes.Public, typeof(int));

            var protoContractAttr = typeof(ProtoContractAttribute).GetConstructor(new Type[0]);

            for (var i = 0; i < Values.Count; i++)
            {
                builder.DefineLiteral(Values[i], i);
            }

            var contractAttrBuilder = new CustomAttributeBuilder(protoContractAttr, new object[0]);
            builder.SetCustomAttribute(contractAttrBuilder);

            EnumType = builder.CreateType();
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            if (EnumType == null)
            {
                Seal(existingDescription);
            }

            return EnumType;
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };
            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            return this;
        }
    }

    internal class EnumTypeDescription<Enum> where Enum : struct
    {
        public static readonly EnumTypeDescription Singleton;
        
        static EnumTypeDescription()
        {
            var t = typeof(Enum);

            if (!t.IsEnum) throw new Exception(t.FullName + " is not an enumeration");

            Singleton = EnumTypeDescription.Create(t);
        }
    }
}
