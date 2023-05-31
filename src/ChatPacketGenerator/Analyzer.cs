using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ChatPacketGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Analyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Diagnostics.PacketsMustBeInsidePacketGroup,
        Diagnostics.PacketFieldsMustBeInAPacket,
        Diagnostics.TypeMayNotBePacketAndPacketGroup,
        Diagnostics.PacketGroupsMayNotBeNested,
        Diagnostics.PacketGroupsMustBeStaticClasses);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        var isPacket = false;
        var isPacketGroup = false;

        var parentIsPacketGroup =
            typeSymbol.ContainingType?.HasAttribute(SourceConstants.PacketGroupAttributeName) ?? false;
        var typeIsStaticClass = typeSymbol is { TypeKind: TypeKind.Class, IsStatic: true };

        foreach (var attrib in typeSymbol.GetAttributes())
        {
            switch (attrib.AttributeClass?.ToDisplayString())
            {
                case SourceConstants.PacketAttributeName:
                    isPacket = true;
                    break;
                case SourceConstants.PacketGroupAttributeName:
                    isPacketGroup = true;
                    break;
            }
        }

        if (isPacket && !parentIsPacketGroup)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PacketsMustBeInsidePacketGroup,
                typeSymbol.Locations[0],
                typeSymbol.Name));
        }

        if (isPacket && isPacketGroup)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.TypeMayNotBePacketAndPacketGroup,
                typeSymbol.Locations[0],
                typeSymbol.Name));
        }

        if (isPacketGroup && parentIsPacketGroup)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PacketGroupsMayNotBeNested,
                typeSymbol.Locations[0],
                new[] { typeSymbol.ContainingType!.Locations[0] },
                typeSymbol.Name,
                typeSymbol.ContainingType!.Name));
        }

        if (isPacketGroup && !typeIsStaticClass)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PacketGroupsMustBeStaticClasses,
                typeSymbol.Locations[0],
                typeSymbol.Name));
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        var propertySymbol = (IPropertySymbol)context.Symbol;
        if (!propertySymbol.HasAttribute(SourceConstants.PacketFieldAttributeName)) return;

        if (!propertySymbol.ContainingType.IsAbstract
            && !propertySymbol.ContainingType.HasAttribute(SourceConstants.PacketAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PacketFieldsMustBeInAPacket,
                propertySymbol.Locations[0],
                propertySymbol.Name));
        }
    }
}
