using ProtoBuf;
using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting
{
    public class Serializer
    {
        public static byte[] Serialize<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                Serialize(mem, obj);

                return mem.ToArray();
            }
        }

        public static void Serialize<T>(Stream stream, T obj)
        {
            Serialize(stream, obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
        }

        public static void Serialize<T>(Stream stream, T obj, IncludedMembers members)
        {
            Serialize(stream, obj, members, IncludedVisibility.Public);
        }

        public static void Serialize<T>(Stream stream, T obj, IncludedVisibility visibility)
        {
            Serialize(stream, obj, IncludedMembers.Properties | IncludedMembers.Fields, visibility);
        }

        public static void Serialize<T>(Stream stream, T obj, IncludedMembers members, IncludedVisibility visibility)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (members == 0) throw new ArgumentException("members");
            if (visibility == 0) throw new ArgumentException("visibility");

            if (members != (IncludedMembers.Fields | IncludedMembers.Properties)) throw new NotSupportedException("members must be Fields | Properties");
            if (visibility != IncludedVisibility.Public) throw new NotSupportedException("visibility must be Public");

            var desc = Describer<T>.Get();

            // Don't include an envelope unless it's needed, no point in wasting bytes
            if (!desc.NeedsEnvelope)
            {
                ProtoBuf.Serializer.Serialize(stream, Envelope.Get(new NoTypeDescription(), obj));
                return;
            }

            var mapper = POCOBuilder<T>.GetMapper(members, visibility);

            var payload = mapper(obj);

            var envelope = Envelope.Get(desc, payload);

            ProtoBuf.Serializer.Serialize(stream, envelope);
        }
    }
}
