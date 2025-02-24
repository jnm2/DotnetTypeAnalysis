using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace DotnetTypeAnalysis;

public sealed record PackageAssembly(
    PackageIdentity PackageIdentity,
    long? PackageDownloadCount,
    NuGetFramework TargetFramework,
    string FileName,
    byte[] AssemblyBytes)
{
    public override string ToString()
    {
        return $"{PackageIdentity}/{FileName} ({TargetFramework})";
    }
}
