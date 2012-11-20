using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class AllProtectedPrivateDescriber<T>
    {
        private static readonly PromisedTypeDescription AllProtectedPrivatePromise;
        private static readonly TypeDescription AllProtectedPrivate;

        static AllProtectedPrivateDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(AllProtectedPrivateDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            AllProtectedPrivatePromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var res = Describer.BuildDescription(typeof(AllProtectedPrivateDescriber<>).MakeGenericType(typeof(T)));

            AllProtectedPrivatePromise.Fulfil(res);

            AllProtectedPrivate = res;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Fields | IncludedMembers.Properties;
        }

        public static IncludedVisibility GetVisibilityMask()
        {
            return IncludedVisibility.Protected | IncludedVisibility.Private;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return AllProtectedPrivate ?? AllProtectedPrivatePromise;
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
