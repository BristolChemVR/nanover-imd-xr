using System;
using System.Collections.Generic;
using CommandArguments = System.Collections.Generic.Dictionary<string, object>;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Converts immutable selection snapshots into plain command arguments.
    /// Produces the payload shape required by the client-defined selection
    /// command contract.
    /// This class neither sends commands nor defines server-side sync behaviour.
    /// </summary>
    public static class ResidueSelectionCommandArguments
    {
        /// <summary>
        /// Builds a fresh argument dictionary containing every selection in
        /// the snapshot. The caller may modify the result without changing the
        /// snapshot or arguments produced by another call.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the snapshot is null.
        /// </exception>
        public static CommandArguments FromWorkspaceSnapshot(
            ResidueSelectionWorkspaceSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var selections = new object[snapshot.Selections.Count];
            for (var i = 0; i < selections.Length; i++)
                selections[i] = FromSelectionSnapshot(snapshot.Selections[i]);

            return new CommandArguments
            {
                { "selections", selections }
            };
        }

        private static CommandArguments FromSelectionSnapshot(
            ResidueSelectionSnapshot snapshot)
        {
            return new CommandArguments
            {
                { "selection_id", snapshot.SelectionId.ToString("D") },
                { "name", snapshot.Name },
                { "residue_indices", CopyIndices(snapshot.ResidueIndices) },
                { "particle_indices", CopyIndices(snapshot.ParticleIndices) }
            };
        }

        private static object[] CopyIndices(IReadOnlyList<int> indices)
        {
            // Use the object-array shape already supported by the WebSocket
            // serializer; each element remains an integer, not a string.
            var values = new object[indices.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = indices[i];

            return values;
        }
    }
}
