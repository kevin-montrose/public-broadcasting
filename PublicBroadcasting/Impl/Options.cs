using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicBroadcasting
{
    /// <summary>
    /// Which members to include while serializing.
    /// 
    /// Always used in conjunction with IncludedVisibility when discovering members.
    /// </summary>
    [Flags]
    public enum IncludedMembers : byte
    {
        /// <summary>
        /// Includes any properties with getters.
        /// </summary>
        Properties = 1,
        /// <summary>
        /// Includes all fields
        /// </summary>
        Fields = 2
    }

    /// <summary>
    /// Which visibility levels to consider when serializing.
    /// 
    /// Always used in conjunction with IncludedMembers when discovering members.
    /// </summary>
    [Flags]
    public enum IncludedVisibility : byte
    {
        /// <summary>
        /// Includes any public members
        /// </summary>
        Public = 1,
        /// <summary>
        /// Includes any protected members.
        /// 
        /// Note that it is possible for a member to be both protected and internal, Public Broadcasting
        /// will include a member which is both if either Protected or Internal is passed for visiblity.
        /// </summary>
        Protected = 2,
        /// <summary>
        /// Includes any internal members.
        /// 
        /// Note that it is possible for a member to be both protected and internal, Public Broadcasting
        /// will include a member which is both if either Protected or Internal is passed for visiblity.
        /// </summary>
        Internal = 4,
        /// <summary>
        /// Includes any private members.
        /// 
        /// Be aware that normally hidden members (like the fields backing auto-implemented properties) will be
        /// included when Private is passed.
        /// </summary>
        Private = 8
    }
}
