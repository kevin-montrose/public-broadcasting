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
    internal class Describer<T>
    {
        private static readonly PromisedTypeDescription AllPublicPromise;
        private static readonly TypeDescription AllPublic;

        static Describer()
        {
            Debug.WriteLine("Describer: " + typeof(T).FullName);

            AllPublicPromise = PromisedTypeDescription<T>.Singleton;
            
            var allPublic = BuildDescription(typeof(Describer<>));

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

        public static TypeDescription BuildDescription(Type describerType)
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

            if (Nullable.GetUnderlyingType(t) != null)
            {
                var nullT = t;

                var nullPromiseType = typeof(PromisedTypeDescription<>).MakeGenericType(nullT);
                var nullPromiseSingle = nullPromiseType.GetField("Singleton");
                var nullPromise = (PromisedTypeDescription)nullPromiseSingle.GetValue(null);

                var valType = nullT.GetGenericArguments()[0];

                var valDesc = describerType.MakeGenericType(valType).GetMethod("Get");
                var val = (TypeDescription)valDesc.Invoke(null, new object[0]);

                var nullRet = new NullableTypeDescription(val);

                nullPromise.Fulfil(nullRet);

                return nullRet;
            }

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                var dictI = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                var dictPromiseType = typeof(PromisedTypeDescription<>).MakeGenericType(dictI);
                var dictPromiseSingle = dictPromiseType.GetField("Singleton");
                var dictPromise = (PromisedTypeDescription)dictPromiseSingle.GetValue(null);

                var keyType = dictI.GetGenericArguments()[0];
                var valueType = dictI.GetGenericArguments()[1];

                var keyDesc = describerType.MakeGenericType(keyType).GetMethod("Get");
                var valDesc = describerType.MakeGenericType(valueType).GetMethod("Get");

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

                var listPromiseType = typeof(PromisedTypeDescription<>).MakeGenericType(listI);
                var listPromiseSingle = listPromiseType.GetField("Singleton");
                var listPromise = (PromisedTypeDescription)listPromiseSingle.GetValue(null);

                var valueType = listI.GetGenericArguments()[0];

                var valDesc = describerType.MakeGenericType(valueType).GetMethod("Get");

                TypeDescription val;

                val = (TypeDescription)valDesc.Invoke(null, new object[0]);

                var listRet = ListTypeDescription.Create(val);

                listPromise.Fulfil(listRet);

                return listRet;
            }

            var get = (typeof(TypeReflectionCache<>).MakeGenericType(t)).GetMethod("Get");

            var cutdown = (CutdownType)get.Invoke(null, new object[] { IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public });

            var classMembers = new Dictionary<string, TypeDescription>(cutdown.Properties.Count + cutdown.Fields.Count);

            foreach (var prop in cutdown.Properties)
            {
                var propName = prop.Name;

                var propType = prop.PropertyType;

                var propDesc = describerType.MakeGenericType(propType).GetMethod("Get");

                classMembers[propName] = (TypeDescription)propDesc.Invoke(null, new object[0]);
            }

            foreach (var field in cutdown.Fields)
            {
                var fieldName = field.Name;
                var fieldType = field.FieldType;

                var fieldDesc = describerType.MakeGenericType(fieldType).GetMethod("Get");

                classMembers[fieldName] = (TypeDescription)fieldDesc.Invoke(null, new object[0]);
            }

            var retType = typeof(ClassTypeDescription<,>).MakeGenericType(t, describerType.MakeGenericType(typeof(object)));
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
