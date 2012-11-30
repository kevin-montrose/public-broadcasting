using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal static class ExtensionMethods
    {
        internal static object ToArray(this IList l, Type elementType)
        {
            var arr = Array.CreateInstance(elementType, l.Count);

            l.CopyTo(arr, 0);

            return arr;
        }
    }
}
