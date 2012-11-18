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
            var untyped = ProtoBuf.Serializer.Deserialize<Envelope>(stream);

            if (untyped.Description is NoTypeDescription)
            {
                using(var mem = new MemoryStream(untyped.Payload))
                {
                    return ProtoBuf.Serializer.Deserialize<T>(mem);
                }
            }

            var effectDesc = untyped.Description.FromCache();

            effectDesc.Seal(untyped.Description);

            var type = effectDesc.GetPocoType(untyped.Description);

            var arg = POCOMapper<T>.GetMapper(type);

            using(var mem = new MemoryStream(untyped.Payload))
            {
                var raw = ProtoBuf.Serializer.NonGeneric.Deserialize(type, mem);

                return (T)arg(raw);
            }
        }
    }
}