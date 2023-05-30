using Microsoft.CodeAnalysis;

namespace ChatPacketGenerator;

public static class SymbolExtensions
{
    public static bool HasAttribute(this ISymbol symbol, string attributeFullyQualifiedName) =>
        symbol.GetFirstAttributeOfTypeOrDefault(attributeFullyQualifiedName) is not null;

    public static AttributeData? GetFirstAttributeOfTypeOrDefault(this ISymbol symbol, string attributeFullyQualifiedName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeFullyQualifiedName)
            {
                return attribute;
            }
        }

        return null;
    }
}

public static class AttributeDataExtensions
{
    public static bool TryGetNamedArgument(this AttributeData attribute, string name, out TypedConstant value)
    {
        foreach (var kvp in attribute.NamedArguments)
        {
            if (kvp.Key == name)
            {
                value = kvp.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
