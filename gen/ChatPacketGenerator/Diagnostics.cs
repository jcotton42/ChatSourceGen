using Microsoft.CodeAnalysis;

namespace ChatPacketGenerator;

public static class Diagnostics
{
    public static DiagnosticDescriptor PacketsMustBeInsidePacketGroup { get; } = new(
        id: "CPG1001",
        title: "Packet types must be inside a packet group",
        messageFormat: "Packet '{0}' must be within a packet group",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketFieldsMustBeInAPacket { get; } = new(
        id: "CPG1002",
        title: "Packet fields must be in a packet",
        messageFormat: "Packet field '{0}' must be within a packet",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeMayNotBePacketAndPacketGroup { get; } = new(
        id: "CPG1003",
        title: "Type may not be a packet and packet group at the same time",
        messageFormat: "'{0}' is marked as both a packet and packet group",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketGroupsMayNotBeNested { get; } = new(
        id: "CPG1004",
        title: "Packet groups may not be nested",
        messageFormat: "'{0}' and '{1}' are packet groups, and may not be nested within each other",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketGroupsMustBeStaticClasses { get; } = new(
        id: "CPG1005",
        title: "Packet groups must be static classes",
        messageFormat: "'{0}' must be a static class",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
