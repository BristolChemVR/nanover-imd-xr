using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Stores the residues selected from a shared protein sequence.
    /// </summary>
    public sealed class ResidueSelection
    {
        private readonly HashSet<int> selectedResidueIndices =
            new HashSet<int>();

        /// <summary>
        /// A stable identifier for this selection during its lifetime.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The fixed display name of this selection.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The complete protein sequence from which residues may be selected.
        /// </summary>
        public ProteinSequence Sequence { get; }

        /// <summary>
        /// The selected frame residue indices, returned as a sorted read-only
        /// snapshot.
        /// </summary>
        public IReadOnlyList<int> SelectedResidueIndices
        {
            get
            {
                var orderedIndices = selectedResidueIndices.OrderBy(index => index)
                                                          .ToList();
                return new ReadOnlyCollection<int>(orderedIndices);
            }
        }

        /// <summary>
        /// The number of currently selected residues.
        /// </summary>
        public int Count => selectedResidueIndices.Count;

        /// <summary>
        /// Raised after the selected residue set changes.
        /// </summary>
        public event Action<ResidueSelection> Changed;

        public ResidueSelection(string name, ProteinSequence sequence)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A selection name is required.",
                                            nameof(name));

            Sequence = sequence
                       ?? throw new ArgumentNullException(nameof(sequence));
            Id = Guid.NewGuid();
            Name = name.Trim();
        }

        /// <summary>
        /// Returns whether the residue is currently selected.
        /// </summary>
        public bool Contains(int residueIndex)
        {
            return selectedResidueIndices.Contains(residueIndex);
        }

        /// <summary>
        /// Toggles a protein residue and returns its new selected state.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the residue is not part of this selection's protein
        /// sequence.
        /// </exception>
        public bool Toggle(int residueIndex)
        {
            if (!Sequence.TryGetResidue(residueIndex, out _))
                throw new ArgumentOutOfRangeException(
                    nameof(residueIndex),
                    residueIndex,
                    "The residue is not part of the protein sequence.");

            bool isSelected;
            if (selectedResidueIndices.Contains(residueIndex))
            {
                selectedResidueIndices.Remove(residueIndex);
                isSelected = false;
            }
            else
            {
                selectedResidueIndices.Add(residueIndex);
                isSelected = true;
            }

            OnChanged();
            return isSelected;
        }

        /// <summary>
        /// Selects every residue in the protein sequence.
        /// </summary>
        public void SelectAll()
        {
            var changed = false;
            foreach (var residueIndex in Sequence.ResidueIndices)
                changed |= selectedResidueIndices.Add(residueIndex);

            if (changed)
                OnChanged();
        }

        /// <summary>
        /// Removes every residue from this selection.
        /// </summary>
        public void Clear()
        {
            if (selectedResidueIndices.Count == 0)
                return;

            selectedResidueIndices.Clear();
            OnChanged();
        }

        private void OnChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
