using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    public class Deserializer
    {
        private static readonly Dictionary<TypeDescription, Type> TypeCache = new Dictionary<TypeDescription, Type>(new TypeDescriptionComparer());

        public static T Deserialize<T>(Stream stream)
        {
            Type type;
            var raw = DeserializeCore(stream, out type);

            var mapGetter = typeof(POCOMapper<,>).MakeGenericType(type, typeof(T)).GetMethod("Get");

            var arg = (POCOMapper)mapGetter.Invoke(null, new object[0]);

            return (T)arg.GetMapper()(raw);
        }

        public static dynamic Deserialize(Stream stream)
        {
            Type ignored;
            return DeserializeCore(stream, out ignored);
        }

        private static object DeserializeCore(Stream stream, out Type type)
        {
            var untyped = ProtoBuf.Serializer.Deserialize<Envelope>(stream);

            var effectDesc = untyped.Description;

            // TODO: Find a way to kill this lock
            lock (TypeCache)
            {
                if (!TypeCache.TryGetValue(effectDesc, out type))
                {
                    effectDesc.Seal(effectDesc);

                    type = effectDesc.GetPocoType(effectDesc);

                    TypeCache[effectDesc] = type;
                }
            }

            using (var mem = new MemoryStream(untyped.Payload))
            {
                var raw = ProtoBuf.Serializer.NonGeneric.Deserialize(type, mem);

                return raw;
            }
        }
    }
}