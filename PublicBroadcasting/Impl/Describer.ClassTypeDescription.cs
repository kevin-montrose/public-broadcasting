﻿using Microsoft.CSharp.RuntimeBinder;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class ClassTypeDescription : TypeDescription
    {
        static readonly ModuleBuilder ModuleBuilder;
        static readonly Type Enumerator;
        static readonly Func<object, string> ToStringFunc;

        static ClassTypeDescription()
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName asmName = new AssemblyName("PublicBroadcastingDynamicClassAssembly");
            AssemblyBuilder asmBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

            Enumerator = BuildEnumerator();

            Expression<Func<object, string>> toString = obj => ToStringThunk.Call(obj);

            ToStringFunc = toString.Compile();
        }

        // kind of a giant HACK here, since I can do this in normal c#... but I need the class to exist in the dynamic module for visibility purposes.
        private static Type BuildEnumerator()
        {
            ILGenerator il;

            var enumerator = ModuleBuilder.DefineType("ClassEnumerator", TypeAttributes.Class, typeof(object), new[] { typeof(IEnumerator) });
            
            var callSite = typeof(System.Runtime.CompilerServices.CallSite<Func<System.Runtime.CompilerServices.CallSite, object, string, object>>);
            var callSiteField = enumerator.DefineField("_CallSite", callSite, FieldAttributes.Private | FieldAttributes.Static);
            
            var currentProp = enumerator.DefineProperty("Current", PropertyAttributes.None, typeof(object), Type.EmptyTypes);
            var currentField = enumerator.DefineField("_Current", typeof(object), FieldAttributes.Private);

            // Current { get; }
            var getCurrentEmit = Sigil.Emit<Func<object>>.BuildMethod(enumerator, "get_Current", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis);
            getCurrentEmit.LoadArgument(0);
            getCurrentEmit.LoadField(currentField);
            getCurrentEmit.Return();

            var getCurrent = getCurrentEmit.CreateMethod();

            currentProp.SetGetMethod(getCurrent);
            enumerator.DefineMethodOverride(getCurrent, typeof(IEnumerator).GetProperty("Current").GetGetMethod());

            // Enumerator(List<string>, object)
            var index = enumerator.DefineField("Index", typeof(int), FieldAttributes.Private);
            var members = enumerator.DefineField("Members", typeof(List<string>), FieldAttributes.Private);
            var values = enumerator.DefineField("Values", typeof(object), FieldAttributes.Private);
            var cons = enumerator.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(List<string>), typeof(object) });
            
            il = cons.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);       // [this]
            il.Emit(OpCodes.Ldarg_1);       // [List<string>] [this]
            il.Emit(OpCodes.Stfld, members);// -----
            il.Emit(OpCodes.Ldarg_0);       // [this]
            il.Emit(OpCodes.Ldarg_2);       // [object] [this]
            il.Emit(OpCodes.Stfld, values); // -----
            il.Emit(OpCodes.Ldarg_0);       // [this]
            il.Emit(OpCodes.Ldc_I4_M1);     // [-1] [this]
            il.Emit(OpCodes.Stfld, index);  // -----
            il.Emit(OpCodes.Ret);

            // void Reset()

            var resetEmit = Sigil.Emit<Action>.BuildMethod(enumerator, "Reset", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis);
            resetEmit.LoadArgument(0);
            resetEmit.LoadConstant(-1);
            resetEmit.StoreField(index);
            resetEmit.LoadArgument(0);
            resetEmit.LoadNull();
            resetEmit.StoreField(currentField);
            resetEmit.Return();

            var reset = resetEmit.CreateMethod();

            enumerator.DefineMethodOverride(reset, typeof(IEnumerator).GetMethod("Reset"));

            // bool MoveNext()
            var moveNextEmit = Sigil.Emit<Func<bool>>.BuildMethod(enumerator, "MoveNext", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis).AsShorthand();
            var getCount = typeof(List<string>).GetProperty("Count").GetGetMethod();
            var getItem = typeof(List<string>).GetProperty("Item").GetGetMethod();
            var entry = typeof(DictionaryEntry);
            var entryCons = entry.GetConstructor(new[] { typeof(object), typeof(object) });
            var argInfo = typeof(CSharpArgumentInfo);
            var createArgInfo = argInfo.GetMethod("Create");
            var getIndex = typeof(Microsoft.CSharp.RuntimeBinder.Binder).GetMethod("GetIndex");
            var createCallSite = callSite.GetMethod("Create");
            var callSiteTarget = callSite.GetField("Target");
            var callSiteInvoke = typeof(Func<System.Runtime.CompilerServices.CallSite, object, string, object>).GetMethod("Invoke");
            var typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

            Sigil.Label good, ready;
            Sigil.Local keyLocal;

            moveNextEmit
                .Ldarg(0)
                .Dup()
                .Ldfld(index)
                .Ldc(1)
                .Add()
                .Stfld(index);

            moveNextEmit
                .Ldarg(0)
                .Ldfld(index)
                .Ldarg(0)
                .Ldfld(members)
                .Callvirt(getCount);

            moveNextEmit
                .DefineLabel(out good, "Good")
                .Blt(good);

            moveNextEmit
                .Ldarg(0)
                .Ldnull()
                .Stfld(currentField)
                .Ldc(0)
                .Ret();

            moveNextEmit
                .MarkLabel(good)
                .Ldarg(0)
                .Dup()
                .Ldfld(members)
                .Ldarg(0)
                .Ldfld(index)
                .Callvirt(getItem);

            // --- begin dynamic
            moveNextEmit.DeclareLocal<string>(out keyLocal, "keyLocal");

            moveNextEmit
                .Dup()
                .Stloc(keyLocal);

            moveNextEmit.DefineLabel(out ready, "ready");

            moveNextEmit
                .Ldfld(callSiteField)
                .Brtrue(ready);

            moveNextEmit
                .Ldc(0)
                .Ldtoken(enumerator)
                .Call(typeFromHandle);

            moveNextEmit
                .Ldc(2)
                .Newarr(argInfo)
                .Dup()
                .Dup()
                .Ldc(0)
                .Ldc(0)
                .Ldnull()
                .Call(createArgInfo)
                .Stelem();

            moveNextEmit
                .Ldc(1)
                .Ldc(3)
                .Ldnull()
                .Call(createArgInfo)
                .Stelem();

            moveNextEmit.Call(getIndex);

            moveNextEmit
                .Call(createCallSite)
                .Stfld(callSiteField);

            moveNextEmit
                .MarkLabel(ready)
                .Ldfld(callSiteField)
                .Ldfld(callSiteTarget);

            moveNextEmit
                .Ldfld(callSiteField)
                .Ldarg(0)
                .Ldfld(values)
                .Ldloc(keyLocal);

            moveNextEmit.Callvirt(callSiteInvoke);
            // --- end dynamic

            moveNextEmit
                .Newobj(entryCons)
                .Box(entry)
                .Stfld(currentField);

            moveNextEmit
                .Ldc(1)
                .Ret();

            var moveNext = moveNextEmit.CreateMethod();

            enumerator.DefineMethodOverride(moveNext, typeof(IEnumerator).GetMethod("MoveNext"));

            return enumerator.CreateType();
        }

        internal override bool NeedsMapping
        {
            get 
            {
                return
                    AnyNonUniformMembers() ||
                    Members.Any(m => m.Value.NeedsMapping);
            }
        }

        [ProtoMember(1)]
        internal Dictionary<string, TypeDescription> Members { get; set; }

        [ProtoMember(2)]
        internal int Id { get; set; }

        internal Type ForType { get; set; }

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

            var name = "POCO" + Guid.NewGuid().ToString().Replace("-", "");

            var protoMemberAttr = typeof(ProtoMemberAttribute).GetConstructor(new[] { typeof(int) });
            var protoContractAttr = typeof(ProtoContractAttribute).GetConstructor(new Type[0]);

            var fields = new Dictionary<string, FieldInfo>();

            TypeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Public, typeof(DynamicObject), new [] { typeof(IEnumerable) });
            var ix = 1;
            foreach (var kv in Members.OrderBy(o => o.Key, StringComparer.Ordinal))
            {
                var memberAttrBuilder = new CustomAttributeBuilder(protoMemberAttr, new object[] { ix });

                kv.Value.Seal(existing);
                var propType = kv.Value.GetPocoType(existing);

                var field = TypeBuilder.DefineField(kv.Key, propType, FieldAttributes.Public);
                field.SetCustomAttribute(memberAttrBuilder);

                fields[kv.Key] = field;
                ix++;
            }

            var contractAttrBuilder = new CustomAttributeBuilder(protoContractAttr, new object[0]);
            TypeBuilder.SetCustomAttribute(contractAttrBuilder);

            // Define indexer
            var strEq = typeof(string).GetMethod("Equals", new[] { typeof(string) });

            var tryGetIndex = TypeBuilder.DefineMethod("TryGetIndex", MethodAttributes.Public | MethodAttributes.Virtual, typeof(bool), new[] { typeof(GetIndexBinder), typeof(object[]), Type.GetType("System.Object&") });
            var il = tryGetIndex.GetILGenerator();

            il.Emit(OpCodes.Ldarg_2);       // object[]

            var invalid = il.DefineLabel();
            il.Emit(OpCodes.Dup);                   // object[] object[]
            il.Emit(OpCodes.Ldlen);                 // length object[]
            il.Emit(OpCodes.Ldc_I4_1);              // 1 length object[]
            il.Emit(OpCodes.Bne_Un_S, invalid);     // object[]
            il.Emit(OpCodes.Ldc_I4_0);              // 0 object[]
            il.Emit(OpCodes.Ldelem_Ref);            // object
            il.Emit(OpCodes.Isinst, typeof(string));// bool

            var valid = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, valid);   // ----
            il.Emit(OpCodes.Ldc_I4_0);          // 0

            il.MarkLabel(invalid);              // (object[] or 0)
            il.Emit(OpCodes.Pop);               // ----

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
                var field = fields[memKey];

                il.Emit(OpCodes.Dup);               // key key (out object)
                il.Emit(OpCodes.Ldstr, memKey);     // memKey key key (out object)

                il.Emit(OpCodes.Callvirt, strEq);   // bool key (out object)

                il.Emit(OpCodes.Brfalse_S, next);   // key (out object)

                il.Emit(OpCodes.Pop);               // (out object)
                il.Emit(OpCodes.Ldarg_0);           // this (out object)
                il.Emit(OpCodes.Ldfld, field);      // ret (out object)

                if (field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, field.FieldType);  // ret (out object);
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
            var getEnumeratorEmit = Sigil.Emit<Func<IEnumerator>>.BuildMethod(TypeBuilder, "GetEnumerator", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis);
            var newStrList = typeof(List<string>).GetConstructor(new[] { typeof(int) });
            var newEnumerator = Enumerator.GetConstructor(new[] { typeof(List<string>), typeof(object) });
            var add = typeof(List<string>).GetMethod("Add");

            getEnumeratorEmit.LoadConstant(Members.Count);
            getEnumeratorEmit.NewObject(newStrList);

            foreach (var mem in Members)
            {
                getEnumeratorEmit.Duplicate();
                getEnumeratorEmit.LoadConstant(mem.Key);
                getEnumeratorEmit.Call(add);
            }

            getEnumeratorEmit.LoadArgument(0);
            getEnumeratorEmit.NewObject(newEnumerator);
            getEnumeratorEmit.Return();

            var getEnumerator = getEnumeratorEmit.CreateMethod();

            TypeBuilder.DefineMethodOverride(getEnumerator, typeof(IEnumerable).GetMethod("GetEnumerator"));

            // Define ToString()
            var toStringEmit = Sigil.Emit<Func<string>>.BuildMethod(TypeBuilder, "ToString", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis);
            var objToString = typeof(object).GetMethod("ToString");
            var thunkField = TypeBuilder.DefineField("__ToStringThunk", typeof(Func<object, string>), FieldAttributes.Static | FieldAttributes.Private);
            var invoke = typeof(Func<object, string>).GetMethod("Invoke");

            toStringEmit
                .LoadField(thunkField)
                .LoadArgument(0)
                .CallVirtual(invoke)
                .Return();

            var toString = toStringEmit.CreateMethod();

            TypeBuilder.DefineMethodOverride(toString, objToString);

            PocoType = TypeBuilder.CreateType();

            // Set the ToStringCallback

            var firstInst = PocoType.GetConstructor(Type.EmptyTypes).Invoke(new object[0]);

            var setThunk = firstInst.GetType().GetField("__ToStringThunk", BindingFlags.NonPublic | BindingFlags.Static);

            setThunk.SetValue(firstInst, ToStringFunc);
        }

        internal override Type GetPocoType(TypeDescription existing = null)
        {
            return PocoType ?? TypeBuilder;
        }

        private bool DePromised { get; set; }
        internal override TypeDescription DePromise(out Action afterPromise)
        {
            if (!DePromised)
            {
                DePromised = true;

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

        /// <summary>
        /// Returns true if any member chosen to be serialized is
        /// A) A property
        /// B) Has only a getter or only a setter
        /// </summary>
        /// <returns></returns>
        private bool AnyNonUniformMembers()
        {
            foreach (var mem in Members)
            {
                var prop = ForType.GetProperty(mem.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (prop == null) continue;

                if ((prop.CanRead && !prop.CanWrite) || (prop.CanWrite && !prop.CanRead))
                {
                    return true;
                }
            }

            return false;
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
