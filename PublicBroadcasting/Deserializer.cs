using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting
{
    public class Deserializer
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return Deserialize<T>(mem);
            }
        }

        public static T Deserialize<T>(Stream stream)
        {
            TypeDescription desc;
            var raw = DeserializeCore(stream, out desc);

            var type = desc.GetPocoType(desc);

            var mapGetter = typeof(POCOMapper<,>).MakeGenericType(type, typeof(T)).GetMethod("Get");

            var arg = (POCOMapper)mapGetter.Invoke(null, new object[0]);

            return (T)arg.GetMapper()(raw);
        }

        public static dynamic Deserialize(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return Deserialize(mem);
            }
        }

        public static dynamic Deserialize(Stream stream)
        {
            TypeDescription ignored;
            return DeserializeCore(stream, out ignored);
        }

        private static object DeserializeCore(Stream stream, out TypeDescription effectDesc)
        {
            var untyped = ProtoBuf.Serializer.Deserialize<Envelope>(stream);

            effectDesc = untyped.Description;

            effectDesc.Seal(effectDesc);

            var type = effectDesc.GetPocoType(effectDesc);

            using (var mem = new MemoryStream(untyped.Payload))
            {
                var raw = ProtoBuf.Serializer.NonGeneric.Deserialize(type, mem);

                return raw;
            }
        }
    }
}