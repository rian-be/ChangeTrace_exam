using System.CommandLine;
using ChangeTrace.Cli.Interfaces;
using ChangeTrace.Cli.Prompts;
using ChangeTrace.Configuration.Discovery;
using ChangeTrace.CredentialTrace.Interfaces;
using ChangeTrace.CredentialTrace.Profiles;
using ChangeTrace.CredentialTrace.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ChangeTrace.Cli.Handlers.Profiles.Organizations;

/// <summary>
/// Handler for 'org list' CLI command that displays organizations filtered by provider.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Implements <see cref="ICliHandler"/> to list <see cref="OrganizationProfile"/> objects.</item>
/// <item>Uses <see cref="IProfileStore{OrganizationProfile}"/> to retrieve stored organizations.</item>
/// <item>Displays results in a formatted <see cref="Spectre.Console.Table"/>.</item>
/// <item>Shows a confirmation panel with the total number of organizations found.</item>
/// <item>Handles cases where no organizations exist or no organizations match the given provider.</item>
/// </list>
/// </remarks>
[AutoRegister(ServiceLifetime.Transient, typeof(OrgListCommandHandler))]
internal sealed class OrgListCommandHandler(
    IProfileStore<OrganizationProfile> store,
    IEnumerable<IAuthProvider> providers) : ICliHandler
{
    /// <summary>
    /// Executes 'org list' command asynchronously.
    /// </summary>
    /// <param name="parseResult">Parsed CLI arguments.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task HandleAsync(ParseResult parseResult, CancellationToken ct)
    {
        var provider = parseResult.GetValue<string>("--provider");

        if (string.IsNullOrWhiteSpace(provider))
            provider = ProviderPrompt.SelectProvider(providers);

        if (string.IsNullOrWhiteSpace(provider))
            return;

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Loading organizations...", async _ =>
                {
                    var filtered = await GetOrganizationsAsync(provider, ct);

                    if (!filtered.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No organizations found.[/]");
                        return;
                    }

                    DisplayOrganizations(filtered);
                    DisplayConfirmation(filtered.Count, provider);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to list organizations: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Gets organizations matching provider.
    /// </summary>
    private async Task<List<OrganizationProfile>> GetOrganizationsAsync(
        string provider,
        CancellationToken ct) => [.. (await store.GetAllAsync(ct)).Where(o => o.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))];

    /// <summary>
    /// Displays organizations table.
    /// </summary>
    private static void DisplayOrganizations(IEnumerable<OrganizationProfile> organizations)
    {
        var table = new Table { Border = TableBorder.Rounded };
        table.AddColumn("Name");
        table.AddColumn("Provider");

        foreach (var org in organizations)
            table.AddRow(org.Name, org.Provider);

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Displays a confirmation panel with the number of organizations found for a given provider.
    /// </summary>
    /// <param name="count">Number of organizations.</param>
    /// <param name="provider">Provider filter used for the query.</param>
    private static void DisplayConfirmation(int count, string provider)
    {
        var panel = new Panel($"Found [bold]{count}[/] organization(s) for provider: [green]{provider}[/]")
            .Header("[blue]Organizations Loaded[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Color.Blue)
            .Padding(new Padding(1));

        AnsiConsole.Write(panel);
    }
}