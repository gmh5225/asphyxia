//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

namespace asphyxia
{
    /// <summary>
    ///     Packet flag
    /// </summary>
    [Flags]
    public enum PacketFlag
    {
        None = 0,
        NoAllocate = 1,
        Unreliable = 2,
        Sequenced = 4,
        Reliable = 8
    }
}