using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Spectre.Console;
using System.Collections.Immutable;
using System.IO;

namespace DotnetTypeAnalysis;

public static class AssemblyCollector
{
    public static async Task<ImmutableArray<PackageAssembly>> LoadPackageAssembliesAsync(string nugetFeedUrl, int topPackageCount, CancellationToken cancellationToken)
    {
        var repository = Repository.Factory.GetCoreV3(nugetFeedUrl);
        var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);

        var packages = await searchResource.SearchMultiPagedAsync(
                searchTerm: null,
                filters: new(includePrerelease: false, SearchFilterType.IsLatestVersion) { PackageTypes = [PackageType.Dependency.Name] },
                skip: 0, take: topPackageCount, NullLogger.Instance, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var downloadResource = await repository.GetResourceAsync<DownloadResource>(cancellationToken);
        var packageDownloadContext = new PackageDownloadContext(new SourceCacheContext());

        var settings = Settings.LoadDefaultSettings(root: null);
        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

        var assemblies = ImmutableArray.CreateBuilder<PackageAssembly>();

        await AnsiConsole.Progress().AutoClear(true).StartAsync(async context =>
        {
            var task = context.AddTask($"Caching top {topPackageCount:N0} NuGet packages", autoStart: true, maxValue: packages.Length);

            await Parallel.ForEachAsync(packages, cancellationToken, async (package, cancellationToken) =>
            {
                var result = await downloadResource.GetDownloadResourceResultAsync(package.Identity, packageDownloadContext, globalPackagesFolder, NullLogger.Instance, cancellationToken);
                var refGroups = (await result.PackageReader.GetItemsAsync("ref", cancellationToken)).ToArray();
                if (refGroups is [])
                    refGroups = [.. await result.PackageReader.GetReferenceItemsAsync(cancellationToken)];

                if (ChooseLatest([.. refGroups]) is { } group)
                {
                    foreach (var path in group.Items)
                    {
                        if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            await using var stream = await result.PackageReader.GetStreamAsync(path, cancellationToken);
                            var contents = await stream.ReadToEndAsync(cancellationToken);

                            if (Utils.IsManagedAssembly(contents))
                            {
                                lock (assemblies)
                                    assemblies.Add(new PackageAssembly(package.Identity, package.DownloadCount, group.TargetFramework, Path.GetFileName(path), contents));
                            }
                        }
                    }
                }

                task.Increment(1);
            });
        });

        return assemblies.DrainToImmutable();
    }

    private static T? ChooseLatest<T>(IReadOnlyCollection<T> items) where T : class, IFrameworkSpecific
    {
        if (items
            .Where(i => i.TargetFramework.Framework == FrameworkConstants.FrameworkIdentifiers.NetCoreApp)
            .ToArray() is (not []) and var netCoreAppItems)
        {
            items = netCoreAppItems;
        }
        else if (items.Any(i => i.TargetFramework.IsAny) && items.Any(i => !i.TargetFramework.IsAny))
        {
            items = [.. items.Where(i => !i.TargetFramework.IsAny)];
        }

        return items.GetNearest(NuGetFramework.AnyFramework);
    }
}
