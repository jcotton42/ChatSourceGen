using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ChatPacketGenerator.Generator;

internal static class Emitter
{
    public static string Emit(PacketGroupInfo packetGroup, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var source = new SourceBuilder();
        source.AppendLine(SourceConstants.GeneratedHeader);
        source.AppendLine(SourceConstants.ParserUsings);
        if (packetGroup.Namespace is not null)
        {
            source.AppendLine($"namespace {packetGroup.Namespace}");
            source.StartBlock();
        }

        WriteTypeHierarchy(source, packetGroup.TypeHierarchyInfo);
        WritePacketGroup(source, packetGroup, ct);

        source.EndAllBlocks();
        source.AppendLine(SourceConstants.ParserExtensions);
        return source.ToString();
    }

    private static void WriteTypeHierarchy(SourceBuilder source, TypeHierarchyInfo? hierarchy)
    {
        while (hierarchy is not null)
        {
            source.AppendLine($"{hierarchy.Modifiers} {hierarchy.Keyword} {hierarchy.Name}");
            source.StartBlock();
            hierarchy = hierarchy.Child;
        }
    }

    private static void WritePacketGroup(SourceBuilder source, PacketGroupInfo packetGroup, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        source.AppendLine($"{packetGroup.Modifiers} class {packetGroup.Name}");
        source.StartBlock();

        foreach (var packet in packetGroup.Packets)
        {
            WritePacket(source, packet, ct);
            source.AppendLine();
        }

        source.EndBlock();
    }

    private static void WritePacket(SourceBuilder source, PacketInfo packet, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        WriteTryRead(source, packet, ct);
        source.AppendLine();
        WriteTryWrite(source, packet, ct);
    }

    private static void WriteTryRead(SourceBuilder source, PacketInfo packet, CancellationToken ct)
    {
        if (packet.CreationType is PacketCreationType.EmptyConstructor)
        {
            source.AppendTryReadEmptyPacket(packet.FullyQualifiedName);
            return;
        }

        source.AppendTryReadStart(packet.FullyQualifiedName);

        foreach (var field in packet.Fields!)
        {
            switch (field.Type)
            {
                case PacketFieldType.String:
                    source.AppendTryReadString(field.Name);
                    break;
                case PacketFieldType.Byte:
                    source.AppendTryReadByte(field.Name);
                    break;
                case PacketFieldType.SByte:
                    source.AppendTryReadSByte(field.Name);
                    break;
                case PacketFieldType.OtherInteger:
                    source.AppendTryReadOtherInteger(field.OtherIntegerType!, field.Name);
                    break;
                case PacketFieldType.EnumByte:
                    source.AppendTryReadEnumByte(field.EnumType!, field.Name);
                    break;
                case PacketFieldType.EnumSByte:
                    source.AppendTryReadEnumSByte(field.EnumType!, field.Name);
                    break;
                case PacketFieldType.EnumOtherInteger:
                    source.AppendTryReadEnumOtherInteger(field.EnumType!, field.EnumUnderlyingType!, field.Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            source.AppendLine();
        }

        switch (packet.CreationType)
        {
            case PacketCreationType.Constructor:
                source.AppendTryReadEndConstructor(packet.FullyQualifiedName, packet.Fields.Value.Select(f => f.Name));
                break;
            case PacketCreationType.ObjectInitializer:
                source.AppendTryReadEndObjectInitializer(
                    packet.FullyQualifiedName,
                    packet.Fields.Value.Select(f => f.Name));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void WriteTryWrite(SourceBuilder source, PacketInfo packet, CancellationToken ct)
    {
        source.AppendLine($"// Write for {packet.FullyQualifiedName}");
    }
}

file static class SourceBuilderTryReadExtensions
{
    public static SourceBuilder AppendTryReadEmptyPacket(this SourceBuilder source, string type)
    {
        source.AppendLine(
            $"public static bool TryRead(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out {type}? result)");
        source.StartBlock();
        source.AppendLine($"result = new {type}();");
        source.AppendLine("return true;");
        source.EndBlock();
        return source;
    }

    public static SourceBuilder AppendTryReadStart(this SourceBuilder source, string type)
    {
        source.AppendLine(
            $"public static bool TryRead(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out {type}? result)");
        source.StartBlock();
        source.AppendLine("result = default;");
        source.AppendLine("SequenceReader<byte> reader = new(buffer);");
        source.AppendLine();
        return source;
    }

    public static SourceBuilder AppendTryReadString(this SourceBuilder source, string name)
    {
        source.AppendLine($"if (!reader.TryReadLittleEndian(out ushort __{name}_length)) return false;");
        source.AppendLine($"if (!reader.TryReadExact(__{name}_length, out ReadOnlySequence<byte> __{name}_sequence)) return false;");
        source.AppendLine($"string __{name} = Encoding.UTF8.GetString(__{name}_sequence);");
        return source;
    }

    public static SourceBuilder AppendTryReadByte(this SourceBuilder source, string name)
    {
        source.AppendLine($"if (!reader.TryRead(out byte __{name}) return false;");
        return source;
    }

    public static SourceBuilder AppendTryReadSByte(this SourceBuilder source, string name)
    {
        source.AppendLine($"if (!reader.TryRead(out sbyte __{name}) return false;");
        return source;
    }

    public static SourceBuilder AppendTryReadOtherInteger(this SourceBuilder source, string type, string name)
    {
        source.AppendLine($"if (!reader.TryReadLittleEndian(out {type} __{name})) return false;");
        return source;
    }

    public static SourceBuilder AppendTryReadEnumByte(
        this SourceBuilder source,
        string enumType,
        string name)
    {
        source.AppendLine($"Unsafe.SkipInit(out {enumType} __{name});");
        source.AppendLine($"if (!reader.TryReadLittleEndian(out Unsafe.As<{enumType}, byte>(ref __{name}))) return false");
        return source;
    }

    public static SourceBuilder AppendTryReadEnumSByte(
        this SourceBuilder source,
        string enumType,
        string name)
    {
        source.AppendLine($"Unsafe.SkipInit(out {enumType} __{name});");
        source.AppendLine($"if (!reader.TryReadLittleEndian(out Unsafe.As<{enumType}, sbyte>(ref __{name}))) return false");
        return source;
    }

    public static SourceBuilder AppendTryReadEnumOtherInteger(
        this SourceBuilder source,
        string enumType,
        string enumUnderlyingType,
        string name)
    {
        source.AppendLine($"Unsafe.SkipInit(out {enumType} __{name});");
        source.AppendLine($"if (!reader.TryReadLittleEndian(out Unsafe.As<{enumType}, {enumUnderlyingType}>(ref __{name}))) return false");
        return source;
    }

    public static SourceBuilder AppendTryReadEndConstructor(
        this SourceBuilder source,
        string type,
        IEnumerable<string> props)
    {
        source.AppendLine("buffer = reader.UnreadSequence;");
        source.Append($"result = new {type}(");
        using var enumerator = props.GetEnumerator();
        if (!enumerator.MoveNext()) throw new InvalidOperationException();

        source.Append($"__{enumerator.Current!}");
        while (enumerator.MoveNext())
        {
            source.Append(", ");
            source.Append($"__{enumerator.Current!}");
        }

        source.AppendLine(");");
        source.AppendLine("return true;");
        source.EndBlock();
        return source;
    }

    public static SourceBuilder AppendTryReadEndObjectInitializer(
        this SourceBuilder source,
        string type,
        IEnumerable<string> props)
    {
        source.AppendLine("buffer = reader.UnreadSequence;");
        source.AppendLine($"result = new {type}");
        source.StartBlock();

        foreach (var prop in props)
        {
            source.AppendLine($"{prop} = __{prop},");
        }

        source.EndBlock(addSemicolon: true);
        source.AppendLine("return true;");
        source.EndBlock();
        return source;
    }
}