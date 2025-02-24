using System.Reflection;

namespace DotnetTypeAnalysis;

internal static class Queries
{
    public static IEnumerable<MethodInfo> GetExtensionMethods(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .AsParallel()
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.DeclaringType is null && ReflectionUtils.ContainsExtensionMethods(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(ReflectionUtils.IsExtensionMethod);
    }
}
