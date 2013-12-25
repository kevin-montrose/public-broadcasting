using Sigil;
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

            var passthrough = Emit<Func<object, object>>.NewDynamicMethod("PublicBroadcasting_ArrayThunk_CreatePassthrough_" + memberType.FullName);
            passthrough.LoadArgument(0);
            passthrough.CastClass(arrayType);
            passthrough.NewObject(passThroughCons);
            passthrough.Return();

            CreatePassthrough = passthrough.CreateDelegate();

            var serialize = typeof(Serializer).GetMethods().Single(m => m.Name == "Serialize" && m.GetParameters().Length == 4);
            var invoke = serialize.MakeGenericMethod(passThroughType);

            var serializer = Emit<Action<Stream, object, IncludedMembers, IncludedVisibility>>.NewDynamicMethod("PublicBroadcasting_ArrayThunk_SerializeDelegate_" + memberType.FullName);
            serializer.LoadArgument(0);
            serializer.LoadArgument(1);
            serializer.CastClass(passThroughType);
            serializer.LoadArgument(2);
            serializer.LoadArgument(3);
            serializer.Call(invoke);
            serializer.Return();

            SerializeDelegate = serializer.CreateDelegate();
        }

        public static void Serialize(Stream stream, object array, IncludedMembers members, IncludedVisibility visibility)
        {
            var passThrough = CreatePassthrough(array);

            SerializeDelegate(stream, passThrough, members, visibility);
        }
    }
}
