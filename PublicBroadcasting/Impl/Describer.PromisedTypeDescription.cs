using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class PromisedTypeDescription : TypeDescription
    {
        private TypeDescription Fulfilment { get; set; }

        private Type ForType { get; set; }

        internal PromisedTypeDescription(Type forType)
        {
            ForType = forType;
        }

        public void Fulfil(TypeDescription desc)
        {
            Fulfilment = desc;
        }

        internal override Type GetPocoType(TypeDescription existingDescription = null)
        {
            return Fulfilment.GetPocoType(existingDescription);
        }

        internal override TypeDescription DePromise(out Action afterPromise)
        {
            afterPromise =
                delegate
                {
                    Action act;
                    Fulfilment.DePromise(out act);

                    act();
                };

            return Fulfilment;
        }

        internal override TypeDescription Clone(Dictionary<TypeDescription, TypeDescription> ignored)
        {
            throw new NotImplementedException();
        }
    }

    internal class PromisedTypeDescription<ForType>
    {
        public static readonly PromisedTypeDescription Singleton;

        static PromisedTypeDescription()
        {
            Singleton = new PromisedTypeDescription(typeof(ForType));
        }
    }
}
