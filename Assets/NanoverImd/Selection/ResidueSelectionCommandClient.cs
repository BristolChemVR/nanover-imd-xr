using System;
using Cysharp.Threading.Tasks;
using WebSocketTypes;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Sends selection snapshots through an existing NanoVer command source.
    /// Does not own the connection, listen for selection changes, or interpret
    /// server-specific responses.
    /// </summary>
    public sealed class ResidueSelectionCommandClient
    {
        private readonly WebSocketMessageSource commandSource;

        public ResidueSelectionCommandClient(WebSocketMessageSource commandSource)
        {
            this.commandSource = commandSource
                ?? throw new ArgumentNullException(nameof(commandSource));
        }

        /// <summary>
        /// Sends one complete workspace snapshot using the caller-supplied
        /// command name and returns the server response unchanged. No request
        /// is sent while disconnected; the returned task is cancelled, matching
        /// NanoverImdSimulation.RunCommand.
        /// </summary>
        /// <remarks>
        /// Returning a response does not imply that the server applied a sync;
        /// that depends on the handler's response contract. This adapter adds
        /// no timeout or retries to the underlying command task. Call it on the
        /// thread used by the command source, normally Unity's main thread.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when the command name is null, empty, or whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the snapshot is null.
        /// </exception>
        public UniTask<CommandReturn> SendWorkspaceSnapshotAsync(
            string commandName,
            ResidueSelectionWorkspaceSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                throw new ArgumentException("A command name is required.",
                                            nameof(commandName));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (!commandSource.Connected)
                return UniTask.FromCanceled<CommandReturn>();

            var arguments = ResidueSelectionCommandArguments
                .FromWorkspaceSnapshot(snapshot);

            return commandSource.RunCommand(commandName, arguments);
        }
    }
}
