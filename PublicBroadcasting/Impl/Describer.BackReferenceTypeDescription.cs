using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    [ProtoContract]
    internal class BackReferenceTypeDescription : TypeDescription
    {
        [ProtoMember(1)]
        public int ClassId { get; set; }

        private BackReferenceTypeDescription() { }

        internal BackReferenceTypeDescription(int id)
        {
            ClassId = id;
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            if (existingDescription == null) throw new ArgumentNullException("existingDescription");

            var stack = new Stack<TypeDescription>();
            stack.Push(existingDescription);

            while (stack.Count > 0)
            {
                var top = stack.Pop();

                if (top is SimpleTypeDescription) continue;

                if (top is ListTypeDescription)
                {
                    stack.Push(((ListTypeDescription)top).Contains);
                    continue;
                }

                if (top is DictionaryTypeDescription)
                {
                    var asDict = (DictionaryTypeDescription)top;

                    stack.Push(asDict.KeyType);
                    stack.Push(asDict.ValueType);

                    continue;
                }

                if (top is ClassTypeDescription)
                {
                    var asClass = (ClassTypeDescription)top;

                    if (asClass.Id == ClassId) return asClass.GetPocoType(existingDescription);

                    foreach (var member in asClass.Members)
                    {
                        stack.Push(member.Value);
                    }

                    continue;
                }

                if (top is NoTypeDescription)
                {
                    throw new Exception("How did this even happen?");
                }
            }

            throw new Exception("Couldn't find reference to ClassId = " + ClassId);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise = () => { };

            return this;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> backRefLookup)
        {
            if (backRefLookup.ContainsKey(this))
            {
                return backRefLookup[this];
            }

            var ret = new BackReferenceTypeDescription(ClassId);

            backRefLookup[this] = ret;

            return ret;
        }
    }
}
