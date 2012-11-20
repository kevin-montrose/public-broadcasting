using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class FieldsProtectedPrivateDescriber<T>
    {
        private static readonly PromisedTypeDescription FieldsProtectedPrivatePromise;
        private static readonly TypeDescription FieldsProtectedPrivate;

        static FieldsProtectedPrivateDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(FieldsProtectedPrivateDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            FieldsProtectedPrivatePromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var res = Describer.BuildDescription(typeof(FieldsProtectedPrivateDescriber<>).MakeGenericType(typeof(T)));

            FieldsProtectedPrivatePromise.Fulfil(res);

            FieldsProtectedPrivate = res;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Fields;
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
            return FieldsProtectedPrivate ?? FieldsProtectedPrivatePromise;
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
