using ChatPacketGenerator;

namespace Chat;

[PacketGroup]
public static partial class ChatPacket
{
    [Packet(Id = 0)]
    public sealed record Hello(string Name, string Password);

    [Packet(Id = 1)]
    public sealed record Ping(string Token);

    [Packet(Id = 2)]
    public sealed record Pong(string Token);

    [Packet(Id = 3)]
    public sealed record Message(string Text);

    [Packet(Id = 4)]
    public sealed record Goodbye
    {
        public string Foo { get; init; }
        public MyEnum State { get; init; }
    }
}

public enum MyEnum
{
    A,
    B,
    C,
}