using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    public class ClassEnumerator : IEnumerator
    {
        private object CurrentVal;
        public object Current
        {
            get { if (CurrentVal == null) throw new InvalidOperationException("Enumerator has no value"); return CurrentVal; }
            private set { CurrentVal = value; }
        }

        private int Index { get; set; }
        private List<string> Members { get; set; }
        private dynamic Values { get; set; }

        public ClassEnumerator(List<string> names, object val)
        {
            Members = names;
            Values = val;
            Index = -1;
        }

        public bool MoveNext()
        {
            Index++;

            if (Index >= Members.Count)
            {
                CurrentVal = null;
                return false;
            }

            CurrentVal = new DictionaryEntry(Members[Index], Values[Members[Index]]);

            return true;
        }

        public void Reset()
        {
            Index = -1;
            CurrentVal = null;
        }
    }

    public class ProbeClass
    {
        public static bool Validate(object[] objs)
        {
            return objs.Length == 1 && (objs[0] is string);
        }

        public static bool TwoStrEqs(string s1, string s2)
        {
            return s1.Equals(s2);
        }
    }

    [ProtoContract]
    internal class ClassTypeDescription : TypeDescription
    {
        static readonly ModuleBuilder ModuleBuilder;

        static ClassTypeDescription()
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName asmName = new AssemblyName("PublicBroadcastingDynamicClassAssembly");
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

            var propGetters = new Dictionary<string, MethodInfo>();

            TypeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public, typeof(DynamicObject), new [] { typeof(IEnumerable) });
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

                propGetters[kv.Key] = getPropMthdBldr;

                ix++;
            }

            var contractAttrBuilder = new CustomAttributeBuilder(protoContractAttr, new object[0]);
            TypeBuilder.SetCustomAttribute(contractAttrBuilder);

            // Define indexer
            var validate = typeof(ProbeClass).GetMethod("Validate");
            var eq = typeof(ProbeClass).GetMethod("TwoStrEqs");

            var tryGetIndex = TypeBuilder.DefineMethod("TryGetIndex", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), new[] { typeof(GetIndexBinder), typeof(object[]), Type.GetType("System.Object&") });
            var il = tryGetIndex.GetILGenerator();

            il.Emit(OpCodes.Ldarg_2);       // object[]

            il.Emit(OpCodes.Call, validate);// bool

            var valid = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, valid);

            il.Emit(OpCodes.Ldarg_3);       // (out object)
            il.Emit(OpCodes.Ldnull);        // null (out object);
            il.Emit(OpCodes.Stind_Ref);     // ----

            il.Emit(OpCodes.Ldc_I4_0);      // 0
            il.Emit(OpCodes.Ret);

            il.MarkLabel(valid);

            il.Emit(OpCodes.Ldarg_3);       // (out object)

            il.Emit(OpCodes.Ldarg_2);       // object[] (out object)
            il.Emit(OpCodes.Ldc_I4_0);      // 0 object[] (out object)
            il.Emit(OpCodes.Ldelem_Ref);    // key (out object)

            Label next;
            var done = il.DefineLabel();
            foreach (var mem in Members)
            {
                next = il.DefineLabel();

                var memKey = mem.Key;
                var prop = propGetters[memKey];

                il.Emit(OpCodes.Dup);               // key key (out object)
                il.Emit(OpCodes.Ldstr, memKey);     // memKey key key (out object)
                il.Emit(OpCodes.Call, eq);          // bool key (out object)
                il.Emit(OpCodes.Brfalse_S, next);   // key (out object)

                il.Emit(OpCodes.Pop);               // (out object)
                il.Emit(OpCodes.Ldarg_0);           // this (out object)
                il.Emit(OpCodes.Callvirt, prop);    // ret (out object);

                if (prop.ReturnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, prop.ReturnType);  // ret (out object);
                }

                il.Emit(OpCodes.Br, done);          // ret (out object);

                il.MarkLabel(next);                 // key (out object);
            }

            il.Emit(OpCodes.Pop);           // (out object)
            il.Emit(OpCodes.Ldnull);        // null (out object)

            il.MarkLabel(done);             // ret (out object);

            il.Emit(OpCodes.Stind_Ref);     // ----
            il.Emit(OpCodes.Ldc_I4_1);      // 1
            il.Emit(OpCodes.Ret);           // ----

            TypeBuilder.DefineMethodOverride(tryGetIndex, typeof(DynamicObject).GetMethod("TryGetIndex"));

            // Implement IEnumerable
            var getEnumerator = TypeBuilder.DefineMethod("GetEnumerator", MethodAttributes.Public | MethodAttributes.Virtual, typeof(IEnumerator), Type.EmptyTypes);
            il = getEnumerator.GetILGenerator();

            var newStrList = typeof(List<string>).GetConstructor(new[] { typeof(int) });
            var newEnumerator = typeof(ClassEnumerator).GetConstructor(new[] { typeof(List<string>), typeof(object) });
            var add = typeof(List<string>).GetMethod("Add");

            il.Emit(OpCodes.Ldc_I4, Members.Count); // [count]
            il.Emit(OpCodes.Newobj, newStrList);    // [mems]

            foreach (var mem in Members)
            {
                il.Emit(OpCodes.Dup);               // [mems] [mems]
                il.Emit(OpCodes.Ldstr, mem.Key);    // [key] [mems] [mems]
                il.Emit(OpCodes.Call, add);         // [mems]
            }

            il.Emit(OpCodes.Ldarg_0);               // [this] [mems]
            il.Emit(OpCodes.Newobj, newEnumerator); // [ret]
            il.Emit(OpCodes.Ret);

            TypeBuilder.DefineMethodOverride(getEnumerator, typeof(IEnumerable).GetMethod("GetEnumerator"));

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

    internal class ClassTypeDescription<ForType, DescriberType>
    {
        public static readonly ClassTypeDescription Singleton;

        static ClassTypeDescription()
        {
            var cutdownVisibility = typeof(DescriberType).GetMethod("GetVisibilityMask");
            var cutdownMembers = typeof(DescriberType).GetMethod("GetMemberMask");

            var visibilityMask = (IncludedVisibility)cutdownVisibility.Invoke(null, new object[0]);
            var membersMask = (IncludedMembers)cutdownMembers.Invoke(null, new object[0]);

            var describerType = typeof(DescriberType).GetGenericTypeDefinition();

            var cutdown = TypeReflectionCache<ForType>.Get(membersMask, visibilityMask);
            var members = new Dictionary<string, TypeDescription>();
            foreach (var field in cutdown.Fields)
            {
                var descType = describerType.MakeGenericType(field.FieldType);
                var descGet = descType.GetMethod("Get");
                var desc = (TypeDescription)descGet.Invoke(null, new object[0]);

                if (desc == null) throw new Exception("No description for [" + field.FieldType + "]");

                members.Add(field.Name, desc);
            }

            foreach (var prop in cutdown.Properties)
            {
                var descType = describerType.MakeGenericType(prop.PropertyType);
                var descGet = descType.GetMethod("Get");
                var desc = (TypeDescription)descGet.Invoke(null, new object[0]);

                if (desc == null) throw new Exception("No description for [" + prop.PropertyType + "]");

                members.Add(prop.Name, desc);
            }

            Singleton = new ClassTypeDescription(members, typeof(ForType));
        }
    }
}
