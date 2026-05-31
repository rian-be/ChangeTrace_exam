using System.CommandLine;
using ChangeTrace.Cli.Interfaces;
using ChangeTrace.Configuration.Discovery;
using ChangeTrace.CredentialTrace.Interfaces;
using ChangeTrace.CredentialTrace.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace ChangeTrace.Cli.Handlers.Profiles.Workspaces;

[AutoRegister(ServiceLifetime.Transient, typeof(WorkRemoveCommandHandler))]
internal sealed class WorkRemoveCommandHandler(
    IProfileStore<OrganizationProfile> orgStore,
    IWorkspaceStore workspaceStore,
    IWorkspaceContext workspaceContext) : ICliHandler
{
    public async Task HandleAsync(ParseResult parseResult, CancellationToken ct)
        => await HandleAsync(
            parseResult.GetValue<string>("name")!,
            parseResult.GetValue<string>("--org")!,
            parseResult.GetValue<bool>("--yes"),
            ct);

    private async Task HandleAsync(
        string name,
        string orgName,
        bool assumeYes,
        CancellationToken ct)
    {
        var org = await orgStore.GetByNameAsync(orgName, ct);
        if (org is null)
        {
            AnsiConsole.MarkupLine($"[red]Organization '{orgName}' not found.[/]");
            return;
        }

        var workspaces = await workspaceStore.GetByNameOrganization(orgName, ct);
        var workspace = workspaces.FirstOrDefault(w =>
            w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (workspace is null)
        {
            AnsiConsole.MarkupLine($"[red]Workspace '{name}' not found in organization '{orgName}'.[/]");
            return;
        }

        if (workspaceContext.Current?.Id == workspace.Id)
        {
            AnsiConsole.MarkupLine("[red]Cannot remove the active workspace. Select another workspace first.[/]");
            return;
        }

        if (!assumeYes && !AnsiConsole.Confirm($"Remove workspace [yellow]{workspace.Name}[/] from [yellow]{org.Name}[/]?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return;
        }

        await workspaceStore.DeleteAsync(workspace.Id, ct);
        AnsiConsole.MarkupLine($"[green]Removed workspace '{workspace.Name}' from organization '{org.Name}'.[/]");
    }
}
