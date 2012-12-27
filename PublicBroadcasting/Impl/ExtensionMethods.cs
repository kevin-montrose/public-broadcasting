using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal static class ExtensionMethods
    {
        internal static bool IsOverride(this MethodInfo method)
        {
            return method.GetBaseDefinition() != method;
        }

        /// <summary>
        /// HACK: This is a best effort attempt to divine if a type is anonymous based on the language spec.
        /// 
        /// Reference section 7.6.10.6 of the C# language spec as of 2012/11/19
        /// 
        /// It checks:
        ///     - is a class
        ///     - descends directly from object
        ///     - has [CompilerGenerated]
        ///     - has a single constructor
        ///     - that constructor takes exactly the same parameters as its public properties
        ///     - all public properties are not writable
        ///     - has a private field for every public property
        ///     - overrides Equals(object)
        ///     - overrides GetHashCode()
        /// </summary>
        internal static bool IsAnonymouseClass(this Type type) // don't fix the typo, it's fitting.
        {
            if (type.IsValueType) return false;
            if (type.BaseType != typeof(object)) return false;
            if (!Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))) return false;

            var allCons = type.GetConstructors();
            if (allCons.Length != 1) return false;

            var cons = allCons[0];
            if (!cons.IsPublic) return false;

            var props = type.GetProperties();
            if (props.Any(p => p.CanWrite)) return false;

            var propTypes = props.Select(t => t.PropertyType).ToList();

            foreach (var param in cons.GetParameters())
            {
                if (!propTypes.Contains(param.ParameterType)) return false;

                propTypes.Remove(param.ParameterType);
            }

            if (propTypes.Count != 0) return false;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            if (fields.Any(f => !f.IsPrivate)) return false;

            propTypes = props.Select(t => t.PropertyType).ToList();
            foreach (var field in fields)
            {
                if (!propTypes.Contains(field.FieldType)) return false;

                propTypes.Remove(field.FieldType);
            }

            if (propTypes.Count != 0) return false;

            var equals = type.GetMethod("Equals", new Type[] { typeof(object) });
            var hashCode = type.GetMethod("GetHashCode", new Type[0]);

            if (!equals.IsOverride() || !hashCode.IsOverride()) return false;

            return true;
        }

        internal static object ToArray(this IList l, Type elementType)
        {
            var arr = Array.CreateInstance(elementType, l.Count);

            l.CopyTo(arr, 0);

            return arr;
        }

        internal static bool IsList(this Type t)
        {
            try
            {
                return
                    (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
                    t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
            }
            catch (Exception) { return false; }
        }

        internal static Type GetListInterface(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ?
                t :
                t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        }

        internal static bool IsDictionary(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        internal static Type GetDictionaryInterface(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ?
                t :
                t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        internal static bool IsSimple(this Type t)
        {
            return
                t == typeof(bool) ||
                t == typeof(char) ||
                t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal) ||
                t == typeof(Guid) ||
                t == typeof(Uri) ||
                t == typeof(string) ||
                t == typeof(TimeSpan) ||
                t == typeof(DateTime);
        }

        public static bool IsMappableToDictionary(this Type from, Type to)
        {
            var dictI = to.GetDictionaryInterface();

            var valType = dictI.GetGenericArguments()[1];

            if (dictI.GetGenericArguments()[0] != typeof(string)) return false;

            if (valType == typeof(object)) return true;

            var fields = from.GetFields();

            var fieldTypes = fields.Select(f => f.FieldType).ToList();

            return
                fieldTypes.All(
                    f =>
                    {
                        Func<object, object> ignored;

                        return 
                            valType == f ||
                            POCOMapper<object, object>.Widens(f, valType, out ignored);
                    }
                );
        }
    }
}
