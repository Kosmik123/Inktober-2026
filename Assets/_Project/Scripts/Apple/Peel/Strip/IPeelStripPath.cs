namespace Inktober2026.Apple.Peel
{
    /// <summary>
    /// Strategy that decides where the strip is in space. The width of the strip comes from <see cref="PeelWidthProfile"/>,
    /// the path only places every section: on a straight line for a preview, later in a physics simulation of a hanging strip.
    /// The nodes are in the local space of the strip object, one node for every section in the same order.
    /// </summary>
    public interface IPeelStripPath
    {
        /// <summary>True when nodes can move after they are created (a simulation), so the mesh has to be refreshed every frame.</summary>
        bool IsDynamic { get; }

        /// <summary>Number of nodes, the same as the number of sections appended.</summary>
        int NodeCount { get; }

        /// <summary>A new section was cut off at the end of the strip. Creates its node.</summary>
        void Append(PeelSection section);

        /// <summary>
        /// Prepares the nodes to be read with <see cref="GetNode"/> for a mesh that is about to be built, with or without the section
        /// still growing at the knife. That section is not added.
        /// </summary>
        /// <param name="hasLive">True when there is a growing section.</param>
        /// <param name="live">The growing section, the newest end of the strip when there is one.</param>
        /// <param name="liveNode">The node of the growing section.</param>
        /// <returns>How the nodes are brought to the frame the strip is drawn in, with the newest end at the origin.</returns>
        PeelStripView Lay(bool hasLive, PeelSection live, out PeelStripNode liveNode);

        PeelStripNode GetNode(int index);

        /// <summary>Removes all nodes.</summary>
        void Clear();
    }
}
