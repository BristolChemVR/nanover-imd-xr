using System;
using System.Collections.Generic;

namespace NanoverImd.Selection
{
    /// <summary>
    /// An immutable copy of all selections in a workspace at capture time.
    /// Contains selection data only, without workspace identity or UI state.
    /// </summary>
    public sealed class ResidueSelectionWorkspaceSnapshot
    {
        /// <summary>
        /// Snapshots in workspace order, including the default protein
        /// selection and any empty custom selections.
        /// </summary>
        public IReadOnlyList<ResidueSelectionSnapshot> Selections { get; }

        private ResidueSelectionWorkspaceSnapshot(
            IEnumerable<ResidueSelectionSnapshot> selections)
        {
            Selections = new List<ResidueSelectionSnapshot>(selections)
                .AsReadOnly();
        }

        /// <summary>
        /// Captures every selection without retaining references to the live
        /// workspace or its selections. Call on the same thread that modifies
        /// the workspace and its selections (normally Unity's main thread).
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the workspace is null.
        /// </exception>
        public static ResidueSelectionWorkspaceSnapshot Capture(
            ResidueSelectionWorkspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            var snapshots = new List<ResidueSelectionSnapshot>();
            foreach (var selection in workspace.Selections)
                snapshots.Add(ResidueSelectionSnapshot.Capture(selection));

            return new ResidueSelectionWorkspaceSnapshot(snapshots);
        }
    }
}
