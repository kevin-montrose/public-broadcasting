using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class AllProtectedInternalDescriber<T>
    {
        private static readonly PromisedTypeDescription AllProtectedInternalPromise;
        private static readonly TypeDescription AllProtectedInternal;

        static AllProtectedInternalDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(AllProtectedInternalDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            AllProtectedInternalPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var res = Describer.BuildDescription(typeof(AllProtectedInternalDescriber<>).MakeGenericType(typeof(T)));

            AllProtectedInternalPromise.Fulfil(res);

            AllProtectedInternal = res;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Fields | IncludedMembers.Properties;
        }

        public static IncludedVisibility GetVisibilityMask()
        {
            return IncludedVisibility.Protected | IncludedVisibility.Internal;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return AllProtectedInternal ?? AllProtectedInternalPromise;
        }

        public static TypeDescription GetForUse(bool flatten)
        {
            var ret = Get();

            Action postPromise;
            ret = ret.DePromise(out postPromise);
            postPromise();

            ret.Seal();

            if (flatten)
            {
                ret = ret.Clone(new Dictionary<TypeDescription, TypeDescription>());

                Flattener.Flatten(ret, Describer.GetIdProvider());
            }

            return ret;
        }
    }
}
