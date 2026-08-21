using Nanover.Frontend.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace NanoverImd.UI
{
    /// <summary>
    /// Displays residue-selection state and forwards panel button presses to
    /// the residue-selection panel controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResidueSelectionPanelView : MonoBehaviour
    {
        [Header("State")]
        [SerializeField]
        private ResidueSelectionPanelController controller;

        [Header("Text")]
        [SerializeField]
        private TMP_Text activeSelectionName;

        [SerializeField]
        private TMP_Text selectionSummary;

        [Header("Actions")]
        [SerializeField]
        private UiButton selectionModeButton;

        [SerializeField]
        private UiButton createButton;

        [SerializeField]
        private UiButton deleteButton;

        [SerializeField]
        private UiButton clearButton;

        private void Awake()
        {
            Assert.IsNotNull(controller);
            Assert.IsNotNull(activeSelectionName);
            Assert.IsNotNull(selectionSummary);
            Assert.IsNotNull(selectionModeButton);
            Assert.IsNotNull(createButton);
            Assert.IsNotNull(deleteButton);
            Assert.IsNotNull(clearButton);

            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            selectionModeButton.OnClick += OnSelectionModeClicked;
            createButton.OnClick += OnCreateClicked;
            deleteButton.OnClick += OnDeleteClicked;
            clearButton.OnClick += OnClearClicked;
        }

        private void OnEnable()
        {
            if (controller == null)
                return;

            controller.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
                controller.StateChanged -= Refresh;
        }

        private bool HasRequiredReferences()
        {
            return controller != null
                   && activeSelectionName != null
                   && selectionSummary != null
                   && selectionModeButton != null
                   && createButton != null
                   && deleteButton != null
                   && clearButton != null;
        }

        private void OnSelectionModeClicked()
        {
            controller.ToggleSelectionMode();
        }

        private void OnCreateClicked()
        {
            controller.CreateSelection();
        }

        private void OnDeleteClicked()
        {
            controller.DeleteActiveSelection();
        }

        private void OnClearClicked()
        {
            controller.ClearActiveSelection();
        }

        private void Refresh()
        {
            var workspace = controller.Workspace;
            var selection = controller.ActiveSelection;
            var hasWorkspace = workspace != null;

            selectionModeButton.gameObject.SetActive(hasWorkspace);
            createButton.gameObject.SetActive(hasWorkspace);

            if (!hasWorkspace || selection == null)
            {
                activeSelectionName.text = "No simulation";
                selectionSummary.text = string.Empty;
                deleteButton.gameObject.SetActive(false);
                clearButton.gameObject.SetActive(false);
                return;
            }

            activeSelectionName.text = selection.Name;
            selectionSummary.text = $"{selection.Count} residues selected";
            selectionModeButton.Text = controller.SelectionModeEnabled
                                           ? "Selection mode: On"
                                           : "Selection mode: Off";

            var canModifySelection = !selection.IsReadOnly;
            deleteButton.gameObject.SetActive(canModifySelection);
            clearButton.gameObject.SetActive(canModifySelection);
        }
    }
}
