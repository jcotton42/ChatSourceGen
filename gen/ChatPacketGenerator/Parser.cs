using System.Collections.Generic;
using System.Threading;

using ChatPacketGenerator.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChatPacketGenerator;

internal static class Parser
{
    public static (PacketGroupInfo? PacketGroup, EquatableArray<Diagnostic>? Diagnostics) GetPacketGroup(
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

            if (attribute is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.PacketGroupsMustNotContainNonPacketTypes,
                    candidate.Locations[0],
                    candidate.Name));
                continue;
            }

            if (!attribute.TryGetNamedArgument("Id", out var idConstant) || idConstant.Value is not int packetId)
            {
                // Id is marked as required, so the compile won't proceed without it, just skip this one
                continue;
            }

            // TODO only look at public and internal constructors
            var (packet, typeDoesNotMatch) = candidate switch
            {
                { InstanceConstructors: [{ Parameters: [] }] } =>
                    (GetPacketInfoFromProperties(candidate, packetId, diagnostics, ct), false),
                { InstanceConstructors: [var ctor] } =>
                    (GetPacketInfoFromConstructor(candidate.Name, ctor, packetId, diagnostics, ct), false),
                { TypeKind: TypeKind.Struct, InstanceConstructors: [var ctor1, var ctor2] } =>
                    ctor1.Parameters is not []
                        ? (GetPacketInfoFromConstructor(candidate.Name, ctor1, packetId, diagnostics, ct), false)
                        : (GetPacketInfoFromConstructor(candidate.Name, ctor2, packetId, diagnostics, ct), false),
                _ => (null, true),
            };

            if (packet is null)
            {
                if (typeDoesNotMatch)
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.PacketHasWrongShape,
                        candidate.Locations[0],
                        candidate.Name));
                }

                continue;
            }

            packets.Add(packet);
        }

        var packetGroup = new PacketGroupInfo(
            packetGroupSymbol.Name,
            ns,
            packetGroupSyntax.Modifiers.ToString(),
            typeHierarchy,
            packets.ToImmutable());

        return (packetGroup, diagnostics.ToImmutable());
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
        INamedTypeSymbol typeSymbol,
        int packetId,
        ImmutableArrayBuilder<Diagnostic> diagnostics,
        CancellationToken ct)
    {
        var typeHasMultipleParts = typeSymbol.Locations.Length > 1;

        var skipGeneration = false;
        var usingImplicitOrdering = false;
        var implicitOrdering = new SortedDictionary<TextSpan, (PacketFieldInfo Field, Location Location)>();
        var usingExplicitOrdering = typeHasMultipleParts;
        var explicitOrdering = new SortedDictionary<int, (PacketFieldInfo Field, Location Location)>();

        foreach (var memberSymbol in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();
            if (memberSymbol is not IPropertySymbol
                {
                    DeclaredAccessibility:
                    Accessibility.Public
                    or Accessibility.Internal
                    or Accessibility.ProtectedOrInternal,
                    IsImplicitlyDeclared: false,
                } propertySymbol)
            {
                continue;
            }

            var field = GetPacketFieldInfo(propertySymbol, propertySymbol.Type, diagnostics);
            if (field is null)
            {
                skipGeneration = true;
                continue;
            }

            if (field.Order is not null)
            {
                usingExplicitOrdering = true;
                if (explicitOrdering.TryGetValue(field.Order.Value, out var existing))
                {
                    skipGeneration = true;
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.DuplicateOrderInPacket,
                        propertySymbol.Locations[0],
                        new[] { existing.Location },
                        propertySymbol.Name,
                        existing.Field.Name));
                    continue;
                }

                explicitOrdering.Add(field.Order.Value, (field, propertySymbol.Locations[0]));
            }
            else if (typeHasMultipleParts)
            {
                usingImplicitOrdering = true;
                skipGeneration = true;
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.FieldsInPartialTypesMustUseExplicitOrdering,
                    propertySymbol.Locations[0],
                    propertySymbol.Name));
            }
            else
            {
                usingImplicitOrdering = true;
                implicitOrdering.Add(propertySymbol.Locations[0].SourceSpan, (field, propertySymbol.Locations[0]));
            }
        }

        if (usingExplicitOrdering && usingImplicitOrdering)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.DoNotMixImplicitAndExplicitFieldOrdering,
                typeSymbol.Locations[0],
                typeSymbol.Name));
            skipGeneration = true;
        }

        if (skipGeneration) return null;

        if (implicitOrdering.Count == 0 && explicitOrdering.Count == 0)
        {
            return new PacketInfo(
                typeSymbol.Name,
                packetId,
                PacketCreationType.EmptyConstructor);
        }

        using var fields = ImmutableArrayBuilder<PacketFieldInfo>.Rent();

        if (usingExplicitOrdering)
        {
            foreach (var kvp in explicitOrdering)
            {
                fields.Add(kvp.Value.Field);
            }
        }
        else if (usingImplicitOrdering)
        {
            foreach (var kvp in implicitOrdering)
            {
                fields.Add(kvp.Value.Field);
            }
        }

        return new PacketInfo(
            typeSymbol.Name,
            packetId,
            PacketCreationType.ObjectInitializer,
            fields.ToImmutable());
    }

    private static PacketInfo? GetPacketInfoFromConstructor(
        string name,
        IMethodSymbol ctor,
        int packetId,
        ImmutableArrayBuilder<Diagnostic> diagnostics,
        CancellationToken ct)
    {
        using var fields = ImmutableArrayBuilder<PacketFieldInfo>.Rent();
        var skipGeneration = false;
        foreach (var param in ctor.Parameters)
        {
            ct.ThrowIfCancellationRequested();
            if (param.IsThis) continue;
            if (param.RefKind is not RefKind.None)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.ConstructorParameterMustBeByValue,
                    param.Locations[0],
                    param.Name,
                    param.RefKind.ToString().ToLowerInvariant()));
                skipGeneration = true;
                continue;
            }

            var field = GetPacketFieldInfo(param, param.Type, diagnostics);

            if (field is null)
            {
                skipGeneration = true;
                continue;
            }

            if (field.Order is not null)
            {
                diagnostics.Add(Diagnostic.Create(
                    Diagnostics.FieldOrderNotSupportedOnConstructors,
                    param.Locations[0],
                    param.Name));
                skipGeneration = true;
                continue;
            }

            fields.Add(field with { Order = param.Ordinal });
        }

        if (skipGeneration) return null;

        return new PacketInfo(
            name,
            packetId,
            PacketCreationType.Constructor,
            fields.ToImmutable());
    }

    private static PacketFieldInfo? GetPacketFieldInfo(
        ISymbol symbol,
        ITypeSymbol typeSymbol,
        ImmutableArrayBuilder<Diagnostic> diagnostics)
    {
        int? order = symbol
            .GetFirstAttributeOfTypeOrDefault(SourceConstants.PacketFieldAttributeName)
            ?.TryGetNamedArgument("Order", out var orderConstant) ?? false
            ? orderConstant.Value as int?
            : null;

        var field = typeSymbol switch
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
            INamedTypeSymbol
                {
                    TypeKind: TypeKind.Enum, EnumUnderlyingType.SpecialType: SpecialType.System_Byte
                } enumType =>
                new PacketFieldInfo(
                    symbol.Name,
                    order,
                    PacketFieldType.EnumByte,
                    EnumType: enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            INamedTypeSymbol
                {
                    TypeKind: TypeKind.Enum, EnumUnderlyingType.SpecialType: SpecialType.System_SByte
                } enumType =>
                new PacketFieldInfo(
                    symbol.Name,
                    order,
                    PacketFieldType.EnumSByte,
                    EnumType: enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: var underlyingType } enumType =>
                new PacketFieldInfo(
                    symbol.Name,
                    order,
                    PacketFieldType.EnumOtherInteger,
                    EnumType: enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    EnumUnderlyingType: underlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            _ => null,
        };

        if (field is null)
        {
            diagnostics.Add(Diagnostic.Create(
                Diagnostics.PacketFieldHasUnsupportedType,
                symbol.Locations[0],
                symbol.Name,
                typeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }

        return field;
    }
}
