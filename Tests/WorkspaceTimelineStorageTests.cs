using ChangeTrace.CredentialTrace.Interfaces;
using ChangeTrace.CredentialTrace.Profiles;
using ChangeTrace.CredentialTrace.Services;
using Xunit;

namespace ChangeTrace.Tests;

public sealed class WorkspaceTimelineStorageTests
{
    [Fact]
    public async Task CreateTimelinePath_UsesOrganizationWorkspaceRepositoryAndUniqueFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), "ChangeTrace.Tests");
        var organization = CreateOrganization("Microsoft");
        var workspace = WorkspaceProfile.Create(organization.Id, "MsQuic");
        var storage = new WorkspaceTimelineStorage(root, new InMemoryProfileStore<OrganizationProfile>(organization));
        var exportedAt = new DateTimeOffset(2026, 5, 30, 12, 34, 56, TimeSpan.Zero);

        var path = await storage.CreateTimelinePathAsync(
            workspace,
            "https://github.com/microsoft/msquic.git",
            exportedAt,
            "01JY0000000000000000000000");

        var expected = Path.Combine(
            root,
            "workspaces",
            "microsoft",
            "msquic",
            "timelines",
            "microsoft-msquic",
            "20260530T123456Z-01jy0000000000000000000000.gittrace");

        Assert.Equal(expected, path);
    }

    [Fact]
    public async Task CreateTimelinePath_UsesLocalRepositoryNameWhenSourceIsPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "ChangeTrace.Tests");
        var organization = CreateOrganization("Local Org");
        var workspace = WorkspaceProfile.Create(organization.Id, "Local Workspace");
        var storage = new WorkspaceTimelineStorage(root, new InMemoryProfileStore<OrganizationProfile>(organization));
        var exportedAt = new DateTimeOffset(2026, 5, 30, 12, 34, 56, TimeSpan.Zero);

        var path = await storage.CreateTimelinePathAsync(
            workspace,
            "/tmp/repos/My Repo.git",
            exportedAt,
            "export-1");

        Assert.EndsWith(
            Path.Combine("workspaces", "local-org", "local-workspace", "timelines", "my-repo", "20260530T123456Z-export-1.gittrace"),
            path);
    }

    private static OrganizationProfile CreateOrganization(string name)
        => new()
        {
            Id = Ulid.NewUlid(),
            Name = name,
            Provider = "github",
            CreatedAt = DateTime.UtcNow,
            SessionId = Ulid.NewUlid()
        };

    private sealed class InMemoryProfileStore<T>(params T[] profiles) : IProfileStore<T>
        where T : class, IProfile
    {
        private readonly Dictionary<Ulid, T> _profiles = profiles.ToDictionary(profile => profile.Id);

        public Task SaveAsync(T profile, CancellationToken ct = default)
        {
            _profiles[profile.Id] = profile;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Ulid id, CancellationToken ct = default)
        {
            _profiles.Remove(id);
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync(Ulid id, CancellationToken ct = default)
            => Task.FromResult(_profiles.GetValueOrDefault(id));

        public Task<T?> GetByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_profiles.Values.FirstOrDefault(profile =>
                profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<T>>(_profiles.Values);
    }
}
