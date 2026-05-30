using System.CommandLine;
using ChangeTrace.Cli.Interfaces;
using ChangeTrace.Configuration.Discovery;
using ChangeTrace.CredentialTrace.Interfaces;
using ChangeTrace.CredentialTrace.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ChangeTrace.Cli.Handlers.Profiles.Organizations;

[AutoRegister(ServiceLifetime.Transient, typeof(OrgRemoveCommandHandler))]
internal sealed class OrgRemoveCommandHandler(
    IProfileStore<OrganizationProfile> orgStore,
    IWorkspaceStore workspaceStore,
    IWorkspaceContext workspaceContext) : ICliHandler
{
    public async Task HandleAsync(ParseResult parseResult, CancellationToken ct)
    {
        var name = parseResult.GetValue<string>("name");
        var assumeYes = parseResult.GetValue<bool>("--yes");

        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Organization name is required.");
            return;
        }

        var org = await orgStore.GetByNameAsync(name, ct);
        if (org == null)
        {
            AnsiConsole.MarkupLine($"[red]Organization '{name}' not found.[/]");
            return;
        }

        var workspaces = (await workspaceStore.GetByNameOrganization(org.Name, ct)).ToList();
        if (workspaces.Any(w => workspaceContext.Current?.Id == w.Id))
        {
            AnsiConsole.MarkupLine("[red]Cannot remove an organization that contains the active workspace. Select another workspace first.[/]");
            return;
        }

        var workspaceSummary = workspaces.Count == 0
            ? "no workspaces"
            : $"{workspaces.Count} workspace(s)";

        if (!assumeYes && !AnsiConsole.Confirm($"Remove organization [yellow]{org.Name}[/] and {workspaceSummary}?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return;
        }

        foreach (var workspace in workspaces)
            await workspaceStore.DeleteAsync(workspace.Id, ct);

        await orgStore.DeleteAsync(org.Id, ct);

        AnsiConsole.MarkupLine($"[green]Removed organization '{org.Name}' and {workspaces.Count} workspace(s).[/]");
    }
}
