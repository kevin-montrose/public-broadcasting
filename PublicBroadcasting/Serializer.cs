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

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Internal))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicInternalDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Protected))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicProtectedDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicProtectedDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicProtectedDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Protected | IncludedVisibility.Internal))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Protected | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPublicInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPublicInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPublicInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPublicInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPublicInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPublicInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Public | IncludedVisibility.Internal | IncludedVisibility.Protected | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllAllDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllAllDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsAllDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsAllDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesAllDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesAllDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == IncludedVisibility.Protected)
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllProtectedDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsProtectedDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesProtectedDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesProtectedDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Protected | IncludedVisibility.Internal))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesProtectedInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesProtectedInternalDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Protected | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesProtectedPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesProtectedPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Protected | IncludedVisibility.Internal | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllProtectedInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllProtectedInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsProtectedInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsProtectedInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesProtectedInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesProtectedInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == IncludedVisibility.Internal)
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsInternalDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesInternalDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesInternalDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == (IncludedVisibility.Internal | IncludedVisibility.Private))
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesInternalPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesInternalPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            if (visibility == IncludedVisibility.Private)
            {
                if (members == (IncludedMembers.Properties | IncludedMembers.Fields))
                {
                    description = AllPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, AllPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Fields)
                {
                    description = FieldsPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, FieldsPrivateDescriber<T>>.GetMapper();

                    return;
                }

                if (members == IncludedMembers.Properties)
                {
                    description = PropertiesPrivateDescriber<T>.GetForUse(true);
                    builder = POCOBuilder<T, PropertiesPrivateDescriber<T>>.GetMapper();

                    return;
                }

                throw new ArgumentOutOfRangeException("members");
            }

            throw new ArgumentOutOfRangeException("visibility");
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
