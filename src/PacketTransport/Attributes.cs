namespace PacketTransport;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PacketGroupAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PacketAttribute : Attribute
{
    public required int Id { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PacketFieldAttribute : Attribute
{
    public int Order { get; init; }
}