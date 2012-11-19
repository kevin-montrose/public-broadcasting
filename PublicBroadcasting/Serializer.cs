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
            return Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
        }

        public static byte[] Serialize<T>(T obj, IncludedMembers members)
        {
            return Serialize(obj, members, IncludedVisibility.Public);
        }

        public static byte[] Serialize<T>(T obj, IncludedVisibility visibility)
        {
            return Serialize(obj, IncludedMembers.Properties | IncludedMembers.Properties, visibility);
        }

        public static byte[] Serialize<T>(T obj, IncludedMembers members, IncludedVisibility visibility)
        {
            using (var mem = new MemoryStream())
            {
                Serialize(mem, obj, members, visibility);

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

        private static void GetDescriptionAndBuilder<T>(IncludedMembers members, IncludedVisibility visibility, out TypeDescription description, out POCOBuilder builder)
        {
            if (visibility == IncludedVisibility.Public)
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicDescriber<T>>.GetMapper();
                    
                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicDescriber<T>>.GetMapper();

                    return;
                }

                throw new NotSupportedException();
            }

            throw new NotSupportedException();
        }

        public static void Serialize<T>(Stream stream, T obj, IncludedMembers members, IncludedVisibility visibility)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (members == 0) throw new ArgumentException("members");
            if (visibility == 0) throw new ArgumentException("visibility");

            TypeDescription desc;
            POCOBuilder builder;
            GetDescriptionAndBuilder<T>(members, visibility, out desc, out builder);

            var payload = builder.GetMapper()(obj);

            var envelope = Envelope.Get(desc, payload);

            ProtoBuf.Serializer.Serialize(stream, envelope);
        }
    }
}
