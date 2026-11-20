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

        PeelStripNode GetNode(int index);

        /// <summary>Removes all nodes.</summary>
        void Clear();
    }
}
