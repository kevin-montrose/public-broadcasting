﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class PropertiesInternalDescriber<T>
    {
        private static readonly PromisedTypeDescription PropertiesInternalPromise;
        private static readonly TypeDescription PropertiesInternal;

        static PropertiesInternalDescriber()
        {
            var promiseType = typeof(PromisedTypeDescription<,>).MakeGenericType(typeof(T), typeof(PropertiesInternalDescriber<>).MakeGenericType(typeof(T)));
            var promiseSingle = promiseType.GetField("Singleton");

            PropertiesInternalPromise = (PromisedTypeDescription)promiseSingle.GetValue(null);

            var res = Describer.BuildDescription(typeof(PropertiesInternalDescriber<>).MakeGenericType(typeof(T)));

            PropertiesInternalPromise.Fulfil(res);

            PropertiesInternal = res;
        }

        public static IncludedMembers GetMemberMask()
        {
            return IncludedMembers.Properties;
        }

        public static IncludedVisibility GetVisibilityMask()
        {
            return IncludedVisibility.Internal;
        }

        public static TypeDescription Get()
        {
            // How does this happen you're thinking?
            //   What happens if you call Get() from the static initializer?
            //   That's how.
            return PropertiesInternal ?? PropertiesInternalPromise;
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

                Flattener.Flatten(ret, Describer.GetIdProvider());

                Flattened = ret;

                return Flattened;
            }
        }
    }
}
