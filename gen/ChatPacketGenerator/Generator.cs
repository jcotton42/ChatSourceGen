using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChatPacketGenerator;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(postContext =>
        {
            postContext.AddSource(
                "Attributes.g.cs",
                SourceText.From(SourceConstants.Attributes, Encoding.UTF8));
        });

        var packetGroups = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceConstants.PacketGroupAttributeName,
                predicate: (node, _) => node is RecordDeclarationSyntax,
                transform: Parse);
    }

    private static ParseResult Parse(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var diagnostics = new List<Diagnostic>();
        var packetGroups = new List<PacketGroup>();

        var declarationSyntax = (RecordDeclarationSyntax)context.TargetNode;
        // TODO handle this being an error
        var groupSymbol = (INamedTypeSymbol)context.TargetSymbol;

        if (!TryGetNamespace(context.TargetNode, out var @namespace))
        {
            diagnostics.Add(Diag(
                Diagnostics.NestedGroupTypesNotSupported,
                declarationSyntax.GetLocation(),
                context.TargetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
            return new ParseResult(diagnostics, packetGroups);
        }

        var declarationText = GetTypeDeclaration(context, declarationSyntax, diagnostics);
    }

    private static bool TryGetNamespace(SyntaxNode node, [NotNullWhen(true)] out string? @namespace)
    {
        @namespace = default;
        var parts = new List<string>();

        while (true)
        {
            switch (node.Parent)
            {
                case BaseNamespaceDeclarationSyntax namespaceSyntax:
                    parts.Add(namespaceSyntax.Name.ToString());
                    break;
                case null or CompilationUnitSyntax:
                    parts.Reverse();
                    @namespace = string.Join(".", parts);
                    return true;
                default:
                    return false;
            }

            node = node.Parent;
        }
    }

    private static string GetTypeDeclaration(GeneratorAttributeSyntaxContext context,
        RecordDeclarationSyntax declarationSyntax,
        List<Diagnostic> diagnostics)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        // + 3 to account for "record", a possible "class" or "struct", and the name
        var declarationParts = new string[declarationSyntax.Modifiers.Count + 3];
        var isPartial = false;
        var i = 0;
        foreach (var modifier in declarationSyntax.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword)) isPartial = true;
            declarationParts[i] = modifier.Text;
            i++;
        }

        declarationParts[i] = "record";
        i++;

        if (declarationSyntax.ClassOrStructKeyword.IsKind(SyntaxKind.ClassKeyword)
            || declarationSyntax.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
        {
            declarationParts[i] = declarationSyntax.ClassOrStructKeyword.Text;
            i++;
        }

        declarationParts[i] = declarationSyntax.Identifier.Text;

        if (!isPartial)
        {
            diagnostics.Add(Diag(
                Diagnostics.PacketGroupsMustBePartial,
                declarationSyntax.GetLocation(),
                context.TargetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }

        return string.Join(" ", declarationParts, 0, i + 1);
    }

    private static Diagnostic Diag(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs) =>
        Diagnostic.Create(descriptor, location, messageArgs);
}

internal readonly record struct ParseResult(List<Diagnostic> Diagnostics, List<PacketGroup> PacketGroups);
internal readonly record struct PacketGroup(string? Namespace, string DeclarationText, int Id, List<PacketField> Fields);
internal readonly record struct PacketField(string Name, string TypeName, string? EnumType);
