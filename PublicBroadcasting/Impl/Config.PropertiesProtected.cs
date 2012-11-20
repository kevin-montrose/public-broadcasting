using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class PropertiesProtectedDescriber<T>
    {
        private static readonly PromisedTypeDescription PropertiesProtectedPromise;
        private static readonly TypeDescription PropertiesProtected;

        static PropertiesProtectedDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(PropertiesProtectedDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            PropertiesProtectedPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var allFields = Describer.BuildDescription(typeof(PropertiesProtectedDescriber<>).MakeGenericType(typeof(T)));

            PropertiesProtectedPromise.Fulfil(allFields);

            PropertiesProtected = allFields;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Properties;
        }

        public static IncludedVisibility GetVisibilityMask()
        {
            return IncludedVisibility.Protected;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return PropertiesProtected ?? PropertiesProtectedPromise;
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
