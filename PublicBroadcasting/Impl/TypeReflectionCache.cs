using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class CutdownType
    {
        public List<PropertyInfo> Properties { get; set; }
        public List<FieldInfo> Fields { get; set; }
    }

    internal class TypeReflectionCache<T>
    {
        private static readonly TypeReflectionCache<T> Singleton;

        static TypeReflectionCache()
        {
            var t = typeof(T);

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(p => p.CanRead).ToList();
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();

            Singleton = new TypeReflectionCache<T>(
                props,
                fields
            );
        }

        private List<PropertyInfo> PublicProperties { get; set; }
        private List<PropertyInfo> PrivateProperties { get; set; }
        private List<PropertyInfo> InternalProperties { get; set; }
        private List<PropertyInfo> ProtectedProperties { get; set; }

        private List<FieldInfo> PublicFields { get; set; }
        private List<FieldInfo> PrivateFields { get; set; }
        private List<FieldInfo> InternalFields { get; set; }
        private List<FieldInfo> ProtectedFields { get; set; }

        private TypeReflectionCache(IEnumerable<PropertyInfo> props, IEnumerable<FieldInfo> fields)
        {
            PublicProperties = props.Where(p => p.GetMethod.IsPublic).ToList();
            PrivateProperties = props.Where(p => p.GetMethod.IsPrivate).ToList();
            InternalProperties = props.Where(p => p.GetMethod.IsAssembly).ToList();
            ProtectedProperties = props.Where(p => p.GetMethod.IsFamily).ToList();

            PublicFields = fields.Where(p => p.IsPublic).ToList();
            PrivateFields = fields.Where(p => p.IsPrivate).ToList();
            InternalFields = fields.Where(p => p.IsAssembly).ToList();
            ProtectedFields = fields.Where(p => p.IsFamily).ToList();
        }

        public static CutdownType Get(IncludedMembers members, IncludedVisibility visibility)
        {
            var props = new List<PropertyInfo>();
            var fields = new List<FieldInfo>();

            if (members.HasFlag(IncludedMembers.Properties))
            {
                if (visibility.HasFlag(IncludedVisibility.Public))
                {
                    props.AddRange(Singleton.PublicProperties);
                }

                if (visibility.HasFlag(IncludedVisibility.Private))
                {
                    props.AddRange(Singleton.PrivateProperties);
                }

                if (visibility.HasFlag(IncludedVisibility.Protected))
                {
                    props.AddRange(Singleton.ProtectedProperties);
                }

                if (visibility.HasFlag(IncludedVisibility.Internal))
                {
                    props.AddRange(Singleton.InternalProperties);
                }
            }

            if (members.HasFlag(IncludedMembers.Fields))
            {
                if (visibility.HasFlag(IncludedVisibility.Public))
                {
                    fields.AddRange(Singleton.PublicFields);
                }

                if (visibility.HasFlag(IncludedVisibility.Private))
                {
                    fields.AddRange(Singleton.PrivateFields);
                }

                if (visibility.HasFlag(IncludedVisibility.Protected))
                {
                    fields.AddRange(Singleton.ProtectedFields);
                }

                if (visibility.HasFlag(IncludedVisibility.Internal))
                {
                    fields.AddRange(Singleton.InternalFields);
                }
            }

            return new CutdownType { Fields = fields.Distinct().ToList(), Properties = props.Distinct().ToList() };
        }
    }
}
