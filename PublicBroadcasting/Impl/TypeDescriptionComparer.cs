using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PublicBroadcasting.Impl
{
    /// <summary>
    /// Note that this only works on TypeDescriptions that *don't have cycles*!
    /// 
    /// In practice this means it can't be used while serializing, but after a type has been flattened
    /// then everything's OK.
    /// </summary>
    class TypeDescriptionComparer : IEqualityComparer<TypeDescription>
    {
        private bool ClassEquality(ClassTypeDescription x, ClassTypeDescription y)
        {
            if (x.Members.Count != y.Members.Count) return false;

            foreach (var kv in x.Members)
            {
                TypeDescription otherValue;
                if (!y.Members.TryGetValue(kv.Key, out otherValue)) return false;

                if (!Equals(kv.Value, otherValue)) return false;
            }

            return true;
        }

        public bool Equals(TypeDescription x, TypeDescription y)
        {
            if (object.ReferenceEquals(x, y)) return true;
            if (x == null && y != null) return false;
            if (y == null & x != null) return false;

            if (x is ClassTypeDescription)
            {
                if (!(y is ClassTypeDescription)) return false;

                return ClassEquality((ClassTypeDescription)x, (ClassTypeDescription)y);
            }

            if (x is SimpleTypeDescription)
            {
                if (!(y is SimpleTypeDescription)) return false;

                return ((SimpleTypeDescription)x).Tag == ((SimpleTypeDescription)y).Tag;
            }

            if (x is ListTypeDescription)
            {
                if (!(y is ListTypeDescription)) return false;

                return Equals(((ListTypeDescription)x).Contains, ((ListTypeDescription)y).Contains);
            }

            if (x is DictionaryTypeDescription)
            {
                if (!(y is DictionaryTypeDescription)) return false;

                return
                    Equals(((DictionaryTypeDescription)x).KeyType, ((DictionaryTypeDescription)y).KeyType) &&
                    Equals(((DictionaryTypeDescription)x).ValueType, ((DictionaryTypeDescription)y).ValueType);
            }

            if (x is NullableTypeDescription)
            {
                if (!(y is NullableTypeDescription)) return false;

                return Equals(((NullableTypeDescription)x).InnerType, ((NullableTypeDescription)y).InnerType);
            }

            if (x is BackReferenceTypeDescription)
            {
                if (!(y is BackReferenceTypeDescription)) return false;

                return ((BackReferenceTypeDescription)x).Id == ((BackReferenceTypeDescription)y).Id;
            }

            if (x is EnumTypeDescription)
            {
                if (!(y is EnumTypeDescription)) return false;

                return
                    ((EnumTypeDescription)x).Values.SequenceEqual(
                        ((EnumTypeDescription)y).Values
                    );
            }

            if (x is PromisedTypeDescription || y is PromisedTypeDescription) throw new Exception("Promises don't have equality");
            if (x is NoTypeDescription || y is NoTypeDescription) throw new Exception("NoTypes don't have equality");

            throw new Exception("Shouldn't be possible");
        }

        public int GetHashCode(TypeDescription x)
        {
            var asClass = x as ClassTypeDescription;
            if (asClass != null)
            {
                var ret = 0;

                int ix = 105;
                foreach (var kv in asClass.Members.OrderBy(o => o.Key))
                {
                    ret ^= kv.Key.GetHashCode();
                    ret ^= (GetHashCode(kv.Value) + ix) * -1;

                    ix++;
                }

                return ret;
            }

            var asSimple = x as SimpleTypeDescription;
            if (asSimple != null)
            {
                return asSimple.Tag;
            }

            var asList = x as ListTypeDescription;
            if (asList != null)
            {
                return (GetHashCode(asList.Contains) + 100) * -1;
            }

            var asDict = x as DictionaryTypeDescription;
            if (asDict != null)
            {
                return
                    (
                        (GetHashCode(asDict.KeyType) + 101) ^
                        (GetHashCode(asDict.ValueType) + 102)
                    ) * -1;
            }

            var asNullable = x as NullableTypeDescription;
            if (asNullable != null)
            {
                return
                    (GetHashCode(asNullable.InnerType) + 103) * -1;
            }

            var asBack = x as BackReferenceTypeDescription;
            if (asBack != null)
            {
                return
                    (asBack.Id + 104) * -1;
            }

            var asEnum = x as EnumTypeDescription;
            if (asEnum != null)
            {
                var ret = 0;
                foreach (var val in asEnum.Values)
                {
                    ret ^= val.GetHashCode();
                }

                return ret;
            }

            if (x is PromisedTypeDescription) throw new Exception("Promises don't have equality");
            if (x is NoTypeDescription) throw new Exception("NoTypes don't have equality");

            throw new Exception("Shouldn't be possible");
        }
    }
}
