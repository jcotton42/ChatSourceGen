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

    public static DiagnosticDescriptor PacketGroupsMustNotContainNonPacketTypes { get; } = new(
        id: "CPG1006",
        title: "Packet groups must not contain non-packet types",
        messageFormat: "'{0}' is not marked as a packet type",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketHasWrongShape { get; } = new(
        id: "CPG1007",
        title: "Packet's constructor setup is invalid",
        messageFormat: """
            '{0}' must be one of:
            - Reference or value type with one zero-param constructor (fields will be extracted from the properties)
            - Reference type with one constructor (fields will be extracted from the constructor arguments)
            - Value type with with one non-default constructor (fields will be extracted from the constructor arguments)
            """,
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PacketFieldHasUnsupportedType { get; } = new(
        id: "CPG1008",
        title: "Packet field has unsupported type",
        messageFormat: "'{0}' has unsupported type '{1}'",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FieldOrderNotSupportedOnConstructors { get; } = new(
        id: "CPG1009",
        title: "Field order may not be explicitly set for constructors",
        messageFormat: "'{0}' is in a constructor, and may not have its order explicitly set",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ConstructorParameterMustBeByValue { get; } = new(
        id: "CPG1010",
        title: "Constructors parameters may not use out, ref, in, or any other modifer",
        messageFormat: "{0} uses modifier {1} which is not allowed",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateOrderInPacket { get; } = new(
        id: "CPG1011",
        title: "Duplicate order values in packet",
        messageFormat: "{0} has the same order as {1}",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FieldsInPartialTypesMustUseExplicitOrdering { get; } = new(
        id: "CPG1012",
        title: "Fields in partial packet types must define an explicit order",
        messageFormat: "{0} must define an explicit order",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DoNotMixImplicitAndExplicitFieldOrdering { get; } = new(
        id: "CPG1013",
        title: "Do not mix usage of implicit and explicit field ordering",
        messageFormat: "{0} has both explicitly and implicitly ordered fields",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
