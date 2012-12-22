using PublicBroadcasting.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class Deserializer
    {
        private static readonly Dictionary<TypeDescription, Type> TypeCache = new Dictionary<TypeDescription, Type>(new TypeDescriptionComparer());
        private static readonly Dictionary<Tuple<Type, Type>, Func<object, object>> MapperCache = new Dictionary<Tuple<Type, Type>, Func<object, object>>();
        private static readonly Dictionary<Tuple<Type, TypeDescription>, ProtoBuf.Meta.TypeModel> ModelCache = new Dictionary<Tuple<Type, TypeDescription>, ProtoBuf.Meta.TypeModel>(new TypeAndTypeDescriptionComparer());

        internal static T Deserialize<T>(Stream stream)
        {
            Type type;
            var raw = DeserializeCore(stream, typeof(T), out type);

            if (type == typeof(T))
            {
                return (T)raw;
            }

            var mapper = GetMapper(type, typeof(T));

            return (T)mapper(raw);
        }

        internal static dynamic Deserialize(Stream stream)
        {
            Type ignored;
            return DeserializeCore(stream, null, out ignored);
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

        private static object DeserializeCore(Stream stream, Type tryMakeType, out Type type)
        {
            var untyped = ProtoBuf.Serializer.Deserialize<Envelope>(stream);

            var effectDesc = untyped.Description;

            if (tryMakeType != null && IsExactMatch(tryMakeType, effectDesc, new Dictionary<int,Type>()))
            {
                ProtoBuf.Meta.TypeModel model;
                lock (ModelCache)
                {
                    var key = Tuple.Create(tryMakeType, effectDesc);

                    if (!ModelCache.TryGetValue(key, out model))
                    {
                        var runtime = ProtoBuf.Meta.RuntimeTypeModel.Create();

                        MakeModel(tryMakeType, effectDesc, runtime);

                        runtime.CompileInPlace();

                        model = runtime;

                        ModelCache[key] = model;
                    }
                }

                using(var mem = new MemoryStream(untyped.Payload))
                {
                    type = tryMakeType;
                    return model.Deserialize(mem, null, tryMakeType);
                }
            }

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

        private static bool IsExactMatch(Type type, TypeDescription description, Dictionary<int, Type> backLookup)
        {
            var simple = description as SimpleTypeDescription;
            if (simple != null)
            {
                switch (simple.Tag)
                {
                    case SimpleTypeDescription.BoolTag: return type == typeof(bool);
                    case SimpleTypeDescription.ByteTag: return type == typeof(byte);
                    case SimpleTypeDescription.CharTag: return type == typeof(char);
                    case SimpleTypeDescription.DateTimeTag: return type == typeof(DateTime);
                    case SimpleTypeDescription.DecimalTag: return type == typeof(decimal);
                    case SimpleTypeDescription.DoubleTag: return type == typeof(double);
                    case SimpleTypeDescription.FloatTag: return type == typeof(float);
                    case SimpleTypeDescription.GuidTag: return type == typeof(Guid);
                    case SimpleTypeDescription.IntTag: return type == typeof(int);
                    case SimpleTypeDescription.LongTag: return type == typeof(long);
                    case SimpleTypeDescription.SByteTag: return type == typeof(sbyte);
                    case SimpleTypeDescription.ShortTag: return type == typeof(short);
                    case SimpleTypeDescription.StringTag: return type == typeof(string);
                    case SimpleTypeDescription.TimeSpanTag: return type == typeof(TimeSpan);
                    case SimpleTypeDescription.UIntTag: return type == typeof(uint);
                    case SimpleTypeDescription.ULongTag: return type == typeof(ulong);
                    case SimpleTypeDescription.UriTag: return type == typeof(Uri);
                    case SimpleTypeDescription.UShortTag: return type == typeof(ushort);
                    default: throw new Exception("Unexpected tag: " + simple.Tag);
                }
            }

            var nullable = description as NullableTypeDescription;
            if (nullable != null)
            {
                var underlying = Nullable.GetUnderlyingType(type);
                if (underlying == null) return false;

                return IsExactMatch(underlying, nullable.InnerType, backLookup);
            }

            var list = description as ListTypeDescription;
            if (list != null)
            {
                if (!type.IsList()) return false;
                
                // Handling of arrays is funky, just bail for arrays
                if (type.IsArray) return false;

                var listI = type.GetListInterface();

                return IsExactMatch(listI.GetGenericArguments()[0], list.Contains, backLookup);
            }

            var dict = description as DictionaryTypeDescription;
            if (dict != null)
            {
                if (!type.IsDictionary()) return false;

                var dictI = type.GetDictionaryInterface();

                return
                    IsExactMatch(dictI.GetGenericArguments()[0], dict.KeyType, backLookup) &&
                    IsExactMatch(dictI.GetGenericArguments()[1], dict.ValueType, backLookup);
            }

            var enumD = description as EnumTypeDescription;
            if (enumD != null)
            {
                if (!type.IsEnum) return false;
                if (Enum.GetUnderlyingType(type) != typeof(int)) return false;

                var names = Enum.GetNames(type);

                if (!enumD.Values.All(a => names.Contains(a))) return false;

                if (enumD.Id != 0)
                {
                    backLookup[enumD.Id] = type;
                }

                return true;
            }

            var back = description as BackReferenceTypeDescription;
            if (back != null)
            {
                return backLookup[back.Id] == type;
            }

            var classD = description as ClassTypeDescription;
            if (classD != null)
            {
                // Assume they're equal for back ref purposes; a contradiction here is going to be blow up anyway
                if(classD.Id != 0)
                {
                    backLookup[classD.Id] = type;
                }

                foreach (var mem in classD.Members)
                {
                    var classMem = type.GetMember(mem.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SingleOrDefault();

                    if (classMem == null) continue;

                    if (classMem is FieldInfo)
                    {
                        if (!IsExactMatch(((FieldInfo)classMem).FieldType, mem.Value, backLookup)) return false;
                    }

                    if (classMem is PropertyInfo)
                    {
                        var asProp = (PropertyInfo)classMem;
                        // protobuf-net wants uniform access; it would be nice if we could get away with just a setter though
                        if (!asProp.CanWrite || !asProp.CanRead) return false;

                        if (!IsExactMatch(asProp.PropertyType, mem.Value, backLookup)) return false;
                    }
                }

                return true;
            }

            throw new Exception("Unexpected description [" + description + "]");
        }

        private static void MakeModel(Type type, TypeDescription description, ProtoBuf.Meta.RuntimeTypeModel model)
        {
            if (description is SimpleTypeDescription) return;
            if(description is BackReferenceTypeDescription) return;

            var nullable = description as NullableTypeDescription;
            if (nullable != null)
            {
                var underlying = Nullable.GetUnderlyingType(type);
                MakeModel(underlying, nullable.InnerType, model);

                return;
            }

            var list = description as ListTypeDescription;
            if (list != null)
            {
                var listI = type.GetListInterface();
                MakeModel(listI.GetGenericArguments()[0], list.Contains, model);

                return;
            }

            var dict = description as DictionaryTypeDescription;
            if (dict != null)
            {
                var dictI = type.GetDictionaryInterface();
                MakeModel(dictI.GetGenericArguments()[0], dict.KeyType, model);
                MakeModel(dictI.GetGenericArguments()[1], dict.ValueType, model);

                return;
            }

            var enumD = description as EnumTypeDescription;
            if (enumD != null)
            {
                var eT = model.Add(type, applyDefaultBehaviour: false);

                for (var i = 0; i < enumD.Values.Count; i++)
                {
                    eT.AddField(i, enumD.Values[i]);
                }

                return;
            }

            var classD = description as ClassTypeDescription;
            if (classD != null)
            {
                var cT = model.Add(type, applyDefaultBehaviour: false);

                var memNames = classD.Members.Keys.OrderBy(o => o, StringComparer.Ordinal).ToList();

                for (var i = 0; i < memNames.Count; i++)
                {
                    var mem = type.GetMember(memNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault();

                    if(mem == null) continue;

                    cT.AddField(i + 1, memNames[i]);

                    var memType = 
                        mem is PropertyInfo ?
                            ((PropertyInfo)mem).PropertyType :
                            ((FieldInfo)mem).FieldType;

                    MakeModel(
                        memType,
                        classD.Members[memNames[i]],
                        model
                    );
                }

                return;
            }

            throw new Exception("Unexpected description [" + description + "]");
        }
    }
}