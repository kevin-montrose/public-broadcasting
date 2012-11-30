using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class Envelope
    {
        [ProtoMember(1)]
        internal TypeDescription Description { get; set; }
        [ProtoMember(2)]
        internal byte[] Payload { get; set; }

        private Envelope() { }

        private Envelope(TypeDescription desc, byte[] payload)
        {
            Description = desc;
            Payload = payload;
        }

        public static Envelope Get(TypeDescription desc, object payload)
        {
            using(var mem = new MemoryStream())
            {
                ProtoBuf.Serializer.NonGeneric.Serialize(mem, payload);

                return new Envelope(desc, mem.ToArray());
            }
        }
    }
}
