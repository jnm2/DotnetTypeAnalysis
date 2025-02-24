using DotnetTypeAnalysis;
using System.IO;
using System.Reflection;

var assemblies = await AssemblyCollector.LoadPackageAssembliesAsync("https://api.nuget.org/v3/index.json", topPackageCount: 8_000, CancellationToken.None);

if (!assemblies.Any(a => a.PackageIdentity.Id == "Microsoft.WindowsDesktop.App.Ref"))
    throw new NotImplementedException("Pick up desktop runtime libraries");

if (!assemblies.Any(a => a.PackageIdentity.Id == "Microsoft.AspNetCore.App.Ref"))
    throw new NotImplementedException("Pick up ASP.NET Core runtime libraries");

using var context = Utils.CreateMetadataLoadContextWithAllAssembliesLoaded(
    Basic.Reference.Assemblies.Net90.ReferenceInfos.All.Select(reference => (reference.FileName, reference.ImageBytes))
        .Concat(assemblies.Select(a => (a.FileName, a.AssemblyBytes)))
        .DistinctBy(a => a.FileName, StringComparer.OrdinalIgnoreCase));

foreach (var type in context.GetAssemblies()
    .SelectMany(assembly => assembly.GetExportedTypes())
    .OrderByDescending(type => NameUtils.IsUnderNamespace(type, "System"))
    .ThenByDescending(type => NameUtils.IsUnderNamespace(type, "Microsoft"))
    .ThenBy(type => type.Namespace))
{
    if (type is { IsSealed: true, IsAbstract: true })
        continue;

    if (NameUtils.ParseTypeName(type.Name) is not { TypeName: var name, GenericArity: > 0 })
        continue;

    if (ReflectionUtils.GetSiblingType(type, name) is not { } nonGenericVersion)
        continue;

    var members = new List<(string Display, Type AccessedType)>();

    try
    {
        foreach (var member in nonGenericVersion.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            try
            {
                Type accessedType;

                switch (member)
                {
                    case MethodInfo { IsSpecialName: false } method:
                        accessedType = method.ReturnType;
                        break;
                    case PropertyInfo { CanRead: true } property:
                        accessedType = property.PropertyType;
                        break;
                    case FieldInfo field:
                        accessedType = field.FieldType;
                        break;
                    default:
                        continue;
                }

                if (accessedType.IsGenericType && accessedType.GetGenericTypeDefinition() == type)
                    members.Add((PrettyPrintUtils.FormatMember(member), accessedType));
            }
            catch (Exception ex) when (ex is FileNotFoundException or TypeLoadException)
            {
            }
        }
    }
    catch (Exception ex) when (ex is FileNotFoundException or TypeLoadException)
    {
    }

    if (members is not [])
    {
        Console.WriteLine($"{nonGenericVersion.FullName} ({type.Assembly.GetName().Name} {type.Assembly.GetName().Version}):");

        foreach (var member in members)
            Console.WriteLine($"- {member.Display}");

        Console.WriteLine();
    }
}
