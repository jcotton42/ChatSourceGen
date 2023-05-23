using Microsoft.CodeAnalysis;

namespace ChatPacketGenerator;

public static class Diagnostics
{
    public static DiagnosticDescriptor NestedGroupTypesNotSupported { get; } = new(
        id: "CPG1001",
        title: "Packet group types must not be nested",
        messageFormat: "{0} is a nested type, which is not allowed",
        category: nameof(ChatPacketGenerator),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketGroupsMustBePartial { get; } = new(
        id: "CPG1002",
        title: "Packet group types must be partial",
        messageFormat: "{0} must be a partial type",
        category: nameof(ChatPacketGenerator),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
