using System;
using Nanover.Core.Math;
using Nanover.Frontend.Manipulation;
using NanoverImd.Interaction;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Represents one continuous residue-selection gesture, from trigger press
    /// to trigger release.
    /// </summary>
    public sealed class ActiveResidueSelectionStroke : IActiveManipulation
    {
        private readonly InteractableScene interactableScene;
        private readonly ResidueSelection selection;

        private int? lastVisitedResidueIndex;
        private bool ended;

        /// <inheritdoc cref="IActiveManipulation.ManipulationEnded"/>
        public event Action ManipulationEnded;

        public ActiveResidueSelectionStroke(
            InteractableScene interactableScene,
            ResidueSelection selection,
            UnitScaleTransformation initialPose)
        {
            this.interactableScene = interactableScene
                                     ?? throw new ArgumentNullException(
                                         nameof(interactableScene));
            this.selection = selection
                             ?? throw new ArgumentNullException(
                                 nameof(selection));

            UpdateManipulatorPose(initialPose);
        }

        /// <inheritdoc />
        public void UpdateManipulatorPose(
            UnitScaleTransformation manipulatorPose)
        {
            if (ended)
                return;

            if (!interactableScene.TryGetNearestResidue(manipulatorPose,
                                                        out var result))
            {
                lastVisitedResidueIndex = null;
                return;
            }

            var residueIndex = result.ResidueIndex;
            if (lastVisitedResidueIndex == residueIndex)
                return;

            lastVisitedResidueIndex = residueIndex;

            if (!selection.Sequence.TryGetResidue(residueIndex, out _))
                return;

            selection.Toggle(residueIndex);
        }

        /// <inheritdoc />
        public void EndManipulation()
        {
            if (ended)
                return;

            ended = true;
            lastVisitedResidueIndex = null;
            ManipulationEnded?.Invoke();
        }
    }
}
