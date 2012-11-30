using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting
{
    [Flags]
    public enum IncludedMembers : byte
    {
        Properties = 1,
        Fields = 2
    }

    [Flags]
    public enum IncludedVisibility : byte
    {
        Public = 1,
        Protected = 2,
        Internal = 4,
        Private = 8
    }
}
