using System.Text;

using ChatPacketGenerator.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ChatPacketGenerator.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            context.AddSource("Attributes.g.cs", SourceText.From(SourceConstants.Attributes, Encoding.UTF8));
        });

        var parseResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceConstants.PacketGroupAttributeName,
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.GetPacketGroup)
            .Where(result => result is not (null, null));
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
    PacketCreationType CreationType,
    EquatableArray<PacketFieldInfo>? Fields = null);

internal enum PacketCreationType
{
    ObjectInitializer,
    Constructor,
    EmptyConstructor,
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
    EnumByte,
    EnumSByte,
    EnumOtherInteger,
}
