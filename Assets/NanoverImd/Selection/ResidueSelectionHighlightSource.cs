using System;
using System.Linq;
using Nanover.Visualisation.Properties;
using Nanover.Visualisation.Property;
using UnityEngine;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Publishes the particles belonging to the active residue selection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResidueSelectionWorkspaceController))]
    public sealed class ResidueSelectionHighlightSource : MonoBehaviour
    {
        private readonly IntArrayProperty selectedParticles =
            new IntArrayProperty();

        private ResidueSelectionWorkspaceController workspaceController;
        private ResidueSelectionWorkspace workspace;
        private ResidueSelection activeSelection;

        /// <summary>
        /// The particles belonging to the active residue selection.
        /// </summary>
        public IReadOnlyProperty<int[]> SelectedParticles
            => selectedParticles;

        private void Awake()
        {
            workspaceController =
                GetComponent<ResidueSelectionWorkspaceController>();
        }

        private void OnEnable()
        {
            workspaceController.WorkspaceChanged += OnWorkspaceChanged;
            BindWorkspace(workspaceController.Workspace);
        }

        private void OnDisable()
        {
            if (workspaceController != null)
                workspaceController.WorkspaceChanged -= OnWorkspaceChanged;

            BindWorkspace(null);
        }

        private void OnWorkspaceChanged(
            ResidueSelectionWorkspace nextWorkspace)
        {
            BindWorkspace(nextWorkspace);
        }

        private void BindWorkspace(
            ResidueSelectionWorkspace nextWorkspace)
        {
            if (ReferenceEquals(workspace, nextWorkspace))
            {
                BindActiveSelection(nextWorkspace?.ActiveSelection);
                return;
            }

            BindActiveSelection(null);

            if (workspace != null)
                workspace.ActiveSelectionChanged -= OnActiveSelectionChanged;

            workspace = nextWorkspace;

            if (workspace != null)
                workspace.ActiveSelectionChanged += OnActiveSelectionChanged;

            BindActiveSelection(workspace?.ActiveSelection);
        }

        private void OnActiveSelectionChanged(
            ResidueSelection nextSelection)
        {
            BindActiveSelection(nextSelection);
        }

        private void BindActiveSelection(
            ResidueSelection nextSelection)
        {
            if (ReferenceEquals(activeSelection, nextSelection))
            {
                RefreshSelectedParticles();
                return;
            }

            if (activeSelection != null)
                activeSelection.Changed -= OnSelectionChanged;

            activeSelection = nextSelection;

            if (activeSelection != null)
                activeSelection.Changed += OnSelectionChanged;

            RefreshSelectedParticles();
        }

        private void OnSelectionChanged(ResidueSelection selection)
        {
            RefreshSelectedParticles();
        }

        private void RefreshSelectedParticles()
        {
            if (activeSelection == null
                || ReferenceEquals(activeSelection,
                                   workspace?.ProteinSelection))
            {
                selectedParticles.Value = Array.Empty<int>();
                return;
            }

            selectedParticles.Value = activeSelection.Sequence
                                                       .GetParticleIndicesForResidues(
                                                           activeSelection.SelectedResidueIndices)
                                                       .ToArray();
        }
    }
}
