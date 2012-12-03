using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class Deserializer
    {
        private static readonly Dictionary<TypeDescription, Type> TypeCache = new Dictionary<TypeDescription, Type>(new TypeDescriptionComparer());
        private static readonly Dictionary<Tuple<Type, Type>, Func<object, object>> MapperCache = new Dictionary<Tuple<Type, Type>, Func<object, object>>();

        internal static T Deserialize<T>(Stream stream)
        {
            Type type;
            var raw = DeserializeCore(stream, out type);

            var mapper = GetMapper(type, typeof(T));

            return (T)mapper(raw);
        }

        internal static dynamic Deserialize(Stream stream)
        {
            Type ignored;
            return DeserializeCore(stream, out ignored);
        }

        private static Func<object, object> GetMapper(Type pocoType, Type toType)
        {
            var key = Tuple.Create(pocoType, toType);
            lock (MapperCache)
            {
                Func<object, object> ret;
                if (MapperCache.TryGetValue(key, out ret))
                {
                    return ret;
                }

                var mapGetter = typeof(POCOMapper<,>).MakeGenericType(pocoType, toType).GetMethod("Get");

                var map = (POCOMapper)mapGetter.Invoke(null, new object[0]);

                ret = map.GetMapper();

                MapperCache[key] = ret;

                return ret;
            }
        }

        private static object DeserializeCore(Stream stream, out Type type)
        {
            var untyped = ProtoBuf.Serializer.Deserialize<Envelope>(stream);

            var effectDesc = untyped.Description;

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