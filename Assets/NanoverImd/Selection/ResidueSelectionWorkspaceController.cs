using System;
using Nanover.Frame;
using Nanover.Frame.Event;
using Nanover.Visualisation;
using UnityEngine;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Maintains the residue-selection workspace associated with one
    /// <see cref="NanoverImdSimulation"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NanoverImdSimulation))]
    public sealed class ResidueSelectionWorkspaceController : MonoBehaviour
    {
        private NanoverImdSimulation simulation;
        private SynchronisedFrameSource frameSource;

        /// <summary>
        /// The current workspace, or null while no protein topology is
        /// available.
        /// </summary>
        public ResidueSelectionWorkspace Workspace { get; private set; }

        /// <summary>
        /// Raised when a workspace is created, replaced, or cleared.
        /// </summary>
        public event Action<ResidueSelectionWorkspace> WorkspaceChanged;

        private void Start()
        {
            simulation = GetComponent<NanoverImdSimulation>();
            frameSource = simulation.FrameSynchronizer;

            if (frameSource == null)
            {
                Debug.LogError(
                    "The simulation has no synchronised frame source.",
                    this);
                enabled = false;
                return;
            }

            frameSource.FrameChanged += OnFrameChanged;
            simulation.SessionClosed += OnSessionClosed;

            TryUpdateWorkspace(frameSource.CurrentFrame);
        }

        private void OnDestroy()
        {
            if (frameSource != null)
                frameSource.FrameChanged -= OnFrameChanged;

            if (simulation != null)
                simulation.SessionClosed -= OnSessionClosed;
        }

        private void OnFrameChanged(IFrame frame, FrameChanges changes)
        {
            if (!HasTopologyChanges(changes))
                return;

            TryUpdateWorkspace(frame as Frame);
        }

        private void TryUpdateWorkspace(Frame frame)
        {
            if (frame?.ResidueNames == null || frame.ResidueNames.Length == 0)
                return;

            var sequence = ProteinSequenceBuilder.Build(frame);
            if (Workspace != null
                && HasSameTopology(Workspace.Sequence, sequence))
                return;

            SetWorkspace(new ResidueSelectionWorkspace(sequence));
        }

        private void OnSessionClosed()
        {
            SetWorkspace(null);
        }

        private void SetWorkspace(ResidueSelectionWorkspace workspace)
        {
            if (ReferenceEquals(Workspace, workspace))
                return;

            Workspace = workspace;
            WorkspaceChanged?.Invoke(workspace);
        }

        private static bool HasTopologyChanges(FrameChanges changes)
        {
            return changes.HasChanged(StandardFrameProperties.ResidueNames.Key)
                   || changes.HasChanged(
                       StandardFrameProperties.ResidueEntities.Key)
                   || changes.HasChanged(
                       StandardFrameProperties.ParticleResidues.Key)
                   || changes.HasChanged(
                       StandardFrameProperties.ParticleNames.Key);
        }

        private static bool HasSameTopology(ProteinSequence first,
                                            ProteinSequence second)
        {
            if (ReferenceEquals(first, second))
                return true;
            if (first == null || second == null || first.Count != second.Count)
                return false;

            for (var i = 0; i < first.Count; i++)
            {
                var firstResidue = first.Residues[i];
                var secondResidue = second.Residues[i];

                if (firstResidue.ResidueIndex != secondResidue.ResidueIndex
                    || firstResidue.EntityIndex != secondResidue.EntityIndex
                    || !string.Equals(firstResidue.ResidueName,
                                      secondResidue.ResidueName,
                                      StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}
