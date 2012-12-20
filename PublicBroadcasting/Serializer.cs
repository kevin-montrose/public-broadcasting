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
    /// <summary>
    /// Provides serialization and deserialization methods for a self-describing format
    /// built on top of protocol buffers.
    /// </summary>
    /// <remarks>
    /// Public Broadcasting is built on top of protobuf-net, and by design will always be
    /// somewhat slower and larger.  If you need higher performace or more compact results,
    /// consider using protobuf-net directly; but be aware that protobuf-net requires
    /// manual member anotation and versioning.
    /// </remarks>
    public class Serializer
    {
        /// <summary>
        /// Serializes public fields and properties of the given instance to a byte array.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <returns>A byte array representing the public fields and properties of obj.</returns>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static byte[] Serialize<T>(T obj)
        {
            return Serialize(obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
        }

        /// <summary>
        /// Serializes the specified public members of the given instance to a byte array.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="members">The members to serialize, either Fields, Properties, or both.</param>
        /// <returns>A byte array representing the specified public members of obj.</returns>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static byte[] Serialize<T>(T obj, IncludedMembers members)
        {
            return Serialize(obj, members, IncludedVisibility.Public);
        }

        /// <summary>
        /// Serializes the specified fields and properties of the given instance to a byte array.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="visibility">The visibility of fields and properties to serialize, some combination of Public, Protected, Internal, and Private.</param>
        /// <returns>A byte array representing the specified fields and properties of obj.</returns>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static byte[] Serialize<T>(T obj, IncludedVisibility visibility)
        {
            return Serialize(obj, IncludedMembers.Properties | IncludedMembers.Properties, visibility);
        }

        /// <summary>
        /// Serializes the specified members of the given instance to a byte array.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="members">The members to serialize, either Fields, Properties, or both.</param>
        /// <param name="visibility">The visibility of fields and properties to serialize, some combination of Public, Protected, Internal, and Private.</param>
        /// <returns>A byte array representing the specified members of obj.</returns>
        public static byte[] Serialize<T>(T obj, IncludedMembers members, IncludedVisibility visibility)
        {
            using (var mem = new MemoryStream())
            {
                Serialize(mem, obj, members, visibility);

                return mem.ToArray();
            }
        }

        /// <summary>
        /// Serializes public fields and properties of the given instance to the given Stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="stream">The stream to serialize obj to.</param>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static void Serialize<T>(Stream stream, T obj)
        {
            Serialize(stream, obj, IncludedMembers.Properties | IncludedMembers.Fields, IncludedVisibility.Public);
        }

        /// <summary>
        /// Serializes the specified public members of the given instance to the given Stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="stream">The stream to serialize obj to.</param>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="members">The members to serialize, either Fields, Properties, or both.</param>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static void Serialize<T>(Stream stream, T obj, IncludedMembers members)
        {
            Serialize(stream, obj, members, IncludedVisibility.Public);
        }

        /// <summary>
        /// Serializes the specified fields and properties of the given instance to the given Stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="stream">The stream to serialize obj to.</param>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="visibility">The visibility of fields and properties to serialize, some combination of Public, Protected, Internal, and Private.</param>
        /// <remarks>To specify a different collection of members to serialize, use one of the overrides of Serialize&lt;T&gt;.</remarks>
        public static void Serialize<T>(Stream stream, T obj, IncludedVisibility visibility)
        {
            Serialize(stream, obj, IncludedMembers.Properties | IncludedMembers.Fields, visibility);
        }

        /// <summary>
        /// Serializes the specified members of the given instance to the given Stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="stream">The stream to serialize obj to.</param>
        /// <param name="obj">The existing instance to be serialized.</param>
        /// <param name="members">The members to serialize, either Fields, Properties, or both.</param>
        /// <param name="visibility">The visibility of fields and properties to serialize, some combination of Public, Protected, Internal, and Private.</param>
        public static void Serialize<T>(Stream stream, T obj, IncludedMembers members, IncludedVisibility visibility)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (members == 0) throw new ArgumentException("members");
            if (visibility == 0) throw new ArgumentException("visibility");

            // Protobuf-net special cases arrays (or List<T>; depending on how you look at it) at a top level
            //   So we need to special case it so arrays and lists end up with the same serialization.
            if (typeof(T).IsArray)
            {
                ArrayThunk<T>.Serialize(stream, obj, members, visibility);
                return;
            }

            TypeDescription desc;
            POCOBuilder builder;
            GetDescriptionAndBuilder<T>(members, visibility, out desc, out builder);

            var envelope = Envelope.Get(desc, builder, obj);

            ProtoBuf.Serializer.Serialize(stream, envelope);
        }

        #region GetDescriptionAndBuilder

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

        #endregion

        /// <summary>
        /// Creates a new instance from a byte array previously generated with Public Broadcasting.
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="bytes">The byte array to deserialize from.</param>
        /// <returns>A new, initialized instance.</returns>
        /// <remarks>T does not need to be the same type used to generate bytes, Public Broadcasting will map
        /// types based on their structure.  In short, members with the same names and compatible types will be mapped to
        /// each other.</remarks>
        public static T Deserialize<T>(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            using (var mem = new MemoryStream(bytes))
            {
                return Deserialize<T>(mem);
            }
        }

        /// <summary>
        /// Creates a new instance from a Stream previously generated with Public Broadcasting.
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="stream">The Stream to deserialize from.</param>
        /// <returns>A new, initialized instance.</returns>
        /// <remarks>T does not need to be the same type used to generate stream, Public Broadcasting will map
        /// types based on their structure.  In short, members with the same names and compatible types will be mapped to
        /// each other.</remarks>
        public static T Deserialize<T>(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            return Deserializer.Deserialize<T>(stream);
        }

        /// <summary>
        /// Creates a dynamically bound object from a byte array previously generated with Public Broadcasting.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize from.</param>
        /// <returns>A new, initialized instance.</returns>
        /// <remarks>Public Broadcasting will not map the type returned to the type used to generate bytes, even if
        /// it is available in theory.  Also be aware that the underlying types of enumerations and the Value/Reference Type distinction
        /// of any serialized types will be lost.
        /// 
        /// In short, dynamically typed returns will have the correct member names and values but nothing else is guaranteed.</remarks>
        public static dynamic Deserialize(byte[] bytes)
        {
            using (var mem = new MemoryStream(bytes))
            {
                return Deserialize(mem);
            }
        }

        /// <summary>
        /// Creates a dynamically bound object from a Stream previously generated with Public Broadcasting.
        /// </summary>
        /// <param name="stream">The Stream to deserialize from.</param>
        /// <returns>A new, initialized instance.</returns>
        /// <remarks>Public Broadcasting will not map the type returned to the type used to generate bytes, even if
        /// it is available in theory.  Also be aware that the underlying types of enumerations and the Value/Reference Type distinction
        /// of any serialized types will be lost.
        /// 
        /// In short, dynamically typed returns will have the correct member names and values but nothing else is guaranteed.</remarks>
        public static dynamic Deserialize(Stream stream)
        {
            return Deserializer.Deserialize(stream);
        }
    }
}
