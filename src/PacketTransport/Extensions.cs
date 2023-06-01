using System.Buffers;
using System.Runtime.CompilerServices;

namespace PacketTransport;

public static class SequenceReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryRead(ref this SequenceReader<byte> reader, out sbyte value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryRead(out Unsafe.As<sbyte, byte>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out ushort value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryReadLittleEndian(out Unsafe.As<ushort, short>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out uint value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryReadLittleEndian(out Unsafe.As<uint, int>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out ulong value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryReadLittleEndian(out Unsafe.As<ulong, long>(ref value));
    }
}