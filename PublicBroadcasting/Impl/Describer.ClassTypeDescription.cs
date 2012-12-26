using Microsoft.CSharp.RuntimeBinder;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    public class ToStringThunk
    {
        private static string EscapeString(string str)
        {
            return str.Replace("\"", @"\""");
        }

        private static string Indent(string str, int by)
        {
            var byStr = "";
            for (var i = 0; i < by; i++)
            {
                byStr += " ";
            }

            return Regex.Replace(str, @"^", @byStr, RegexOptions.Multiline);
        }

        private static List<dynamic> OrderDynamic(dynamic dyn)
        {
            var ret = new List<dynamic>();

            foreach (var kv in dyn)
            {
                ret.Add(new { Key = (string)kv.Key, Value = kv.Value });
            }

            ret = (List<dynamic>)ret.OrderBy(o => o.Key).ToList();

            return ret;
        }

        public static string Call(dynamic val)
        {
            if (val == null)
            {
                return "null";
            }

            if (val is string)
            {
                return "\"" + EscapeString((string)val) + "\"";
            }

            if (Nullable.GetUnderlyingType(val.GetType()) != null)
            {
                val = val.Value;
            }

            var valType = (Type)val.GetType();

            if (valType == typeof(byte) || valType == typeof(sbyte) || valType == typeof(short) || valType == typeof(ushort) || valType == typeof(int) || valType == typeof(uint) ||
                valType == typeof(long) || valType == typeof(ulong) || valType == typeof(float) || valType == typeof(double) || valType == typeof(decimal) || valType == typeof(bool) ||
                valType == typeof(char))
            {
                return val.ToString();
            }

            if (val is Guid)
            {
                return val.ToString("D");
            }

            if (val is Uri)
            {
                if (val.IsAbsoluteUri)
                {
                    return val.AbsoluteUri;
                }
                else
                {
                    return val.ToString();
                }
            }

            if (val is TimeSpan)
            {
                return val.ToString("c");
            }

            if (val is DateTime)
            {
                return val.ToString("u");
            }

            if (valType.IsList())
            {
                var parts = new List<string>();

                foreach (var part in val)
                {
                    parts.Add(Call(part));
                }

                var containsType = valType.GetListInterface().GetGenericArguments()[0];

                if (containsType.IsSimple())
                {
                    return "[" + string.Join(", ", parts) + "]";
                }
                else
                {
                    return "[" + Environment.NewLine + Indent(string.Join("," + Environment.NewLine, parts), 1) + Environment.NewLine + "]";
                }
            }

            if (valType.IsDictionary())
            {
                var parts = new List<string>();

                foreach (var kv in val)
                {
                    var dKey = (string)Call(kv.Key);
                    var dVal = (string)Call(kv.Value);

                    if (dKey.Contains(Environment.NewLine) || dVal.Contains(Environment.NewLine))
                    {
                        parts.Add("{" + Environment.NewLine + Indent(dKey, 2) + "" + Environment.NewLine + "   ->" + Environment.NewLine + Indent(dVal, 2) + Environment.NewLine + " }");
                    }
                    else
                    {
                        parts.Add("{" + dKey + " -> " + dVal + "}");
                    }
                }

                parts = parts.OrderBy(o => o).ToList();

                return "{" + Environment.NewLine + " " + string.Join("," + Environment.NewLine + " ", parts) + Environment.NewLine + "}";
            }

            var ret = new StringBuilder();
            var first = true;

            ret.AppendLine("{");
            var inOrder = OrderDynamic(val);
            foreach (var kv in inOrder)
            {
                var propName = (string)kv.Key;
                var propVal = kv.Value;

                if (!first)
                {
                    ret.AppendLine(",");
                }

                first = false;

                ret.Append(" ");
                ret.Append(propName);
                ret.Append(": ");

                var propValStr = Call(propVal);

                if (propValStr.Contains(Environment.NewLine))
                {
                    ret.AppendLine();
                    ret.Append(Indent(propValStr, 2));
                }
                else
                {
                    ret.Append(propValStr);
                }
            }

            ret.AppendLine();
            ret.Append("}");

            return ret.ToString();
        }
    }

    [ProtoContract]
    internal class ClassTypeDescription : TypeDescription
    {
        static readonly ModuleBuilder ModuleBuilder;
        static readonly Type Enumerator;

        static ClassTypeDescription()
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName asmName = new AssemblyName("PublicBroadcastingDynamicClassAssembly");
            AssemblyBuilder asmBuilder = domain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

            Enumerator = BuildEnumerator();
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
            var getCurrent = enumerator.DefineMethod("get_Current", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object), Type.EmptyTypes);
            
            il = getCurrent.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Ldfld, currentField);   // [ret]
            il.Emit(OpCodes.Ret);                   // -----

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
            var reset = enumerator.DefineMethod("Reset", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, null, Type.EmptyTypes);
            
            il = reset.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Ldc_I4_M1);             // [-1] [this]
            il.Emit(OpCodes.Stfld, index);          // -----
            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Ldnull);                // [null] [this]
            il.Emit(OpCodes.Stfld, currentField);   // -----
            il.Emit(OpCodes.Ret);                   // -----

            enumerator.DefineMethodOverride(reset, typeof(IEnumerator).GetMethod("Reset"));

            // bool MoveNext()
            var moveNext = enumerator.DefineMethod("MoveNext", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(bool), Type.EmptyTypes);
            var getCount = typeof(List<string>).GetProperty("Count").GetGetMethod();
            var getItem = typeof(List<string>).GetProperty("Item").GetGetMethod();
            var entry = typeof(DictionaryEntry);
            var entryCons = entry.GetConstructor(new[] { typeof(object), typeof(object) });

            il = moveNext.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Dup);                   // [this] [this]
            il.Emit(OpCodes.Ldfld, index);          // [index] [this]
            il.Emit(OpCodes.Ldc_I4_1);              // [1] [index] [this]
            il.Emit(OpCodes.Add);                   // [index+1] [this]
            il.Emit(OpCodes.Stfld, index);          // -----

            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Ldfld, index);          // [index]
            il.Emit(OpCodes.Ldarg_0);               // [this] [index]
            il.Emit(OpCodes.Ldfld, members);        // [members] [index]
            il.Emit(OpCodes.Callvirt, getCount);    // [count] [index]

            var good = il.DefineLabel();
            il.Emit(OpCodes.Blt_S, good);           // -----

            il.Emit(OpCodes.Ldarg_0);               // [this]
            il.Emit(OpCodes.Ldnull);                // [null] [this]
            il.Emit(OpCodes.Stfld, currentField);   // -----
            il.Emit(OpCodes.Ldc_I4_0);              // 0
            il.Emit(OpCodes.Ret);                   // -----

            il.MarkLabel(good);                     // -----
            il.Emit(OpCodes.Ldarg_0);               // [this] 
            il.Emit(OpCodes.Dup);                   // [this] [this]
            il.Emit(OpCodes.Ldfld, members);        // [members] [this]
            il.Emit(OpCodes.Ldarg_0);               // [this] [members] [this]
            il.Emit(OpCodes.Ldfld, index);          // [index] [members] [this]
            il.Emit(OpCodes.Callvirt, getItem);     // [key] [this]

            // --- begin dynamic
            var argInfo = typeof(CSharpArgumentInfo);
            var createArgInfo = argInfo.GetMethod("Create");
            var getIndex = typeof(Microsoft.CSharp.RuntimeBinder.Binder).GetMethod("GetIndex");
            var createCallSite = callSite.GetMethod("Create");
            var callSiteTarget = callSite.GetField("Target");
            var callSiteInvoke = typeof(Func<System.Runtime.CompilerServices.CallSite, object, string, object>).GetMethod("Invoke");
            var typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

            var keyLocal = il.DeclareLocal(typeof(string));
            
            il.Emit(OpCodes.Dup);                                   // [key] [key] [this]
            il.Emit(OpCodes.Stloc, keyLocal);                       // [key] [this]

            var ready = il.DefineLabel();
            il.Emit(OpCodes.Ldsfld, callSiteField);                 // [CallSite<T>] [key] [this]
            il.Emit(OpCodes.Brtrue_S, ready);                       // [key] [this]

            il.Emit(OpCodes.Ldc_I4_0);                              // [0] [key] [this]
            il.Emit(OpCodes.Ldtoken, enumerator);                   // [ClassEnumerator token] [0] [key] [this]
            il.Emit(OpCodes.Call, typeFromHandle);                  // [typeof(ClassEnumerator)] [0] [key] [this]

            il.Emit(OpCodes.Ldc_I4_2);                              // [2] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Newarr, argInfo);                       // [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Dup);                                   // [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Dup);                                   // [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Ldc_I4_0);                              // [0] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Ldc_I4_0);                              // [0] [0] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Ldnull);                                // [null] [0] [0] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Call, createArgInfo);                   // [CSharpArgumentInfo] [0] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Stelem_Ref);                            // [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]

            il.Emit(OpCodes.Ldc_I4_1);                              // [1] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Ldc_I4_3);                              // [3] [1] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Ldnull);                                // [null] [3] [1] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Call, createArgInfo);                   // [CSharpArgumentInfo] [1] [CSharpArgumentInfo[]] [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]
            il.Emit(OpCodes.Stelem_Ref);                            // [CSharpArgumentInfo[]] [typeof(ClassEnumerator)] [0] [key] [this]

            il.Emit(OpCodes.Call, getIndex);                        // [CallSiteBinder] [key] [this]

            il.Emit(OpCodes.Call, createCallSite);                  // [CallSite<T>] [key] [this]
            il.Emit(OpCodes.Stsfld, callSiteField);                 // [key] [this]

            il.MarkLabel(ready);
            il.Emit(OpCodes.Ldsfld, callSiteField);                 // [CallSite<T>] [key] [this]
            il.Emit(OpCodes.Ldfld, callSiteTarget);                 // [CallSite<T>.Target] [key] [this]

            il.Emit(OpCodes.Ldsfld, callSiteField);                 // [CallSite<T>] [CallSite<T>.Target] [key] [this]
            il.Emit(OpCodes.Ldarg_0);                               // [this] [CallSite<T>] [CallSite<T>.Target] [key] [this]
            il.Emit(OpCodes.Ldfld, values);                         // [Values] [CallSite<T>] [CallSite<T>.Target] [key] [this]
            il.Emit(OpCodes.Ldloc, keyLocal);                       // [key] [Values] [CallSite<T>] [CallSite<T>.Target] [key] [this]

            il.Emit(OpCodes.Callvirt, callSiteInvoke);              // [value] [key] [this]

            // --- end dynamic

            il.Emit(OpCodes.Newobj, entryCons);     // [entry] [this]
            il.Emit(OpCodes.Box, entry);            // [entry as object] [this]
            il.Emit(OpCodes.Stfld, currentField);   // -----

            il.Emit(OpCodes.Ldc_I4_1);              // 1
            il.Emit(OpCodes.Ret);                   // -----

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
            var getEnumerator = TypeBuilder.DefineMethod("GetEnumerator", MethodAttributes.Public | MethodAttributes.Virtual, typeof(IEnumerator), Type.EmptyTypes);
            il = getEnumerator.GetILGenerator();

            var newStrList = typeof(List<string>).GetConstructor(new[] { typeof(int) });
            var newEnumerator = Enumerator.GetConstructor(new[] { typeof(List<string>), typeof(object) });
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
            il.Emit(OpCodes.Ret);                   // -----

            TypeBuilder.DefineMethodOverride(getEnumerator, typeof(IEnumerable).GetMethod("GetEnumerator"));

            // Define ToString()
            var toString = TypeBuilder.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string), Type.EmptyTypes);
            var objToString = typeof(object).GetMethod("ToString");

            il = toString.GetILGenerator();

            var sbCons = typeof(StringBuilder).GetConstructor(Type.EmptyTypes);
            var sbAppend = typeof(StringBuilder).GetMethod("Append", new [] { typeof(string) });
            var sbToString = typeof(StringBuilder).GetMethod("ToString", Type.EmptyTypes);
            var contains = typeof(string).GetMethod("Contains");
            var replace = typeof(Regex).GetMethod("Replace", new[] { typeof(string), typeof(string), typeof(string), typeof(RegexOptions) });

            var loc = il.DeclareLocal(typeof(StringBuilder));

            il.Emit(OpCodes.Newobj, sbCons);        // [ret]
            il.Emit(OpCodes.Stloc, loc);            // -----
            il.Emit(OpCodes.Ldloc, loc);            // [ret]

            il.Emit(OpCodes.Ldstr, "{" + Environment.NewLine);  // ["..."] [ret]
            il.Emit(OpCodes.Call, sbAppend);                    // [ret]

            var first = true;

            foreach (var prop in Members.OrderBy(o => o.Key))
            {
                var field = fields[prop.Key];

                if (!first)
                {
                    il.Emit(OpCodes.Ldstr, ","+Environment.NewLine);    // ["..."] [ret]
                    il.Emit(OpCodes.Call, sbAppend);                    // [ret]
                }

                first = false;

                il.Emit(OpCodes.Ldstr, " " + prop.Key + ": ");  // ["..."] [ret]
                il.Emit(OpCodes.Call, sbAppend);                // [ret]

                il.Emit(OpCodes.Ldarg_0);                   // [this] [ret]
                il.Emit(OpCodes.Ldfld, field);              // [field] [ret]

                if (field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, field.FieldType);  // [field] [ret]
                }

                il.Emit(OpCodes.Dup);                       // [field] [field] [ret]
                il.Emit(OpCodes.Ldnull);                    // [null] [field] [field] [ret]
                il.Emit(OpCodes.Ceq);                       // [isNull] [field] [ret]

                var contL = il.DefineLabel();
                var end = il.DefineLabel();
                var noIndent = il.DefineLabel();

                il.Emit(OpCodes.Brfalse_S, contL);          // [field] [ret]

                il.Emit(OpCodes.Pop);                       // [ret]
                il.Emit(OpCodes.Ldstr, "null");             // ["null"] [ret]
                il.Emit(OpCodes.Callvirt, sbAppend);        // [ret]
                il.Emit(OpCodes.Br_S, end);                 // [ret]

                il.MarkLabel(contL);                        // [field] [ret]

                Type effectiveType = field.FieldType;

                if (Nullable.GetUnderlyingType(effectiveType) != null)
                {
                    var getValue = effectiveType.GetProperty("Value").GetGetMethod();

                    effectiveType = Nullable.GetUnderlyingType(effectiveType);

                    il.Emit(OpCodes.Call, getValue);            // [field] [ret]
                    il.Emit(OpCodes.Box, effectiveType);        // [field] [ret]
                }

                if (effectiveType == typeof(DateTime))
                {
                    var dtToString = typeof(DateTime).GetMethod("ToString", new[] { typeof(string) });

                    il.Emit(OpCodes.Ldstr, "u");        // ["..."] [field] [ret]
                    il.Emit(OpCodes.Call, dtToString);  // [string] [ret]
                }
                else
                {
                    if (effectiveType == typeof(Guid))
                    {
                        var gToString = typeof(Guid).GetMethod("ToString", new[] { typeof(string) });

                        il.Emit(OpCodes.Ldstr, "D");        // ["..."] [field] [ret]
                        il.Emit(OpCodes.Call, gToString);   // [string] [ret]
                    }
                    else
                    {
                        if (effectiveType == typeof(TimeSpan))
                        {
                            var tsToString = typeof(TimeSpan).GetMethod("ToString", new[] { typeof(string) });

                            il.Emit(OpCodes.Ldstr, "c");        // ["..."] [field] [ret]
                            il.Emit(OpCodes.Call, tsToString);  // ["..."] [ret]
                        }
                        else
                        {
                            if (effectiveType == typeof(Uri))
                            {
                                var isAbsolute = typeof(Uri).GetProperty("IsAbsoluteUri").GetGetMethod();
                                var notAbsL = il.DefineLabel();
                                var getAbs = typeof(Uri).GetProperty("AbsoluteUri").GetGetMethod();

                                il.Emit(OpCodes.Dup);               // [field] [field] [ret]
                                il.Emit(OpCodes.Call, isAbsolute);  // [bool] [field] [ret]
                                il.Emit(OpCodes.Brfalse_S, notAbsL);// [field] [ret]

                                il.Emit(OpCodes.Call, getAbs);      // [string] [ret]

                                il.MarkLabel(notAbsL);              // [string/field] [ret]
                                il.Emit(OpCodes.Callvirt, toString);// [string] [ret]

                            }
                            else
                            {
                                if (effectiveType.IsList())
                                {
                                    var containsType = effectiveType.GetListInterface().GetGenericArguments()[0];
                                    var allStatic = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static);
                                    var joins = allStatic.Where(m => m.Name == "Join").ToList();
                                    joins = joins.Where(w => w.GetParameters().Length == 2).ToList();
                                    joins = joins.Where(w => w.GetParameters()[1].ParameterType.IsGenericType && w.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToList();
                                    var join = joins.Single(w => w.IsGenericMethod);
                                    join = join.MakeGenericMethod(containsType);

                                    var list = il.DeclareLocal(effectiveType);

                                    if (containsType.IsSimple())
                                    {
                                        var tempLoc = il.DeclareLocal(typeof(StringBuilder));
                                        il.Emit(OpCodes.Newobj, sbCons);        // [temp] [list] [ret]
                                        il.Emit(OpCodes.Stloc, tempLoc);        // [list] [ret]
                                        il.Emit(OpCodes.Ldloc, tempLoc);        // [temp] [list] [ret]

                                        il.Emit(OpCodes.Ldstr, "[");            // ["..."] [temp] [list] [ret]
                                        il.Emit(OpCodes.Callvirt, sbAppend);    // [temp] [list] [ret]
                                        il.Emit(OpCodes.Pop);                   // [list] [ret]
                                        il.Emit(OpCodes.Stloc, list);           // [ret]
                                        il.Emit(OpCodes.Ldloc, tempLoc);        // [temp] [ret]
                                        il.Emit(OpCodes.Ldstr, ", ");           // ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Ldloc, list);           // [list] ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Call, join);            // [string] [temp] [ret]
                                        il.Emit(OpCodes.Callvirt, sbAppend);    // [temp] [ret]
                                        il.Emit(OpCodes.Ldstr, "]");            // ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Callvirt, sbAppend);    // [temp] [ret]
                                        il.Emit(OpCodes.Call, sbToString);      // [string] [ret]
                                    }
                                    else
                                    {
                                        var tempLoc = il.DeclareLocal(typeof(StringBuilder));
                                        il.Emit(OpCodes.Newobj, sbCons);        // [temp] [list] [ret]
                                        il.Emit(OpCodes.Stloc, tempLoc);        // [list] [ret]
                                        il.Emit(OpCodes.Ldloc, tempLoc);        // [temp] [list] [ret]

                                        il.Emit(OpCodes.Ldstr, "[" + Environment.NewLine);  // ["..."] [temp] [list] [ret]
                                        il.Emit(OpCodes.Callvirt, sbAppend);                // [temp] [list] [ret]
                                        il.Emit(OpCodes.Pop);                               // [list] [ret]
                                        il.Emit(OpCodes.Stloc, list);                       // [ret]
                                        il.Emit(OpCodes.Ldloc, tempLoc);                    // [temp] [ret]
                                        il.Emit(OpCodes.Ldstr, "," + Environment.NewLine);  // ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Ldloc, list);                       // [list] ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Call, join);                        // [string] [temp] [ret]

                                        // Regex.Replace
                                        il.Emit(OpCodes.Ldstr, "^");                            // ["..."] [string] [temp] [ret]
                                        il.Emit(OpCodes.Ldstr, " ");                            // ["..."] ["..."] [string] [temp] [ret]
                                        il.Emit(OpCodes.Ldc_I4, (int)RegexOptions.Multiline);   // [Multiline] ["..."] ["..."] [string] [temp] [ret]
                                        il.Emit(OpCodes.Call, replace);                         // [string] [temp] [ret]

                                        il.Emit(OpCodes.Callvirt, sbAppend);                    // [temp] [ret]
                                        il.Emit(OpCodes.Ldstr, Environment.NewLine + "]");      // ["..."] [temp] [ret]
                                        il.Emit(OpCodes.Callvirt, sbAppend);                    // [temp] [ret]
                                        il.Emit(OpCodes.Call, sbToString);                      // [string] [ret]
                                    }

                                    /*if (containsType.IsSimple())
                                    {
                                        return "[" + string.Join(", ", parts) + "]";
                                    }
                                    else
                                    {
                                        return "[" + Environment.NewLine + Indent(string.Join("," + Environment.NewLine, parts), 1) + Environment.NewLine + "]";
                                    }*/
                                }
                                else
                                {
                                    il.Emit(OpCodes.Callvirt, toString);        // [string] [ret]
                                }
                            }
                        }
                    }
                }

                il.Emit(OpCodes.Dup);                       // [string] [string] [ret]
                il.Emit(OpCodes.Ldstr, Environment.NewLine);// ["..."] [string] [string] [ret]
                il.Emit(OpCodes.Call, contains);            // [bool] [string] [ret]
                il.Emit(OpCodes.Brfalse_S, noIndent);       // [string] [ret]

                //Regex.Replace(str, @"^", @byStr, RegexOptions.Multiline);

                il.Emit(OpCodes.Ldloc, loc);                // [ret] [string] [ret]
                il.Emit(OpCodes.Ldstr, Environment.NewLine);// ["..."] [ret] [string] [ret]
                il.Emit(OpCodes.Call, sbAppend);            // [ret] [string] [ret]
                il.Emit(OpCodes.Pop);                       // [string] [ret]

                il.Emit(OpCodes.Ldstr, "^");                            // ["..."] [string] [ret]
                il.Emit(OpCodes.Ldstr, "  ");                           // ["..."] ["..."] [string] [ret]
                il.Emit(OpCodes.Ldc_I4, (int)RegexOptions.Multiline);   // [Multiline] ["..."] ["..."] [string] [ret]
                il.Emit(OpCodes.Call, replace);                         // [string] [ret]

                il.MarkLabel(noIndent);                     // [string] [ret]
                il.Emit(OpCodes.Callvirt, sbAppend);        // [ret]

                il.MarkLabel(end);                          // [ret]
            }

            il.Emit(OpCodes.Ldstr, Environment.NewLine + "}");  // ["..."] [ret]
            il.Emit(OpCodes.Call, sbAppend);                    // [ret]

            il.Emit(OpCodes.Call, sbToString);          // [ret as string]
            il.Emit(OpCodes.Ret);                       // -----

            TypeBuilder.DefineMethodOverride(toString, objToString);

            PocoType = TypeBuilder.CreateType();
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
