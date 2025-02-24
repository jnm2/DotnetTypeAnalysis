using System.Globalization;

namespace DotnetTypeAnalysis;

internal static class NameUtils
{
    public static (string TypeName, int GenericArity) ParseTypeName(string typeName)
    {
        if (typeName.AsSpan().Contains('+'))
            throw new ArgumentException("Nested type names must be parsed a segment at a time.", nameof(typeName));

        var separator = typeName.LastIndexOf('`');
        return separator != -1
            ? (typeName[..separator], int.Parse(typeName[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture))
            : (typeName, 0);
    }

    public static bool IsUnderNamespace(Type type, string @namespace)
    {
        return string.IsNullOrEmpty(@namespace)
            || (type.Namespace?.StartsWith(@namespace, StringComparison.Ordinal) == true
                && (type.Namespace.Length == @namespace.Length || type.Namespace[@namespace.Length] == '.'));
    }
}
