using ChangeTrace.Rendering.Enums;

namespace ChangeTrace.Rendering;

/// <summary>
/// Configuration options controlling which rendering event categories are enabled.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Uses <see cref="RenderEventKinds"/> flags to enable or disable specific event types.</item>
/// <item>Provides convenience properties for checking if a specific event category should be rendered.</item>
/// <item>Defaults to rendering all event kinds.</item>
/// </list>
/// </remarks>
internal sealed class RenderingOptions
{
    /// <summary>
    /// Gets or sets the set of enabled rendering event kinds.
    /// </summary>
    public RenderEventKinds EnabledEvents { get; set; } = RenderEventKinds.All;

    /// <summary>
    /// Gets value indicating whether commit events should be rendered.
    /// </summary>
    public bool RenderCommits => IsEnabled(RenderEventKinds.Commit);

    /// <summary>
    /// Gets value indicating whether branch events should be rendered.
    /// </summary>
    public bool RenderBranches => IsEnabled(RenderEventKinds.Branch);

    /// <summary>
    /// Gets a value indicating whether merge events should be rendered.
    /// </summary>
    public bool RenderMerges => IsEnabled(RenderEventKinds.Merge);

    /// <summary>
    /// Gets a value indicating whether file coupling events should be rendered.
    /// </summary>
    public bool RenderFileCoupling => IsEnabled(RenderEventKinds.FileCoupling);

    /// <summary>
    /// Gets or sets the layout visualization mode.
    /// </summary>
    public LayoutMode Mode { get; set; } = LayoutMode.SingleTree;

    private bool IsEnabled(RenderEventKinds kind) =>
        (EnabledEvents & kind) == kind;
}

/// <summary>
/// Defines the visual structure of the project tree.
/// </summary>
internal enum LayoutMode
{
    /// <summary>
    /// Classic Gource style where everything is connected to a single root.
    /// </summary>
    SingleTree,

    /// <summary>
    /// Independent trees for main folders, better for massive repositories.
    /// </summary>
    Forest
}
