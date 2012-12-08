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
    internal class Describer
    {
        public static TypeDescription BuildDescription(Type describerType)
        {
            var t = describerType.GetGenericArguments()[0];

            var cutdownVisibility = describerType.GetMethod("GetVisibilityMask");
            var cutdownMembers = describerType.GetMethod("GetMemberMask");

            var visibilityMask = (IncludedVisibility)cutdownVisibility.Invoke(null, new object[0]);
            var membersMask = (IncludedMembers)cutdownMembers.Invoke(null, new object[0]);

            // from here on, the concrete type is useless, we care about the generic "parent" describer
            describerType = describerType.GetGenericTypeDefinition();

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

            if (t == typeof(DateTime)) return SimpleTypeDescription.DateTime;
            if (t == typeof(TimeSpan)) return SimpleTypeDescription.TimeSpan;

            if (t == typeof(Guid)) return SimpleTypeDescription.Guid;

            if (t == typeof(Uri)) return SimpleTypeDescription.Uri;

            if (Nullable.GetUnderlyingType(t) != null)
            {
                var nullT = t;

                var valType = nullT.GetGenericArguments()[0];

                var nullPromiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(nullT, describerType.MakeGenericType(nullT));
                var nullPromiseSingle = nullPromiseType.GetField("Singleton");
                var nullPromise = (PromisedTypeDescription)nullPromiseSingle.GetValue(null);

                var valDesc = describerType.MakeGenericType(valType).GetMethod("Get");
                var val = (TypeDescription)valDesc.Invoke(null, new object[0]);

                var nullRet = new NullableTypeDescription(val);

                nullPromise.Fulfil(nullRet);

                return nullRet;
            }

            if (t.IsEnum)
            {
                var single = typeof(EnumTypeDescription<>).MakeGenericType(t).GetField("Singleton");

                return (EnumTypeDescription)single.GetValue(null);
            }

            if ((t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
               t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                var dictI = 
                    (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ?
                    t :
                    t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

                var dictPromiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(dictI, describerType.MakeGenericType(dictI));
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
                var listI = 
                    (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ?
                    t :
                    t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

                var listPromiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(listI, describerType.MakeGenericType(listI));
                var listPromiseSingle = listPromiseType.GetField("Singleton");
                var listPromise = (PromisedTypeDescription)listPromiseSingle.GetValue(null);

                var valueType = listI.GetGenericArguments()[0];

                var valDesc = describerType.MakeGenericType(valueType).GetMethod("Get");

                TypeDescription val;

                val = (TypeDescription)valDesc.Invoke(null, new object[0]);

                var listRet = ListTypeDescription.Create(val, t);

                listPromise.Fulfil(listRet);

                return listRet;
            }

            var retType = typeof(ClassTypeDescription<,>).MakeGenericType(t, describerType.MakeGenericType(t));
            var retSingle = retType.GetField("Singleton");

            var ret = (TypeDescription)retSingle.GetValue(null);

            return ret;
        }
    }
}
