using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class PropertiesPublicDescriber<T>
    {
        private static readonly PromisedTypeDescription PropertiesPublicPromise;
        private static readonly TypeDescription PropertiesPublic;

        static PropertiesPublicDescriber()
        {
            Debug.WriteLine("FieldsPublicDescriber: " + typeof(T).FullName);

            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(PropertiesPublicDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            PropertiesPublicPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var allFields = Describer.BuildDescription(typeof(PropertiesPublicDescriber<>).MakeGenericType(typeof(T)));

            PropertiesPublicPromise.Fulfil(allFields);

            PropertiesPublic = allFields;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Properties;
        }

        public static IncludedVisibility GetVisibilityMask()
        {
            return IncludedVisibility.Public;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return PropertiesPublic ?? PropertiesPublicPromise;
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
