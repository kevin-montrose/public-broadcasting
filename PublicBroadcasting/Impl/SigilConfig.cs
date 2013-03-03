using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal static class SigilConfig
    {
#if DEBUG
        public static readonly ValidationOptions ValidationOptions = Sigil.ValidationOptions.All;
#else
        public static readonly ValidationOptions ValidationOptions = Sigil.ValidationOptions.None;
#endif
    }
}
