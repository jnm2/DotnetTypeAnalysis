using System.Reflection;
using System.Runtime.CompilerServices;

namespace DotnetTypeAnalysis;

internal static class ReflectionUtils
{
    public static Type? GetSiblingType(Type type, string siblingName)
    {
        if (type.DeclaringType is not null)
            return type.DeclaringType.GetNestedType(siblingName, BindingFlags.Public | BindingFlags.NonPublic);

        return type.Assembly.GetType(string.IsNullOrEmpty(type.Namespace)
            ? siblingName
            : type.Namespace + '.' + siblingName);
    }

    public static bool ContainsExtensionMethods(Type type)
    {
        return HasAttribute(type, typeof(ExtensionAttribute).FullName!);
    }

    public static bool IsExtensionMethod(MethodInfo method)
    {
        return HasAttribute(method, typeof(ExtensionAttribute).FullName!);
    }

    public static bool IsVisibleOutsideAssembly(Type type)
    {
        return (type.Attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public => true,
            TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem => IsVisibleOutsideAssembly(type.DeclaringType!),
            _ => false,
        };
    }

    public static bool IsVisibleOutsideAssembly(MethodInfo method, bool assumeDeclaringTypeIsVisibleOutsideAssembly = false)
    {
        return (method.Attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem =>
                assumeDeclaringTypeIsVisibleOutsideAssembly || IsVisibleOutsideAssembly(method.DeclaringType!),
            _ => false,
        };
    }

    public static bool IsInitOnly(MethodInfo method)
    {
        return method.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.FullName == typeof(IsExternalInit).FullName!);
    }

    public static bool IsReadOnly(MethodInfo method)
    {
        return HasAttribute(method, typeof(IsReadOnlyAttribute).FullName!);
    }

    public static bool IsReadOnly(Type type)
    {
        return HasAttribute(type, typeof(IsReadOnlyAttribute).FullName!);
    }

    private static bool HasAttribute(MemberInfo member, string attributeFullName)
    {
        return member.GetCustomAttributesData().Any(d => d.AttributeType.FullName == attributeFullName);
    }
}
