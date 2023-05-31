using Verify = ChatPacketGenerator.Tests.Verifiers.CSharpAnalyzerVerifier<ChatPacketGenerator.Analyzer>;

namespace ChatPacketGenerator.Tests;

public class UnitTest1
{
    [Fact]
    public async Task PacketFieldsMustBeInAPacket()
    {
        var code = $$"""
            using ChatPacketGenerator;
            [PacketGroup]
            public static class ChatPacket
            {
                public sealed partial record Ping([property: PacketField] string Token);
            }

            {{SourceConstants.Attributes}}
            """;

        var expected = Verify
            .Diagnostic(Diagnostics.PacketFieldsMustBeInAPacket)
            .WithArguments("Ping")
            .WithLocation(line: 4, column: 33);

        await Verify.VerifyAnalyzerAsync(code, expected);
    }
}
