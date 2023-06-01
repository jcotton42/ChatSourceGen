namespace PacketTransport.Generator;

public static class SourceConstants
{
    public const string GeneratedHeader = """
        // <auto-generated>
        //     Automatically generated by PacketTransport.Generator.
        //     Changes made to this file may be lost and may cause undesirable behaviour.
        // </auto-generated>

        #nullable enable
        """;

    public const string PacketGroupAttributeName = "PacketTransport.PacketGroupAttribute";
    public const string PacketAttributeName = "PacketTransport.PacketAttribute";
    public const string PacketFieldAttributeName = "PacketTransport.PacketFieldAttribute";

    public const string ParserUsings = """
        using System.Buffers;
        using System.Diagnostics.CodeAnalysis;
        using System.Text;
        using System.Runtime.CompilerServices;
        using PacketTransport;
        """;
}