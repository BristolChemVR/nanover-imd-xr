using System;
using System.Collections.Generic;

namespace NanoverImd.Selection
{
    /// <summary>
    /// An immutable copy of one selection's identity and selected indices.
    /// Indices refer to the simulation topology at capture time.
    /// </summary>
    public sealed class ResidueSelectionSnapshot
    {
        /// <summary>
        /// The identifier of the selection from which this snapshot was captured.
        /// </summary>
        public Guid SelectionId { get; }

        /// <summary>
        /// The display name of the selection at capture time.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Selected frame residue indices, in ascending order.
        /// These are topology indices, not PDB residue numbers.
        /// </summary>
        public IReadOnlyList<int> ResidueIndices { get; }

        /// <summary>
        /// Distinct particle indices belonging to the selected residues,
        /// in ascending order.
        /// </summary>
        public IReadOnlyList<int> ParticleIndices { get; }

        private ResidueSelectionSnapshot(Guid selectionId,
                                         string name,
                                         IEnumerable<int> residueIndices,
                                         IEnumerable<int> particleIndices)
        {
            SelectionId = selectionId;
            Name = name;
            ResidueIndices = new List<int>(residueIndices).AsReadOnly();
            ParticleIndices = new List<int>(particleIndices).AsReadOnly();
        }

        /// <summary>
        /// Copies a selection without retaining references to the mutable
        /// selection or its shared protein sequence. Capture on the same thread
        /// that modifies the selection.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the selection is null.
        /// </exception>
        public static ResidueSelectionSnapshot Capture(
            ResidueSelection selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            var residueIndices = selection.SelectedResidueIndices;
            var particleIndices = selection.Sequence
                                           .GetParticleIndicesForResidues(
                                               residueIndices);

            return new ResidueSelectionSnapshot(selection.Id,
                                                selection.Name,
                                                residueIndices,
                                                particleIndices);
        }
    }
}
