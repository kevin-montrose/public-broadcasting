using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class FieldsPublicDescriber<T>
    {
        private static readonly PromisedTypeDescription AllFieldsPromise;
        private static readonly TypeDescription AllFields;

        static FieldsPublicDescriber()
        {
            Debug.WriteLine("FieldsPublicDescriber: " + typeof(T).FullName);

            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(FieldsPublicDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            AllFieldsPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var allFields = Describer.BuildDescription(typeof(FieldsPublicDescriber<>).MakeGenericType(typeof(T)));

            AllFieldsPromise.Fulfil(allFields);

            AllFields = allFields;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Fields;
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
            return AllFields ?? AllFieldsPromise;
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
