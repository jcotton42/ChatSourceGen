using ChatPacketGenerator;

namespace Chat;

[PacketGroup]
public partial record ChatPacket
{
    [PacketId(0)]
    public sealed partial record Hello(string Name, string Password) : ChatPacket;

    [PacketId(1)]
    public sealed partial record Ping(string Token) : ChatPacket;

    [PacketId(2)]
    public sealed partial record Pong(string Token) : ChatPacket;

    [PacketId(3)]
    public sealed partial record Message(string Text) : ChatPacket;

    [PacketId(4)]
    public sealed partial record Goodbye : ChatPacket;
}
