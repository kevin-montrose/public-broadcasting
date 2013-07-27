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
        internal override bool NeedsMapping
        {
            get { return false; }
        }

        [ProtoMember(1)]
        public int Id { get; set; }

        private BackReferenceTypeDescription() { }

        internal BackReferenceTypeDescription(int id)
        {
            Id = id;
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
                if (top is BackReferenceTypeDescription) continue;

                if (top is NullableTypeDescription)
                {
                    stack.Push(((NullableTypeDescription)top).InnerType);
                    continue;
                }

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

                    if (asClass.Id == Id)
                    {
                        asClass.Seal(existingDescription);
                        return asClass.GetPocoType(existingDescription);
                    }

                    foreach (var member in asClass.Members)
                    {
                        stack.Push(member.Value);
                    }

                    continue;
                }

                if (top is EnumTypeDescription)
                {
                    var asEnum = (EnumTypeDescription)top;

                    if (asEnum.Id == Id)
                    {
                        asEnum.Seal(existingDescription);
                        return asEnum.GetPocoType(existingDescription);
                    }

                    continue;
                }

                throw new Exception("Shouldn't be possible");
            }

            throw new Exception("Couldn't find reference to ClassId = " + Id);
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

            var ret = new BackReferenceTypeDescription(Id);

            backRefLookup[this] = ret;

            return ret;
        }

        internal override bool ContainsRawObject(out string path)
        {
            path = null;
            return false;
        }
    }
}
