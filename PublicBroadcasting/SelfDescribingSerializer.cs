using ProtoBuf;
using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting
{
    public class SelfDescribingSerializer
    {
        public static byte[] Serialize<T>(T obj)
        {
            using (var mem = new MemoryStream())
            {
                Serialize(obj, mem);

                return mem.ToArray();
            }
        }

        public static void Serialize<T>(T obj, Stream stream)
        {
            Serialize(obj, IncludedMembers.Properties, IncludedVisibility.Public, stream);
        }

        public static void Serialize<T>(T obj, IncludedMembers members, Stream stream)
        {
            Serialize(obj, members, IncludedVisibility.Public, stream);
        }

        public static void Serialize<T>(T obj, IncludedVisibility visibility, Stream stream)
        {
            Serialize(obj, IncludedMembers.Properties, visibility, stream);
        }

        public static void Serialize<T>(T obj, IncludedMembers members, IncludedVisibility visibility, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (members == 0) throw new ArgumentException("members");
            if (visibility == 0) throw new ArgumentException("visibility");

            var desc = Describer<T>.Get(members, visibility);
        }
    }
}
