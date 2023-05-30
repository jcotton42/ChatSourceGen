using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ChatPacketGenerator;

/*
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class CodeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Diagnostics.PacketGroupsMustBeStaticClasses.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.sp
        }
    }
}
*/
