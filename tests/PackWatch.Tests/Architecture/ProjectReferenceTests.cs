using System.Xml.Linq;

namespace PackWatch.Tests.Architecture;

public sealed class ProjectReferenceTests
{
    private static readonly string RepositoryRoot = GetRepositoryRoot();

    [Theory]
    [InlineData("src/PackWatch.Domain/PackWatch.Domain.csproj", 0)]
    [InlineData("src/PackWatch.Application/PackWatch.Application.csproj", 2)]
    [InlineData("src/PackWatch.Infrastructure/PackWatch.Infrastructure.csproj", 3)]
    [InlineData("src/PackWatch.Persistence/PackWatch.Persistence.csproj", 3)]
    public void Projects_keep_expected_reference_counts(string projectPath, int expectedReferenceCount)
    {
        var document = XDocument.Load(Path.Combine(RepositoryRoot, projectPath));
        var referenceCount = document
            .Descendants("ProjectReference")
            .Count();

        Assert.Equal(expectedReferenceCount, referenceCount);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PackWatch.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate repository root.");
        }

        return directory.FullName;
    }
}
