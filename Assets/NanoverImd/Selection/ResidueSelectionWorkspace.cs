using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Owns the residue selections associated with one protein sequence.
    /// A separate workspace should be created for each simulation.
    /// </summary>
    public sealed class ResidueSelectionWorkspace
    {
        private readonly List<ResidueSelection> selections =
            new List<ResidueSelection>();
        private readonly IReadOnlyList<ResidueSelection> readOnlySelections;
        private int nextSelectionNumber = 1;

        /// <summary>
        /// The shared protein sequence used by every selection in this
        /// workspace.
        /// </summary>
        public ProteinSequence Sequence { get; }

        /// <summary>
        /// The protected default selection, initially containing every protein
        /// residue.
        /// </summary>
        public ResidueSelection ProteinSelection { get; }

        /// <summary>
        /// The selection that currently receives residue-selection input.
        /// </summary>
        public ResidueSelection ActiveSelection { get; private set; }

        /// <summary>
        /// All selections in this workspace, including the default protein
        /// selection.
        /// </summary>
        public IReadOnlyList<ResidueSelection> Selections => readOnlySelections;

        /// <summary>
        /// Raised after a selection is added to the workspace.
        /// </summary>
        public event Action<ResidueSelection> SelectionAdded;

        /// <summary>
        /// Raised after a selection is removed from the workspace.
        /// </summary>
        public event Action<ResidueSelection> SelectionRemoved;

        /// <summary>
        /// Raised after the active selection changes.
        /// </summary>
        public event Action<ResidueSelection> ActiveSelectionChanged;

        public ResidueSelectionWorkspace(ProteinSequence sequence)
        {
            Sequence = sequence
                       ?? throw new ArgumentNullException(nameof(sequence));
            readOnlySelections =
                new ReadOnlyCollection<ResidueSelection>(selections);

            ProteinSelection = new ResidueSelection("protein", Sequence);
            ProteinSelection.SelectAll();
            selections.Add(ProteinSelection);
            ActiveSelection = ProteinSelection;
        }

        /// <summary>
        /// Creates an empty, sequentially named selection and makes it active.
        /// Deleted selection numbers are not reused.
        /// </summary>
        public ResidueSelection CreateSelection()
        {
            var selection = new ResidueSelection(
                $"sel {nextSelectionNumber}",
                Sequence);
            nextSelectionNumber++;

            selections.Add(selection);
            ActiveSelection = selection;

            SelectionAdded?.Invoke(selection);
            ActiveSelectionChanged?.Invoke(selection);
            return selection;
        }

        /// <summary>
        /// Makes one of this workspace's selections active.
        /// </summary>
        public void SetActiveSelection(ResidueSelection selection)
        {
            ValidateOwnedSelection(selection);

            if (ReferenceEquals(ActiveSelection, selection))
                return;

            ActiveSelection = selection;
            ActiveSelectionChanged?.Invoke(selection);
        }

        /// <summary>
        /// Deletes a custom selection. The default protein selection is
        /// protected and cannot be deleted.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the selection was deleted; <c>false</c> when the
        /// protected protein selection was supplied.
        /// </returns>
        public bool DeleteSelection(ResidueSelection selection)
        {
            ValidateOwnedSelection(selection);

            if (ReferenceEquals(selection, ProteinSelection))
                return false;

            var wasActive = ReferenceEquals(selection, ActiveSelection);
            selections.Remove(selection);

            if (wasActive)
                ActiveSelection = ProteinSelection;

            SelectionRemoved?.Invoke(selection);
            if (wasActive)
                ActiveSelectionChanged?.Invoke(ProteinSelection);

            return true;
        }

        private void ValidateOwnedSelection(ResidueSelection selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            if (!selections.Contains(selection))
                throw new ArgumentException(
                    "The selection does not belong to this workspace.",
                    nameof(selection));
        }
    }
}
