using ProtoBuf;
using System;
using System.Collections.Generic;
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
        internal object Payload { get; set; }

        public Envelope(TypeDescription desc, object payload)
        {
            Description = desc;
            Payload = payload;
        }
    }
}
