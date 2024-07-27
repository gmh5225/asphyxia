//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif
using static System.Runtime.InteropServices.Marshal;
using static System.Runtime.CompilerServices.Unsafe;

namespace asphyxia
{
    /// <summary>
    ///     DataPacket
    /// </summary>
    public unsafe struct DataPacket : IDisposable
    {
        /// <summary>
        ///     Data
        /// </summary>
        public nint Data;

        /// <summary>
        ///     Length
        /// </summary>
        public int Length;

        /// <summary>
        ///     Flag
        /// </summary>
        public PacketFlag Flag;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        private DataPacket(nint data, int length, PacketFlag flag)
        {
            Data = data;
            Length = length;
            Flag = flag;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsSet => Data != IntPtr.Zero;

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(int length, PacketFlag flag)
        {
            var data = AllocHGlobal(length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(DataPacket src, PacketFlag flag)
        {
            var length = src.Length;
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, (byte*)src.Data, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(DataPacket src, int length, PacketFlag flag)
        {
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, (byte*)src.Data, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(DataPacket src, int offset, int length, PacketFlag flag)
        {
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, (byte*)src.Data + offset, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(byte* src, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)src, length, flag);
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, src, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(byte* src, int offset, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)src, length, flag);
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, src + offset, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(nint src, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket(src, length, flag);
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, (byte*)src, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(nint src, int offset, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket(src, length, flag);
            var data = AllocHGlobal(length);
            CopyBlock((byte*)data, (byte*)src + offset, (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(byte[] src, PacketFlag flag)
        {
            var length = src.Length;
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)AsPointer(ref src[0]), length, flag);
            var data = AllocHGlobal(length);
            CopyBlock(ref *(byte*)data, ref src[0], (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(byte[] src, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)AsPointer(ref src[0]), length, flag);
            var data = AllocHGlobal(length);
            CopyBlock(ref *(byte*)data, ref src[0], (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(byte[] src, int offset, int length, PacketFlag flag)
        {
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)AsPointer(ref src[offset]), length, flag);
            var data = AllocHGlobal(length);
            CopyBlock(ref *(byte*)data, ref src[offset], (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="flag">Flag</param>
        /// <returns>DataPacket</returns>
        public static DataPacket Create(Span<byte> src, PacketFlag flag)
        {
            var length = src.Length;
            if (((int)flag & (int)PacketFlag.NoAllocate) != 0)
                return new DataPacket((nint)AsPointer(ref src[0]), length, flag);
            var data = AllocHGlobal(length);
            CopyBlock(ref *(byte*)data, ref src[0], (uint)length);
            return new DataPacket(data, length, flag);
        }

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        public void CopyTo(DataPacket dst) => CopyBlock((byte*)dst.Data, (byte*)Data, (uint)Length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="length">Length</param>
        public void CopyTo(DataPacket dst, int length) => CopyBlock((byte*)dst.Data, (byte*)Data, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        public void CopyTo(DataPacket dst, int offset, int length) => CopyBlock((byte*)dst.Data, (byte*)Data + offset, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        public void CopyTo(byte* dst) => CopyBlock(dst, (byte*)Data, (uint)Length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="length">Length</param>
        public void CopyTo(byte* dst, int length) => CopyBlock(dst, (byte*)Data, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        public void CopyTo(byte* dst, int offset, int length) => CopyBlock(dst, (byte*)Data + offset, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        public void CopyTo(nint dst) => CopyBlock((byte*)dst, (byte*)Data, (uint)Length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="length">Length</param>
        public void CopyTo(nint dst, int length) => CopyBlock((byte*)dst, (byte*)Data, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        public void CopyTo(nint dst, int offset, int length) => CopyBlock((byte*)dst, (byte*)Data + offset, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        public void CopyTo(byte[] dst) => CopyBlock(ref dst[0], ref *(byte*)Data, (uint)Length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="length">Length</param>
        public void CopyTo(byte[] dst, int length) => CopyBlock(ref dst[0], ref *(byte*)Data, (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        public void CopyTo(byte[] dst, int offset, int length) => CopyBlock(ref dst[0], ref *((byte*)Data + offset), (uint)length);

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="dst">Destination</param>
        public void CopyTo(Span<byte> dst) => CopyBlock(ref dst[0], ref *(byte*)Data, (uint)Length);

        /// <summary>
        ///     AsSpan
        /// </summary>
        /// <returns>Span</returns>
        public Span<byte> AsSpan() => new((byte*)Data, Length);

        /// <summary>
        ///     AsSpan
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <returns>Span</returns>
        public Span<byte> AsSpan(int offset) => new((byte*)Data + offset, Length);

        /// <summary>
        ///     AsSpan
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <returns>Span</returns>
        public Span<byte> AsSpan(int offset, int length) => new((byte*)Data + offset, length);

        /// <summary>
        ///     AsReadOnlySpan
        /// </summary>
        /// <returns>ReadOnlySpan</returns>
        public ReadOnlySpan<byte> AsReadOnlySpan() => new((byte*)Data, Length);

        /// <summary>
        ///     AsReadOnlySpan
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <returns>ReadOnlySpan</returns>
        public ReadOnlySpan<byte> AsReadOnlySpan(int offset) => new((byte*)Data + offset, Length);

        /// <summary>
        ///     AsReadOnlySpan
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <returns>ReadOnlySpan</returns>
        public ReadOnlySpan<byte> AsReadOnlySpan(int offset, int length) => new((byte*)Data + offset, length);

        /// <summary>
        ///     Dispose
        /// </summary>
        public void Dispose()
        {
            if (((int)Flag & (int)PacketFlag.NoAllocate) != 0 || Data == IntPtr.Zero)
                return;
            FreeHGlobal(Data);
            Data = IntPtr.Zero;
        }
    }
}