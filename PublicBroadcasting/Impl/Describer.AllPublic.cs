using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class AllPublicDescriber<T>
    {
        private static readonly PromisedTypeDescription AllPublicPromise;
        private static readonly TypeDescription AllPublic;

        static AllPublicDescriber()
        {
            Debug.WriteLine("Describer: " + typeof(T).FullName);

            AllPublicPromise = PromisedTypeDescription<T>.Singleton;

            var allPublic = Describer.BuildDescription(typeof(AllPublicDescriber<>).MakeGenericType(typeof(T)));

            AllPublicPromise.Fulfil(allPublic);

            AllPublic = allPublic;
        }

        private static Func<int> GetIdProvider()
        {
            int startId = 0;

            return
                () =>
                {
                    return Interlocked.Increment(ref startId);
                };
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return AllPublic ?? AllPublicPromise;
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

                Flattener.Flatten(ret, GetIdProvider());
            }

            return ret;
        }
    }
}
