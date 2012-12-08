using ProtoBuf;
using ProtoBuf.Meta;
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
        private const int CurrentVersion = 0;

        [ProtoMember(1)]
        internal byte Version { get; set; }
        [ProtoMember(2)]
        internal TypeDescription Description { get; set; }
        [ProtoMember(3)]
        internal byte[] Payload { get; set; }

        private Envelope() { }

        private Envelope(TypeDescription desc, byte[] payload)
        {
            Version = CurrentVersion;
            Description = desc;
            Payload = payload;
        }

        public static Envelope Get(TypeDescription desc, POCOBuilder mapper, object payload)
        {
            using(var mem = new MemoryStream())
            {
                // null *can* be serialized, it just has no value
                if (payload != null)
                {
                    // A lot of models can skip the whole "create shadow type" operation, so let's squeeze those savings out
                    if (!desc.NeedsMapping)
                    {
                        var model = desc.TypeModel ?? RuntimeTypeModel.Default;

                        model.Serialize(mem, payload);
                    }
                    else
                    {
                        payload = mapper.GetMapper()(payload);

                        ProtoBuf.Serializer.NonGeneric.Serialize(mem, payload);
                    }
                }

                return new Envelope(desc, mem.ToArray());
            }
        }
    }
}
