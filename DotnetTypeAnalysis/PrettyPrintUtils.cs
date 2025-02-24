using System.Reflection;
using System.Text;

namespace DotnetTypeAnalysis;

internal static class PrettyPrintUtils
{
    public static string FormatTypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return FormatTypeName(underlyingType) + '?';

        if (type.IsArray)
            return FormatTypeName(type.GetElementType()!) + "[]";

        if (type.IsByRef)
            return "ref " + FormatTypeName(type.GetElementType()!);

        if (type.IsPointer)
            return FormatTypeName(type.GetElementType()!) + '*';

        if (type.FullName == typeof(int).FullName) return "int";
        if (type.FullName == typeof(string).FullName) return "string";
        if (type.FullName == typeof(long).FullName) return "long";
        if (type.FullName == typeof(bool).FullName) return "bool";
        if (type.FullName == typeof(void).FullName) return "void";
        if (type.FullName == typeof(object).FullName) return "object";
        if (type.FullName == typeof(char).FullName) return "char";
        if (type.FullName == typeof(byte).FullName) return "byte";
        if (type.FullName == typeof(short).FullName) return "short";
        if (type.FullName == typeof(ushort).FullName) return "ushort";
        if (type.FullName == typeof(uint).FullName) return "uint";
        if (type.FullName == typeof(ulong).FullName) return "ulong";
        if (type.FullName == typeof(float).FullName) return "float";
        if (type.FullName == typeof(double).FullName) return "double";
        if (type.FullName == typeof(decimal).FullName) return "decimal";
        if (type.FullName == typeof(sbyte).FullName) return "sbyte";
        if (type.FullName == typeof(nint).FullName) return "nint";
        if (type.FullName == typeof(nuint).FullName) return "nuint";

        return NameUtils.ParseTypeName(type.Name).TypeName + FormatTypeArguments(type.GenericTypeArguments);
    }

    public static string FormatTypeArguments(IReadOnlyList<Type> typeArguments)
    {
        if (typeArguments is [])
            return "";

        var builder = new StringBuilder();
        builder.Append('<');
        builder.AppendJoin(", ", typeArguments.Select(FormatTypeName));
        builder.Append('>');
        return builder.ToString();
    }

    public static string FormatMember(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => FormatField(field),
            PropertyInfo property => FormatProperty(property),
            MethodInfo method => FormatMethod(method),
            _ => throw new NotImplementedException(),
        };
    }

    public static string FormatField(FieldInfo field)
    {
        var builder = new StringBuilder();
        if (field.IsInitOnly)
            builder.Append("readonly ");
        builder.Append(FormatTypeName(field.FieldType));
        builder.Append(' ');
        builder.Append(field.Name);
        return builder.ToString();
    }

    public static string FormatProperty(PropertyInfo property)
    {
        var builder = new StringBuilder();
        builder.Append(FormatTypeName(property.PropertyType));
        builder.Append(' ');
        builder.Append(property.Name);
        builder.Append(" { ");

        var accessors = new List<string>();
        if (property.CanRead)
            accessors.Add("get;");
        if (property.CanWrite)
            accessors.Add(ReflectionUtils.IsInitOnly(property.GetMethod!) ? "init;" : "set;");

        builder.AppendJoin(" ", accessors);
        builder.Append(" }");
        return builder.ToString();
    }

    public static string FormatMethod(MethodInfo method)
    {
        var builder = new StringBuilder();
        builder.Append(FormatTypeName(method.ReturnType));
        builder.Append(' ');
        builder.Append(method.Name);
        builder.Append(FormatTypeArguments(method.GetGenericArguments()));
        builder.Append('(');
        builder.AppendJoin(", ", method.GetParameters().Select(FormatParameter));
        builder.Append(')');
        return builder.ToString();
    }

    public static string FormatParameter(ParameterInfo parameter)
    {
        var builder = new StringBuilder();

        var parameterType = parameter.ParameterType;
        if (parameter is { IsOut: true, ParameterType.IsByRef: true })
        {
            builder.Append("out ");
            parameterType = parameterType.GetElementType()!;
        }

        builder.Append(FormatTypeName(parameterType));
        return builder.ToString();
    }
}
