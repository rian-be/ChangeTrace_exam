using ChangeTrace.Configuration.Discovery;
using ChangeTrace.Rendering.Enums;
using ChangeTrace.Rendering.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChangeTrace.Rendering.Snapshots;

/// <summary>
/// Represents snapshot of scenes at specific moments in virtual time.
/// </summary>
/// <remarks>
/// Holds immutable lists of nodes, avatars, edges, and particles.
/// Provides helper methods for filtering, spatial queries, and basic statistics.
/// </remarks>
[AutoRegister(ServiceLifetime.Singleton)]
internal sealed class SceneSnapshot : ISceneSnapshot
{
    private readonly Dictionary<string, NodeSnapshot> _nodeIndex;

    internal SceneSnapshot(
        IReadOnlyList<NodeSnapshot> nodes,
        IReadOnlyList<AvatarSnapshot> avatars,
        IReadOnlyList<EdgeSnapshot> edges,
        IReadOnlyList<ParticleSnapshot> particles)
    {
        Nodes = nodes;
        Avatars = avatars;
        Edges = edges;
        Particles = particles;
        
        // Prebuild index for O(1) lookup
        _nodeIndex = new Dictionary<string, NodeSnapshot>(nodes.Count);
        foreach (var n in nodes) _nodeIndex[n.Id] = n;
    }

    /// <summary>
    /// An empty scene snapshot with no nodes, avatars, edges, or particles.
    /// </summary>
    internal static SceneSnapshot Empty { get; } = new([], [], [], []);

    public IReadOnlyList<NodeSnapshot> Nodes { get; }

    public IReadOnlyList<AvatarSnapshot> Avatars { get; }

    public IReadOnlyList<EdgeSnapshot> Edges { get; }

    public IReadOnlyList<ParticleSnapshot> Particles { get; }

    public int NodeCount => Nodes.Count;
    public int AvatarCount => Avatars.Count;
    public int EdgeCount => Edges.Count;
    public int ParticleCount => Particles.Count;
    public int TotalObjects => NodeCount + AvatarCount + EdgeCount + ParticleCount;

    public bool IsEmpty => TotalObjects == 0;
    public bool HasParticles => Particles.Count > 0;

    /// <summary>
    /// Finds node by its identifier.
    /// </summary>
    /// <param name="id">The node's unique identifier.</param>
    /// <returns>The <see cref="NodeSnapshot"/> if found; otherwise, null.</returns>
    public NodeSnapshot? FindNode(string id)
    {
        return _nodeIndex.GetValueOrDefault(id);
    }

    /// <summary>
    /// Returns nodes of a given kind.
    /// </summary>
    /// <param name="kind">The kind of nodes to filter.</param>
    /// <returns>An enumerable of <see cref="NodeSnapshot"/> objects.</returns>
    public IEnumerable<NodeSnapshot> NodesOfKind(NodeKind kind)
        => Nodes.Where(n => n.Kind == kind);

    /// <summary>
    /// Returns nodes with glows above thresholds.
    /// </summary>
    /// <param name="threshold">Glow threshold.</param>
    /// <returns>An enumerable of <see cref="NodeSnapshot"/> objects.</returns>
    public IEnumerable<NodeSnapshot> GlowingNodes(float threshold = 0.05f)
        => Nodes.Where(n => n.Glow > threshold);

    /// <summary>
    /// Returns avatars with activity above thresholds.
    /// </summary>
    /// <param name="activityThreshold">Activity threshold.</param>
    /// <returns>An enumerable of <see cref="AvatarSnapshot"/> objects.</returns>
    public IEnumerable<AvatarSnapshot> ActiveAvatars(float activityThreshold = 0.1f)
        => Avatars.Where(a => a.ActivityLevel > activityThreshold);

    /// <summary>
    /// Returns avatars with alpha above thresholds.
    /// </summary>
    /// <param name="alphaThreshold">Alpha threshold.</param>
    /// <returns>An enumerable of <see cref="AvatarSnapshot"/> objects.</returns>
    public IEnumerable<AvatarSnapshot> VisibleAvatars(float alphaThreshold = 0.05f)
        => Avatars.Where(a => a.Alpha * a.ActivityLevel > alphaThreshold);

    /// <summary>
    /// Finds an avatar by actor name.
    /// </summary>
    /// <param name="actor">The actor's name.</param>
    /// <returns>The <see cref="AvatarSnapshot"/> if found; otherwise, null.</returns>
    public AvatarSnapshot? FindAvatar(string actor)
        => Avatars.FirstOrDefault(a => a.Actor == actor);

    /// <summary>
    /// Returns edges starting from a given node.
    /// </summary>
    /// <param name="nodeId">The source node identifier.</param>
    /// <returns>An enumerable of <see cref="EdgeSnapshot"/> objects.</returns>
    public IEnumerable<EdgeSnapshot> EdgesFrom(string nodeId)
        => Edges.Where(e => e.FromId == nodeId);

    /// <summary>
    /// Returns edges ending at given node.
    /// </summary>
    /// <param name="nodeId">The target node identifier.</param>
    /// <returns>An enumerable of <see cref="EdgeSnapshot"/> objects.</returns>
    public IEnumerable<EdgeSnapshot> EdgesTo(string nodeId)
        => Edges.Where(e => e.ToId == nodeId);

    /// <summary>
    /// Returns edges of a specified kind.
    /// </summary>
    /// <param name="kind">The kind of edge.</param>
    /// <returns>An enumerable of <see cref="EdgeSnapshot"/> objects.</returns>
    public IEnumerable<EdgeSnapshot> EdgesOfKind(EdgeKind kind)
        => Edges.Where(e => e.Kind == kind);

    /// <summary>
    /// Returns edges with alpha above threshold.
    /// </summary>
    /// <param name="alphaThreshold">Alpha threshold.</param>
    /// <returns>An enumerable of <see cref="EdgeSnapshot"/> objects.</returns>
    public IEnumerable<EdgeSnapshot> VisibleEdges(float alphaThreshold = 0.02f)
        => Edges.Where(e => e.Alpha > alphaThreshold);
    

    /// <summary>
    ///  Geometric center of all nodes, or null if none exist.
    /// </summary>
    /// <returns>The center position as <see cref="Vec2"/>; or null.</returns>
    public Vec2? NodesCenter()
    {
        if (Nodes.Count == 0) return null;
        var sum = Vec2.Zero;
        foreach (var n in Nodes) sum += n.Position;
        return sum / Nodes.Count;
    }

    /// <summary>
    /// Finds the node closest to the given point.
    /// </summary>
    /// <param name="point">The reference point in world coordinates.</param>
    /// <returns>The closest <see cref="NodeSnapshot"/>; or null if no nodes exist.</returns>
    public NodeSnapshot? ClosestNode(Vec2 point)
    {
        if (Nodes.Count == 0) return null;
        NodeSnapshot? best = null;
        float bestDist = float.MaxValue;

        foreach (var n in Nodes)
        {
            float dist = (n.Position - point).LengthSq;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = n;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns basic statistics about scenes.
    /// </summary>
    /// <returns>A <see cref="SceneStats"/> object.</returns>
    public SceneStats ComputeStats() => new(
        NodeCount: NodeCount,
        AvatarCount: AvatarCount,
        EdgeCount: EdgeCount,
        ParticleCount: ParticleCount,
        ActiveAvatars: Avatars.Count(a => a.ActivityLevel > 0.1f),
        GlowingNodes: Nodes.Count(n => n.Glow > 0.05f),
        VisibleEdges: Edges.Count(e => e.Alpha > 0.02f)
    );
}