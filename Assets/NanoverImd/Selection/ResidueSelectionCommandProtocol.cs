using System;
using CommandReturn = System.Collections.Generic.Dictionary<string, object>;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Client-defined contract for manually replacing the server's dedicated
    /// residue-selection collection with the current complete snapshot.
    /// </summary>
    public static class ResidueSelectionCommandProtocol
    {
        public const string SyncCommandName = "residue-selections/sync";

        /// <summary>
        /// Requires an explicit boolean success acknowledgement. A response
        /// alone is not treated as successful synchronization.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown for a rejected snapshot or an invalid response shape.
        /// </exception>
        public static void EnsureSuccess(CommandReturn response)
        {
            if (response == null
                || !response.TryGetValue("success", out var value)
                || !(value is bool success))
                throw new InvalidOperationException(
                    "Invalid selection command response: expected a boolean 'success' field.");

            if (success)
                return;

            if (response.TryGetValue("error", out var error)
                && error is string message
                && !string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException(
                    "Server rejected selection snapshot: " + message);

            throw new InvalidOperationException(
                "Invalid selection command response: success=false requires a non-empty 'error' string.");
        }
    }
}
