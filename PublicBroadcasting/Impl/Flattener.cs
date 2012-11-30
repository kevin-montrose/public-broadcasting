using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class Flattener
    {
        public static void Flatten(TypeDescription root, Func<int> nextId)
        {
            var seenClasses = new HashSet<ClassTypeDescription>();
            var finishedClasses = new HashSet<ClassTypeDescription>();
            var seenEnums = new HashSet<EnumTypeDescription>();

            var toCheck = new Stack<TypeDescription>();
            toCheck.Push(root);

            while (toCheck.Count > 0)
            {
                var top = toCheck.Pop();

                if (top is SimpleTypeDescription) continue;
                if (top is BackReferenceTypeDescription) continue;

                var asNullable = top as NullableTypeDescription;
                if (asNullable != null)
                {
                    if (seenClasses.Contains(asNullable.InnerType))
                    {
                        var id = ((ClassTypeDescription)asNullable.InnerType).Id != 0 ? ((ClassTypeDescription)asNullable.InnerType).Id : nextId();
                        ((ClassTypeDescription)asNullable.InnerType).Id = id;
                        asNullable.InnerType = new BackReferenceTypeDescription(id);

                        continue;
                    }

                    if (seenEnums.Contains(asNullable.InnerType))
                    {
                        var id = ((EnumTypeDescription)asNullable.InnerType).Id != 0 ? ((EnumTypeDescription)asNullable.InnerType).Id : nextId();
                        ((EnumTypeDescription)asNullable.InnerType).Id = id;
                        asNullable.InnerType = new BackReferenceTypeDescription(id);

                        continue;
                    }

                    toCheck.Push(asNullable.InnerType);

                    continue;
                }

                var asDict = top as DictionaryTypeDescription;
                if (asDict != null)
                {
                    if (seenClasses.Contains(asDict.KeyType))
                    {
                        var id = ((ClassTypeDescription)asDict.KeyType).Id != 0 ? ((ClassTypeDescription)asDict.KeyType).Id : nextId();
                        ((ClassTypeDescription)asDict.KeyType).Id = id;
                        asDict.KeyType = new BackReferenceTypeDescription(id);
                    }
                    else
                    {
                        toCheck.Push(asDict.KeyType);
                    }

                    if (seenClasses.Contains(asDict.ValueType))
                    {
                        var id = ((ClassTypeDescription)asDict.ValueType).Id != 0 ? ((ClassTypeDescription)asDict.ValueType).Id : nextId();
                        ((ClassTypeDescription)asDict.ValueType).Id = id;
                        asDict.ValueType = new BackReferenceTypeDescription(id);
                    }
                    else
                    {
                        toCheck.Push(asDict.ValueType);
                    }

                    continue;
                }

                var asList = top as ListTypeDescription;
                if (asList != null)
                {
                    if (seenClasses.Contains(asList.Contains))
                    {
                        var id = ((ClassTypeDescription)asList.Contains).Id != 0 ? ((ClassTypeDescription)asList.Contains).Id : nextId();
                        ((ClassTypeDescription)asList.Contains).Id = id;
                        asList.Contains = new BackReferenceTypeDescription(id);
                    }
                    else
                    {
                        toCheck.Push(asList.Contains);
                    }

                    continue;
                }

                var asClass = top as ClassTypeDescription;
                if (asClass != null)
                {
                    if (finishedClasses.Contains(asClass))
                    {
                        continue;
                    }

                    seenClasses.Add(asClass);

                    foreach (var key in asClass.Members.Keys.ToList())
                    {
                        var val = asClass.Members[key];

                        if (seenClasses.Contains(val))
                        {
                            var id = ((ClassTypeDescription)val).Id != 0 ? ((ClassTypeDescription)val).Id : nextId();
                            ((ClassTypeDescription)val).Id = id;
                            asClass.Members[key] = new BackReferenceTypeDescription(id);

                            continue;
                        }

                        if (seenEnums.Contains(val))
                        {
                            var id = ((EnumTypeDescription)val).Id != 0 ? ((EnumTypeDescription)val).Id : nextId();
                            ((EnumTypeDescription)val).Id = id;
                            asClass.Members[key] = new BackReferenceTypeDescription(id);

                            continue;
                        }

                        if(val is ClassTypeDescription)
                        {
                            seenClasses.Add((ClassTypeDescription)val);
                        }

                        if (val is EnumTypeDescription)
                        {
                            seenEnums.Add((EnumTypeDescription)val);
                        }

                        toCheck.Push(val);

                        finishedClasses.Add(asClass);
                    }

                    continue;
                }

                var asEnum = top as EnumTypeDescription;
                if (asEnum != null)
                {
                    if (seenEnums.Contains(asEnum))
                    {
                        continue;
                    }

                    seenEnums.Add(asEnum);

                    continue;
                }

                throw new Exception("Should have been able to get here, found [" + top + "]");
            }
        }
    }
}
