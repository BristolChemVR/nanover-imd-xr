using Nanover.Core.Math;
using Nanover.Frontend.Controllers;
using Nanover.Frontend.Input;
using Nanover.Frontend.Manipulation;
using Nanover.Frontend.XR;
using NanoverImd.Interaction;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Translates XR trigger input into local residue-selection strokes.
    /// </summary>
    public sealed class XRResidueSelectionManager : MonoBehaviour
    {
        [SerializeField]
        private InteractableScene interactableScene;

        [SerializeField]
        private ResidueSelectionWorkspaceController workspaceController;

        [SerializeField]
        private ControllerManager controllerManager;

        [SerializeField]
        private ControllerInputMode targetMode;

        private sealed class HandSelectionInput
        {
            public InputDeviceCharacteristics Characteristics { get; }
            public AttemptableManipulator Manipulator { get; set; }
            public IButton Button { get; set; }
            public bool Armed { get; set; }
            public bool CanStartSelection { get; set; }

            public HandSelectionInput(
                InputDeviceCharacteristics characteristics)
            {
                Characteristics = characteristics;
            }
        }

        private readonly HandSelectionInput leftHand =
            new HandSelectionInput(InputDeviceCharacteristics.Left);
        private readonly HandSelectionInput rightHand =
            new HandSelectionInput(InputDeviceCharacteristics.Right);

        private bool wasTargetMode;

        private void OnEnable()
        {
            Assert.IsNotNull(interactableScene);
            Assert.IsNotNull(workspaceController);
            Assert.IsNotNull(controllerManager);
            Assert.IsNotNull(targetMode);

            controllerManager.LeftController.ControllerReset +=
                SetupLeftManipulator;
            controllerManager.RightController.ControllerReset +=
                SetupRightManipulator;

            SetupLeftManipulator();
            SetupRightManipulator();
        }

        private void OnDisable()
        {
            if (controllerManager != null)
            {
                controllerManager.LeftController.ControllerReset -=
                    SetupLeftManipulator;
                controllerManager.RightController.ControllerReset -=
                    SetupRightManipulator;
            }

            ClearHand(leftHand);
            ClearHand(rightHand);
            wasTargetMode = false;
        }

        private void Update()
        {
            var isTargetMode = controllerManager != null
                               && controllerManager.CurrentInputMode == targetMode;

            if (wasTargetMode && !isTargetMode)
            {
                leftHand.Manipulator?.EndActiveManipulation();
                rightHand.Manipulator?.EndActiveManipulation();
            }

            UpdateHandArming(leftHand, isTargetMode);
            UpdateHandArming(rightHand, isTargetMode);
            wasTargetMode = isTargetMode;
        }

        private void SetupLeftManipulator()
        {
            SetupHand(leftHand, controllerManager.LeftController);
        }

        private void SetupRightManipulator()
        {
            SetupHand(rightHand, controllerManager.RightController);
        }

        private void SetupHand(HandSelectionInput hand,
                               VrController controller)
        {
            ClearHand(hand);

            if (!controller.IsControllerActive)
                return;

            hand.Manipulator = new AttemptableManipulator(
                controller.CursorPose,
                AttemptSelection);
            hand.Button = hand.Characteristics.WrapUsageAsButton(
                CommonUsages.triggerButton,
                () => hand.CanStartSelection);

            hand.Button.Pressed += hand.Manipulator.AttemptManipulation;
            hand.Button.Released += hand.Manipulator.EndActiveManipulation;
        }

        private void ClearHand(HandSelectionInput hand)
        {
            hand.CanStartSelection = false;
            hand.Armed = false;

            if (hand.Manipulator != null)
            {
                hand.Manipulator.EndActiveManipulation();

                if (hand.Button != null)
                {
                    hand.Button.Pressed -=
                        hand.Manipulator.AttemptManipulation;
                    hand.Button.Released -=
                        hand.Manipulator.EndActiveManipulation;
                }
            }

            hand.Button = null;
            hand.Manipulator = null;
        }

        private void UpdateHandArming(HandSelectionInput hand,
                                      bool isTargetMode)
        {
            if (!isTargetMode)
            {
                hand.Armed = false;
                hand.CanStartSelection = false;
                return;
            }

            if (!hand.Armed)
            {
                var triggerPressed = hand.Characteristics
                                         .GetFirstDevice()
                                         .GetButtonPressed(
                                             CommonUsages.triggerButton);
                if (triggerPressed == false)
                    hand.Armed = true;
            }

            hand.CanStartSelection = hand.Armed
                                     && hand.Manipulator != null;
        }

        private IActiveManipulation AttemptSelection(
            UnitScaleTransformation initialPose)
        {
            var selection = workspaceController.Workspace?.ActiveSelection;
            if (selection == null)
                return null;

            return new ActiveResidueSelectionStroke(interactableScene,
                                                    selection,
                                                    initialPose);
        }
    }
}
