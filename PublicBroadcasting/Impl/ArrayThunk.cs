using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class ArrayThunk<T>
    {
        private static Action<Stream, object, IncludedMembers, IncludedVisibility> SerializeDelegate;
        private static Func<object, object> CreatePassthrough;

        static ArrayThunk()
        {
            var arrayType = typeof(T);

            var memberType = arrayType.GetElementType();

            var passThroughType = typeof(PassThroughList<>).MakeGenericType(memberType);

            var passThroughCons = passThroughType.GetConstructor(new[] { arrayType });

            var dynPassthrough = new DynamicMethod("PublicBroadcasting_ArrayThunk_CreatePassthrough_" + memberType.FullName, typeof(object), new[] { typeof(object) }, restrictedSkipVisibility: true);
            var il = dynPassthrough.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, arrayType);
            il.Emit(OpCodes.Newobj, passThroughCons);
            il.Emit(OpCodes.Ret);

            CreatePassthrough = (Func<object, object>)dynPassthrough.CreateDelegate(typeof(Func<object, object>));

            var serialize = typeof(Serializer).GetMethods().Single(m => m.Name == "Serialize" && m.GetParameters().Length == 4);

            var invoke = serialize.MakeGenericMethod(passThroughType);

            var dynSerial = new DynamicMethod("PublicBroadcasting_ArrayThunk_SerializeDelegate_" + memberType.FullName, null, new[] { typeof(Stream), typeof(object), typeof(IncludedMembers), typeof(IncludedVisibility) }, restrictedSkipVisibility: true);
            il = dynSerial.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, passThroughType);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Call, invoke);
            il.Emit(OpCodes.Ret);

            SerializeDelegate = (Action<Stream, object, IncludedMembers, IncludedVisibility>)dynSerial.CreateDelegate(typeof(Action<Stream, object, IncludedMembers, IncludedVisibility>));
        }

        public static void Serialize(Stream stream, object array, IncludedMembers members, IncludedVisibility visibility)
        {
            var passThrough = CreatePassthrough(array);

            SerializeDelegate(stream, passThrough, members, visibility);
        }
    }
}
