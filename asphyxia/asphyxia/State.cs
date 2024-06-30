//------------------------------------------------------------
// Onryo あなたたちを許すことはできません
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

namespace asphyxia
{
    /// <summary>
    ///     State
    /// </summary>
    internal enum State
    {
        /// <summary>
        ///     None
        /// </summary>
        None,

        /// <summary>
        ///     Connecting
        /// </summary>
        Connecting,

        /// <summary>
        ///     Connect acknowledging
        /// </summary>
        ConnectAcknowledging,

        /// <summary>
        ///     Connect establishing
        /// </summary>
        ConnectEstablishing,

        /// <summary>
        ///     Connected
        /// </summary>
        Connected,

        /// <summary>
        ///     Disconnecting
        /// </summary>
        Disconnecting,

        /// <summary>
        ///     Disconnected
        /// </summary>
        Disconnected
    }
}