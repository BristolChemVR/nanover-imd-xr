using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace NanoverImd.Selection
{
    public enum ResidueSelectionSyncStatus
    {
        Idle,
        Sending,
        Succeeded,
        Failed,
        TimedOut,
        Cancelled
    }

    /// <summary>
    /// Manually sends the current workspace and exposes request state for UI.
    /// Never sends automatically when selections or connections change.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NanoverImdSimulation))]
    [RequireComponent(typeof(ResidueSelectionWorkspaceController))]
    public sealed class ResidueSelectionSyncController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Server-registered command name. Empty disables sending.")]
        private string commandName = ResidueSelectionCommandProtocol.SyncCommandName;

        [SerializeField, Min(0.1f)]
        [Tooltip("Maximum real-time seconds to wait for a command response.")]
        private float timeoutSeconds = 10f;

        private NanoverImdSimulation simulation;
        private ResidueSelectionWorkspaceController workspaceController;
        private ResidueSelectionCommandClient commandClient;
        private CancellationTokenSource pendingWait;
        private string cancellationReason;

        public string CommandName => commandName;
        public ResidueSelectionSyncStatus Status { get; private set; }
        public bool IsSending => pendingWait != null;

        /// <summary>
        /// Whether a manual request can currently start. Does not check that
        /// the server has registered the configured command.
        /// </summary>
        public bool CanSync => isActiveAndEnabled
                               && !IsSending
                               && simulation != null
                               && simulation.Connected
                               && workspaceController != null
                               && workspaceController.Workspace != null
                               && !string.IsNullOrWhiteSpace(commandName)
                               && IsValidTimeout();

        /// <summary>
        /// The snapshot captured for the most recent request attempt.
        /// Later local edits are not represented by this snapshot.
        /// </summary>
        public ResidueSelectionWorkspaceSnapshot LastSnapshot { get; private set; }

        /// <summary>
        /// Raw server response, including rejected or malformed responses.
        /// Succeeded means the server explicitly acknowledged success=true.
        /// </summary>
        public CommandReturn LastResponse { get; private set; }

        public string LastError { get; private set; }
        public event Action StateChanged;

        private void Awake()
        {
            simulation = GetComponent<NanoverImdSimulation>();
            workspaceController = GetComponent<ResidueSelectionWorkspaceController>();
            commandClient = new ResidueSelectionCommandClient(simulation);
        }

        private void OnEnable()
        {
            workspaceController.WorkspaceChanged += OnWorkspaceChanged;
            simulation.SessionOpened += OnSessionOpened;
            simulation.SessionClosed += OnSessionClosed;
            ResetResult();
            StateChanged?.Invoke();
        }

        private void OnDisable()
        {
            workspaceController.WorkspaceChanged -= OnWorkspaceChanged;
            simulation.SessionOpened -= OnSessionOpened;
            simulation.SessionClosed -= OnSessionClosed;
            CancelPendingWait("The sync component was disabled; the server outcome is unknown.");
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Captures and sends all current selections once. Await and handle
        /// exceptions at the call site. A concurrent call is rejected without
        /// changing the state of the request already in progress.
        /// </summary>
        /// <remarks>
        /// Timeout and lifecycle cancellation stop only the local wait. They
        /// cannot retract the command or remove it from the underlying network
        /// client's pending-command registry. No automatic retries are made.
        /// Call on Unity's main thread.
        /// </remarks>
        public async UniTask<CommandReturn> SyncCurrentWorkspaceAsync()
        {
            if (IsSending)
                throw new InvalidOperationException("A selection request is already in progress.");

            CancellationTokenSource requestWait = null;
            IDisposable timeoutTimer = null;
            ResetResult();

            try
            {
                ValidateCanSend();
                LastSnapshot = ResidueSelectionWorkspaceSnapshot
                    .Capture(workspaceController.Workspace);

                requestWait = new CancellationTokenSource();
                pendingWait = requestWait;
                cancellationReason = null;
                Status = ResidueSelectionSyncStatus.Sending;
                timeoutTimer = requestWait.CancelAfterSlim(
                    TimeSpan.FromSeconds(timeoutSeconds), DelayType.Realtime);
                StateChanged?.Invoke();

                // An event listener may disable this component or replace the
                // workspace before sending. Do not send in that case.
                requestWait.Token.ThrowIfCancellationRequested();
                var response = await commandClient
                    .SendWorkspaceSnapshotAsync(commandName, LastSnapshot)
                    .AttachExternalCancellation(requestWait.Token);
                requestWait.Token.ThrowIfCancellationRequested();

                LastResponse = response;
                ResidueSelectionCommandProtocol.EnsureSuccess(response);
                Status = ResidueSelectionSyncStatus.Succeeded;
                return response;
            }
            catch (OperationCanceledException exception)
            {
                if (requestWait != null
                    && requestWait.IsCancellationRequested
                    && cancellationReason == null)
                {
                    Status = ResidueSelectionSyncStatus.TimedOut;
                    LastError = "Waiting for the selection command timed out; the server outcome is unknown.";
                    throw new TimeoutException(LastError, exception);
                }

                Status = ResidueSelectionSyncStatus.Cancelled;
                LastError = cancellationReason
                    ?? "The selection command was cancelled; the server outcome is unknown.";
                throw;
            }
            catch (Exception exception)
            {
                Status = ResidueSelectionSyncStatus.Failed;
                LastError = exception.Message;
                throw;
            }
            finally
            {
                timeoutTimer?.Dispose();
                if (ReferenceEquals(pendingWait, requestWait))
                    pendingWait = null;
                requestWait?.Dispose();
                StateChanged?.Invoke();
            }
        }

        private void ValidateCanSend()
        {
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("The selection sync component is disabled.");
            if (simulation == null || workspaceController == null || commandClient == null)
                throw new InvalidOperationException("The selection sync component is not initialized.");
            if (string.IsNullOrWhiteSpace(commandName))
                throw new InvalidOperationException("No selection command name is configured.");
            if (!IsValidTimeout())
                throw new InvalidOperationException("The response timeout must be positive and finite.");
            if (!simulation.Connected)
                throw new InvalidOperationException("The simulation is not connected to a server.");
            if (workspaceController.Workspace == null)
                throw new InvalidOperationException("No residue-selection workspace is available.");
        }

        private bool IsValidTimeout()
        {
            return timeoutSeconds > 0f
                   && !float.IsNaN(timeoutSeconds)
                   && !float.IsInfinity(timeoutSeconds);
        }

        private void OnWorkspaceChanged(ResidueSelectionWorkspace workspace)
        {
            if (IsSending)
                CancelPendingWait("The workspace changed; the previous command's server outcome is unknown.");
            else
                ResetResult();
            StateChanged?.Invoke();
        }

        private void OnSessionOpened()
        {
            if (IsSending)
                CancelPendingWait("The connection changed; the previous command's server outcome is unknown.");
            else
                ResetResult();
            StateChanged?.Invoke();
        }

        private void OnSessionClosed()
        {
            if (IsSending)
                CancelPendingWait("The connection closed; the server outcome is unknown.");
            else
                ResetResult();
            StateChanged?.Invoke();
        }

        private void CancelPendingWait(string reason)
        {
            if (pendingWait == null || pendingWait.IsCancellationRequested)
                return;

            cancellationReason = reason;
            pendingWait.Cancel();
        }

        private void ResetResult()
        {
            Status = ResidueSelectionSyncStatus.Idle;
            LastSnapshot = null;
            LastResponse = null;
            LastError = null;
        }
    }
}
