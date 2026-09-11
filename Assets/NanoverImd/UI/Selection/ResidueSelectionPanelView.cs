using System.Collections.Generic;
using System.Text;
using Nanover.Frontend.UI;
using NanoverImd.Selection;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

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

        [SerializeField]
        private TMP_Text sequenceLabel;

        [SerializeField]
        private RectTransform sequenceContent;

        [SerializeField]
        [Min(8)]
        private int residuesPerLine = 30;

        [SerializeField]
        [Min(1f)]
        private float sequenceLineHeight = 24f;

        [SerializeField]
        private Color selectedResidueText =
            new Color(1f, 0.3f, 0.68f, 1f);

        [SerializeField]
        private Color unselectedResidueText =
            new Color(0.62f, 0.67f, 0.75f, 1f);

        [Header("Selection List")]
        [SerializeField]
        private Transform selectionListContent;

        [SerializeField]
        private ResidueSelectionListItemView selectionItemPrefab;

        [Header("Actions")]
        [SerializeField]
        private UiButton selectionModeButton;

        [SerializeField]
        private UiButton createButton;

        [SerializeField]
        private UiButton deleteButton;

        [SerializeField]
        private UiButton clearButton;

        [Header("Manual Server Send")]
        [SerializeField]
        private UiButton syncButton;

        [SerializeField]
        private TMP_Text syncStatusLabel;

        private Button syncButtonControl;

        private readonly List<ResidueSelectionListItemView> selectionItems =
            new List<ResidueSelectionListItemView>();

        private ResidueSelectionWorkspace displayedWorkspace;

        private void Awake()
        {
            Assert.IsNotNull(controller);
            Assert.IsNotNull(activeSelectionName);
            Assert.IsNotNull(selectionSummary);
            Assert.IsNotNull(sequenceLabel);
            Assert.IsNotNull(sequenceContent);
            Assert.IsNotNull(selectionListContent);
            Assert.IsNotNull(selectionItemPrefab);
            Assert.IsNotNull(selectionModeButton);
            Assert.IsNotNull(createButton);
            Assert.IsNotNull(deleteButton);
            Assert.IsNotNull(clearButton);

            if (!HasRequiredReferences())
            {
                enabled = false;
                return;
            }

            selectionModeButton.Image = null;
            selectionModeButton.OnClick += OnSelectionModeClicked;
            createButton.OnClick += OnCreateClicked;
            deleteButton.OnClick += OnDeleteClicked;
            clearButton.OnClick += OnClearClicked;

            // Optional so older panel instances keep their local selection UI.
            if (syncButton != null)
            {
                syncButton.Image = null;
                syncButtonControl = syncButton.GetComponent<Button>();
                if (syncButtonControl != null)
                    syncButtonControl.onClick.AddListener(OnSyncClicked);
            }

            if (syncStatusLabel != null)
            {
                syncStatusLabel.richText = false;
                syncStatusLabel.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void OnDestroy()
        {
            if (syncButtonControl != null)
                syncButtonControl.onClick.RemoveListener(OnSyncClicked);
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
                   && sequenceLabel != null
                   && sequenceContent != null
                   && selectionListContent != null
                   && selectionItemPrefab != null
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

        private void OnSyncClicked()
        {
            controller.SendSelections();
        }

        private void RefreshSyncState()
        {
            if (syncButton != null)
                syncButton.Text = controller.IsSendingSelections
                    ? "Sending..."
                    : "Send selections";

            if (syncButtonControl != null)
                syncButtonControl.interactable = controller.CanSyncSelections;

            if (syncStatusLabel != null)
                syncStatusLabel.text = controller.SelectionSyncStatusText;
        }

        private void OnSelectionItemClicked(ResidueSelection selection)
        {
            controller.SetActiveSelection(selection);
        }

        private void Refresh()
        {
            RefreshSyncState();
            var workspace = controller.Workspace;
            var selection = controller.ActiveSelection;
            var hasWorkspace = workspace != null;

            EnsureSelectionItems(workspace);
            foreach (var item in selectionItems)
                item.Refresh(ReferenceEquals(item.Selection, selection));

            selectionModeButton.gameObject.SetActive(hasWorkspace);
            createButton.gameObject.SetActive(hasWorkspace);

            if (!hasWorkspace || selection == null)
            {
                activeSelectionName.text = "No simulation";
                selectionSummary.text = string.Empty;
                sequenceLabel.text = "No protein sequence";
                SetSequenceHeight(1);
                deleteButton.gameObject.SetActive(false);
                clearButton.gameObject.SetActive(false);
                return;
            }

            activeSelectionName.text = selection.Name;
            selectionSummary.text = $"{selection.Count} residues selected";
            sequenceLabel.text = BuildSequenceText(selection,
                                                   out var lineCount);
            SetSequenceHeight(lineCount);
            selectionModeButton.Text = controller.SelectionModeEnabled
                                           ? "<size=28>■</size> Selection mode: On"
                                           : "<size=28>□</size> Selection mode: Off";

            var canModifySelection = !selection.IsReadOnly;
            deleteButton.gameObject.SetActive(canModifySelection);
            clearButton.gameObject.SetActive(canModifySelection);
        }

        private void EnsureSelectionItems(
            ResidueSelectionWorkspace workspace)
        {
            if (ReferenceEquals(displayedWorkspace, workspace)
                && ItemsMatch(workspace))
                return;

            ClearSelectionItems();
            displayedWorkspace = workspace;

            if (workspace == null)
                return;

            foreach (var selection in workspace.Selections)
            {
                var item = Instantiate(selectionItemPrefab,
                                       selectionListContent);
                item.gameObject.SetActive(true);
                item.Bind(selection, OnSelectionItemClicked);
                selectionItems.Add(item);
            }
        }

        private bool ItemsMatch(ResidueSelectionWorkspace workspace)
        {
            if (workspace == null)
                return selectionItems.Count == 0;

            if (selectionItems.Count != workspace.Selections.Count)
                return false;

            for (var i = 0; i < selectionItems.Count; i++)
            {
                if (!ReferenceEquals(selectionItems[i].Selection,
                                     workspace.Selections[i]))
                    return false;
            }

            return true;
        }

        private void ClearSelectionItems()
        {
            foreach (var item in selectionItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            selectionItems.Clear();
        }

        private string BuildSequenceText(ResidueSelection selection,
                                         out int lineCount)
        {
            var residues = selection.Sequence.Residues;
            if (residues.Count == 0)
            {
                lineCount = 1;
                return "No protein sequence";
            }

            var selectedColor = ColorUtility.ToHtmlStringRGB(
                selectedResidueText);
            var unselectedColor = ColorUtility.ToHtmlStringRGB(
                unselectedResidueText);
            var builder = new StringBuilder(residues.Count * 2);
            builder.Append("<color=#").Append(unselectedColor).Append('>');

            lineCount = 1;
            var residuesOnLine = 0;
            var currentEntity = residues[0].EntityIndex;

            for (var i = 0; i < residues.Count; i++)
            {
                var residue = residues[i];
                var startsNewEntity = i > 0
                                      && residue.EntityIndex != currentEntity;
                if (startsNewEntity || residuesOnLine >= residuesPerLine)
                {
                    builder.Append('\n');
                    lineCount++;
                    residuesOnLine = 0;
                    currentEntity = residue.EntityIndex;
                }

                if (selection.Contains(residue.ResidueIndex))
                {
                    builder.Append("</color><color=#")
                           .Append(selectedColor)
                           .Append("><b>")
                           .Append(residue.SequenceCode)
                           .Append("</b></color><color=#")
                           .Append(unselectedColor)
                           .Append('>');
                }
                else
                {
                    builder.Append(residue.SequenceCode);
                }

                residuesOnLine++;
            }

            builder.Append("</color>");
            return builder.ToString();
        }

        private void SetSequenceHeight(int lineCount)
        {
            sequenceContent.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(1, lineCount) * sequenceLineHeight + 16f);
        }
    }
}
