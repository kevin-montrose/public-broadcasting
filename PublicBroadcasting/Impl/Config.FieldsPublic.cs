using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class FieldsPublicDescriber<T>
    {
        private static readonly PromisedTypeDescription FieldsPublicPromise;
        private static readonly TypeDescription FieldsPublic;

        static FieldsPublicDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(FieldsPublicDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            FieldsPublicPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var res = Describer.BuildDescription(typeof(FieldsPublicDescriber<>).MakeGenericType(typeof(T)));

            FieldsPublicPromise.Fulfil(res);

            FieldsPublic = res;
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
            return FieldsPublic ?? FieldsPublicPromise;
        }

        private static object GetForUseLock = new object();
        private static volatile TypeDescription Flattened;
        private static volatile TypeDescription Sealed;

        public static TypeDescription GetForUse(bool flatten)
        {
            if (Sealed != null && !flatten) return Sealed;

            if (Sealed == null)
            {
                lock (GetForUseLock)
                {
                    if (Sealed != null && !flatten) return Sealed;

                    var ret = Get();
                    Action postPromise;
                    ret = ret.DePromise(out postPromise);
                    postPromise();

                    ret.Seal();

                    Sealed = ret;
                }
            }

            if (!flatten) return Sealed;

            if (Flattened != null) return Flattened;

            lock (GetForUseLock)
            {
                if (Flattened != null) return Flattened;

                var ret = Sealed.Clone(new Dictionary<TypeDescription, TypeDescription>());

                Flattener.Flatten(ret, Config.GetIdProvider());

                if (!ret.NeedsMapping)
                {
                    ret.TypeModel = TypeModelBuilder.BuildTypeModel(ret);
                    ret.TypeModel.CompileInPlace();
                }

                Flattened = ret;

                return Flattened;
            }
        }
    }
}
