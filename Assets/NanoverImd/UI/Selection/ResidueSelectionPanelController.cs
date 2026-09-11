using System;
using NanoverImd.Selection;
using UnityEngine;
using UnityEngine.Assertions;

namespace NanoverImd.UI
{
    /// <summary>
    /// Exposes the residue-selection state needed by a selection panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResidueSelectionPanelController : MonoBehaviour
    {
        [SerializeField]
        private ResidueSelectionWorkspaceController workspaceController;

        [SerializeField]
        private GameObject selectionMode;

        [SerializeField]
        private ResidueSelectionSyncController syncController;

        private ResidueSelectionWorkspace workspace;
        private ResidueSelection activeSelection;

        /// <summary>
        /// The workspace currently represented by the panel.
        /// </summary>
        public ResidueSelectionWorkspace Workspace => workspace;

        /// <summary>
        /// The selection currently represented by the panel.
        /// </summary>
        public ResidueSelection ActiveSelection => activeSelection;

        /// <summary>
        /// Whether residue-selection controller input is currently enabled.
        /// </summary>
        public bool SelectionModeEnabled =>
            selectionMode != null && selectionMode.activeSelf;

        public bool CanSyncSelections => isActiveAndEnabled
                                         && workspace != null
                                         && syncController != null
                                         && syncController.CanSync;

        public bool IsSendingSelections => syncController != null
                                           && syncController.IsSending;

        /// <summary>
        /// UI success requires the server's explicit success acknowledgement
        /// and refers only to the captured snapshot, not subsequent local edits.
        /// </summary>
        public string SelectionSyncStatusText
        {
            get
            {
                if (syncController == null)
                    return "Selection sending is unavailable.";
                if (string.IsNullOrWhiteSpace(syncController.CommandName))
                    return "Set a server Command name to enable sending.";

                switch (syncController.Status)
                {
                    case ResidueSelectionSyncStatus.Sending:
                        return "Sending the current snapshot...";
                    case ResidueSelectionSyncStatus.Succeeded:
                        return "Snapshot synced.\nConfirmed by server.\nLater edits need a new send.";
                    case ResidueSelectionSyncStatus.TimedOut:
                        return "Timed out.\nServer outcome unknown.\nNo automatic retry.";
                    case ResidueSelectionSyncStatus.Cancelled:
                        return "Waiting cancelled.\nServer outcome unknown.";
                    case ResidueSelectionSyncStatus.Failed:
                        return "Send failed.\n" + syncController.LastError;
                }

                return CanSyncSelections
                    ? "Send all selections, including protein.\nLater edits need a new send."
                    : "Sending unavailable.\nCheck connection, workspace and timeout.";
            }
        }

        /// <summary>
        /// Raised when the workspace, active selection, or active selection
        /// contents change.
        /// </summary>
        public event Action StateChanged;

        private void OnEnable()
        {
            Assert.IsNotNull(workspaceController);
            if (workspaceController == null)
            {
                enabled = false;
                return;
            }

            workspaceController.WorkspaceChanged += OnWorkspaceChanged;
            if (syncController != null)
                syncController.StateChanged += OnStateChanged;
            var currentWorkspace = workspaceController.Workspace;
            if (currentWorkspace == null)
                SetSelectionModeEnabled(false);

            BindWorkspace(currentWorkspace);
        }

        private void OnDisable()
        {
            if (syncController != null)
                syncController.StateChanged -= OnStateChanged;

            if (workspaceController != null)
                workspaceController.WorkspaceChanged -= OnWorkspaceChanged;

            BindWorkspace(null);
        }

        private void OnWorkspaceChanged(
            ResidueSelectionWorkspace nextWorkspace)
        {
            if (nextWorkspace == null)
                SetSelectionModeEnabled(false);

            BindWorkspace(nextWorkspace);
        }

        /// <summary>
        /// Enables or disables residue-selection controller input.
        /// </summary>
        public void SetSelectionModeEnabled(bool enabled)
        {
            if (selectionMode == null
                || selectionMode.activeSelf == enabled)
                return;

            selectionMode.SetActive(enabled);
            OnStateChanged();
        }

        /// <summary>
        /// Toggles residue-selection controller input.
        /// </summary>
        public void ToggleSelectionMode()
        {
            if (selectionMode == null)
                return;

            SetSelectionModeEnabled(!selectionMode.activeSelf);
        }

        /// <summary>
        /// Creates an empty selection and makes it active.
        /// </summary>
        public void CreateSelection()
        {
            workspace?.CreateSelection();
        }

        /// <summary>
        /// Makes the supplied workspace selection active.
        /// </summary>
        public void SetActiveSelection(ResidueSelection selection)
        {
            if (workspace == null || selection == null)
                return;

            workspace.SetActiveSelection(selection);
        }

        /// <summary>
        /// Deletes the active custom selection.
        /// </summary>
        public void DeleteActiveSelection()
        {
            if (workspace == null
                || activeSelection == null
                || activeSelection.IsReadOnly)
                return;

            workspace.DeleteSelection(activeSelection);
        }

        /// <summary>
        /// Removes every residue from the active custom selection.
        /// </summary>
        public void ClearActiveSelection()
        {
            if (activeSelection == null || activeSelection.IsReadOnly)
                return;

            activeSelection.Clear();
        }

        /// <summary>
        /// Button-event entry point. Handles async errors here so UI callbacks
        /// never leave an unobserved command task. It does not retry requests.
        /// </summary>
        public async void SendSelections()
        {
            if (!CanSyncSelections)
            {
                OnStateChanged();
                return;
            }

            try
            {
                await syncController.SyncCurrentWorkspaceAsync();
            }
            catch (OperationCanceledException)
            {
                // The sync controller exposes the cancellation state to UI.
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Selection command did not complete: "
                                 + exception.Message);
            }
        }

        private void BindWorkspace(
            ResidueSelectionWorkspace nextWorkspace)
        {
            if (ReferenceEquals(workspace, nextWorkspace))
            {
                BindActiveSelection(nextWorkspace?.ActiveSelection);
                return;
            }

            UnbindActiveSelection();

            if (workspace != null)
            {
                workspace.ActiveSelectionChanged -= OnActiveSelectionChanged;
                workspace.SelectionAdded -= OnSelectionAdded;
                workspace.SelectionRemoved -= OnSelectionRemoved;
            }

            workspace = nextWorkspace;

            if (workspace != null)
            {
                workspace.ActiveSelectionChanged += OnActiveSelectionChanged;
                workspace.SelectionAdded += OnSelectionAdded;
                workspace.SelectionRemoved += OnSelectionRemoved;
            }

            BindActiveSelection(workspace?.ActiveSelection);
        }

        private void OnSelectionAdded(ResidueSelection selection)
        {
            OnStateChanged();
        }

        private void OnSelectionRemoved(ResidueSelection selection)
        {
            OnStateChanged();
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
                OnStateChanged();
                return;
            }

            UnbindActiveSelection();
            activeSelection = nextSelection;

            if (activeSelection != null)
                activeSelection.Changed += OnSelectionChanged;

            OnStateChanged();
        }

        private void UnbindActiveSelection()
        {
            if (activeSelection != null)
                activeSelection.Changed -= OnSelectionChanged;

            activeSelection = null;
        }

        private void OnSelectionChanged(ResidueSelection selection)
        {
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
