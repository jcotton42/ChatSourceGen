using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using ChatPacketGenerator.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChatPacketGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            context.AddSource("Attributes.g.cs", SourceText.From(SourceConstants.Attributes, Encoding.UTF8));
        });

        context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceConstants.PacketGroupAttributeName,
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: GetPacketGroup)
            .Where(result => result is not (null, null));
    }

    private static (PacketGroupInfo?, EquatableArray<Diagnostic>?) GetPacketGroup(
        GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (context.TargetSymbol is not INamedTypeSymbol packetGroupSymbol) return (null, null);
        var packetGroupSyntax = (ClassDeclarationSyntax)context.TargetNode;

        var packetCandidates = packetGroupSymbol.GetTypeMembers();
        if (packetCandidates.IsEmpty) return (null, null);

        var ns = packetGroupSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeHierarchy = GetTypeHierarchy(packetGroupSyntax);

        using var packets = ImmutableArrayBuilder<PacketInfo>.Rent();
        using var diagnostics = ImmutableArrayBuilder<Diagnostic>.Rent();
        foreach (var candidate in packetCandidates)
        {
            ct.ThrowIfCancellationRequested();
            var attribute = candidate.GetFirstAttributeOfTypeOrDefault(SourceConstants.PacketAttributeName);
            // TODO maybe emit diagnostic?
            if (attribute is null) continue;
            if (!attribute.TryGetNamedArgument("Id", out var idConstant) || idConstant.Value is not int packetId)
            {
                // Id is marked as required, so the compile won't proceed without it, just skip this one
                continue;
            }

            var packet = candidate switch
            {
                { InstanceConstructors: [{ Parameters: [] }] } =>
                    GetPacketInfoFromProperties(candidate, packetId, diagnostics, ct),
                { InstanceConstructors: [var ctor] } => GetPacketInfoFromConstructor(ctor, packetId, diagnostics, ct),
                { TypeKind: TypeKind.Struct, InstanceConstructors: [var ctor1, var ctor2] } =>
                    ctor1.Parameters is not []
                        ? GetPacketInfoFromConstructor(ctor1, packetId, diagnostics, ct)
                        : GetPacketInfoFromConstructor(ctor2, packetId, diagnostics, ct),
                _ => null,
            };

            if (packet is null)
        }
    }

    private static TypeHierarchyInfo? GetTypeHierarchy(TypeDeclarationSyntax syntax)
    {
        TypeHierarchyInfo? hierarchy = null;
        var parent = syntax.Parent;
        while (parent is TypeDeclarationSyntax typeSyntax)
        {
            hierarchy = typeSyntax switch
            {
                RecordDeclarationSyntax { ClassOrStructKeyword.Span.IsEmpty: false } recordSyntax => new
                    TypeHierarchyInfo(
                        recordSyntax.Identifier.ToString(),
                        $"{recordSyntax.Keyword} {recordSyntax.ClassOrStructKeyword}",
                        recordSyntax.Modifiers.ToString(),
                        hierarchy),
                _ => new TypeHierarchyInfo(
                    typeSyntax.Identifier.ToString(),
                    typeSyntax.Keyword.ToString(),
                    typeSyntax.Modifiers.ToString(),
                    hierarchy),
            };

            parent = parent.Parent;
        }

        return hierarchy;
    }

    private static PacketInfo? GetPacketInfoFromProperties(
        INamedTypeSymbol symbol,
        int packetId,
        ImmutableArrayBuilder<Diagnostic> diagnostics,
        CancellationToken ct)
    {
    }

    private static PacketInfo? GetPacketInfoFromConstructor(
        IMethodSymbol ctor,
        int packetId,
        ImmutableArrayBuilder<Diagnostic> diagnostics,
        CancellationToken ct)
    {
        var fields = ImmutableArray.CreateBuilder<PacketFieldInfo>(ctor.Parameters.Length);
        foreach (var param in ctor.Parameters)
        {
            ct.ThrowIfCancellationRequested();
            var field = GetPacketFieldInfo(param, param.Type);

            if (field is null)
            {
                // report diagnostic and bail
            }
        }
    }

    private static PacketFieldInfo? GetPacketFieldInfo(ISymbol symbol, ITypeSymbol typeSymbol)
    {
        int? order = symbol
            .GetFirstAttributeOfTypeOrDefault(SourceConstants.PacketFieldAttributeName)
            ?.TryGetNamedArgument("Order", out var orderConstant) ?? false
            ? orderConstant.Value as int?
            : null;
        symbol.Locations[0].SourceSpan.CompareTo()

        return typeSymbol switch
        {
            { SpecialType: SpecialType.System_String } => new PacketFieldInfo(
                symbol.Name,
                order,
                PacketFieldType.String),
            { SpecialType: SpecialType.System_Byte } => new PacketFieldInfo(
                symbol.Name,
                order,
                PacketFieldType.Byte),
            { SpecialType: SpecialType.System_SByte } => new PacketFieldInfo(
                symbol.Name,
                order,
                PacketFieldType.SByte),
            {
                SpecialType: SpecialType.System_Int16
                or SpecialType.System_UInt16
                or SpecialType.System_Int32
                or SpecialType.System_UInt32
                or SpecialType.System_Int64
                or SpecialType.System_UInt64
            } integerType => new PacketFieldInfo(
                symbol.Name,
                order,
                PacketFieldType.OtherInteger,
                OtherIntegerType: integerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: var underlyingType } enumType =>
                new PacketFieldInfo(
                    symbol.Name,
                    order,
                    PacketFieldType.Enum,
                    EnumType: enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    EnumUnderlyingType: underlyingType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            _ => null,
        };
    }
}

internal sealed record PacketGroupInfo(
    string Name,
    string? Namespace,
    string Modifiers,
    TypeHierarchyInfo? TypeHierarchyInfo,
    EquatableArray<PacketInfo> Packets);

internal sealed record TypeHierarchyInfo(string Name, string Keyword, string Modifiers, TypeHierarchyInfo? Child);

internal sealed record PacketInfo(
    string Name,
    int Id,
    bool MustUseExplicitOrdering,
    PacketCreationType CreationType,
    EquatableArray<PacketFieldInfo> Fields);

internal enum PacketCreationType
{
    ObjectInitializer,
    Constructor,
}

internal sealed record PacketFieldInfo(
    string Name,
    int? Order,
    PacketFieldType Type,
    string? OtherIntegerType = null,
    string? EnumType = null,
    string? EnumUnderlyingType = null);

internal enum PacketFieldType
{
    String,
    Byte,
    SByte,
    OtherInteger,
    Enum,
}
