using Microsoft.PowerShell;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace EmbedPSExample
{
    /// <summary>
    /// A somewhat simple PowerShell runspace manager.
    /// </summary>
    public class PsRunspaceManager : IDisposable
    {
        /// <summary>
        /// The PowerShell runspace pool.
        /// </summary>
        private RunspacePool? RunspacePool { get; set; }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="minRunspaces">The minimum number of runspaces in the underlying runspace pool.</param>
        /// <param name="maxRunspaces">The maximum number of runspaces in the underlying runspace pool.</param>
        /// <param name="modulesToLoad">The names of the modules to load for each runspace.</param>
        /// <param name="executionPolicy">The execution policy that will be applied for each runspace.</param>
        /// <param name="sessionStateVariables">The initial session state variables for each runspace.</param>
        /// <param name="psThreadOptions">The thread options to use for each runspace.</param>
        /// <param name="initialSessionStateCreationSetting">This setting controls whether to create a default initial session state, or a restricted (minimal) one.</param>
        /// <returns>The async task, indicating when initialization becomes completed.</returns>
        public async Task InitializeAsync(
            int minRunspaces = 1,
            int maxRunspaces = 4,
            IEnumerable<string>? modulesToLoad = null,
            ExecutionPolicy executionPolicy = ExecutionPolicy.Default,
            IEnumerable<SessionStateVariableEntry>? sessionStateVariables = null,
            PSThreadOptions psThreadOptions = PSThreadOptions.Default,
            InitialSessionStateCreationSetting initialSessionStateCreationSetting = default)
        {
            modulesToLoad ??= Array.Empty<string>();
            sessionStateVariables ??= Array.Empty<SessionStateVariableEntry>();

            // create the default session state.
            // session state can be used to set things like execution policy, language constraints, etc.
            var defaultSessionState = initialSessionStateCreationSetting switch
            {
                InitialSessionStateCreationSetting.Default => InitialSessionState.CreateDefault(),
                InitialSessionStateCreationSetting.Restricted => InitialSessionState.CreateRestricted(SessionCapabilities.Language),
            };

            defaultSessionState.ExecutionPolicy = executionPolicy;

            foreach (var moduleName in modulesToLoad)
            {
                defaultSessionState.ImportPSModule(moduleName);
            }

            foreach (var sessionStateVariable in sessionStateVariables)
            {
                defaultSessionState.Variables.Add(sessionStateVariable);
            }

            // use the runspace factory to create a pool of runspaces
            // with a minimum and maximum number of runspaces to maintain.
            RunspacePool = RunspaceFactory.CreateRunspacePool(defaultSessionState);
            RunspacePool.SetMinRunspaces(minRunspaces);
            RunspacePool.SetMaxRunspaces(maxRunspaces);
            RunspacePool.ThreadOptions = psThreadOptions;

            await Task.Factory.FromAsync(RunspacePool.BeginOpen(null, null), RunspacePool.EndOpen);
        }

        /// <summary>
        /// Execute a block of PowerShell code asynchronously, returning the results from the execution.
        /// </summary>
        /// <param name="powershellCode">The PowerShell code to execute.</param>
        /// <param name="scriptParameters">The parameters to pass to the PowerShell code, if any.</param>
        /// <param name="streamCallbacks">The object describing how output should be handled.</param>
        /// <returns>The async result from the execution.</returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<PSDataCollection<PSObject>> ExecutePowershellCodeAsync(
            string powershellCode,
            Dictionary<string, object>? scriptParameters = null,
            IStreamCallbacks? streamCallbacks = null)
        {
            if (RunspacePool is null)
            {
                throw new ApplicationException(
                    $"This instance of {nameof(PsRunspaceManager)} is not initialized, which is required in order to be able to invoke {nameof(ExecutePowershellCodeAsync)}.");
            }

            // create a new hosted PowerShell instance using a custom runspace.
            // wrap in a using statement to ensure resources are cleaned up.
            using PowerShell ps = PowerShell.Create();

            // Important: use the manager's runspace pool.
            ps.RunspacePool = RunspacePool;
            ps.AddScript(powershellCode);
            if (scriptParameters is not null)
            {
                ps.AddParameters(scriptParameters);
            }

            // This is a catch-all delegate for use with all of the PS streams.
            // It dispatches to the relevant callback on the value of the parameter streamCallbacks,
            // but only if the callback is not null. It is up to the caller of the containing method
            // to either make sure that the powershell code itself sets $ErrorActionPreference OR
            // to make sure that there is a streamCallbacks.Error callback defined.
            // (otherwise errors will not be reported).
            void Stream_DataAdded(object? sender, DataAddedEventArgs e)
            {
                if (streamCallbacks is null)
                {
                    return;
                }

                if (sender is PSDataCollection<VerboseRecord> verboses)
                {
                    var currentStreamRecord = verboses[e.Index];
                    streamCallbacks.Verbose(currentStreamRecord);
                }
                else if (sender is PSDataCollection<ProgressRecord> progresses)
                {
                    var currentStreamRecord = progresses[e.Index];
                    streamCallbacks.Progress(currentStreamRecord);
                }
                else if (sender is PSDataCollection<DebugRecord> debugs)
                {
                    var currentStreamRecord = debugs[e.Index];
                    streamCallbacks.Debug(currentStreamRecord);
                }
                else if (sender is PSDataCollection<InformationRecord> infos)
                {
                    var currentStreamRecord = infos[e.Index];
                    streamCallbacks.Information(currentStreamRecord);
                }
                else if (sender is PSDataCollection<WarningRecord> warnings)
                {
                    var currentStreamRecord = warnings[e.Index];
                    streamCallbacks.Warning(currentStreamRecord);
                }
                else if (sender is PSDataCollection<ErrorRecord> errors)
                {
                    var currentStreamRecord = errors[e.Index];
                    streamCallbacks.Error(currentStreamRecord);
                }
            }

            // subscribe to events from all available streams
            ps.Streams.Verbose.DataAdded += Stream_DataAdded;
            ps.Streams.Information.DataAdded += Stream_DataAdded;
            ps.Streams.Warning.DataAdded += Stream_DataAdded;
            ps.Streams.Error.DataAdded += Stream_DataAdded;
            ps.Streams.Debug.DataAdded += Stream_DataAdded;
            ps.Streams.Progress.DataAdded += Stream_DataAdded;

            // execute the script and await the result.
            var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);

            return pipelineObjects ?? Array.Empty<PSObject>();
        }

        /// <summary>
        /// Disposes of the underlying runspace pool, if initialized.
        /// </summary>
        public void Dispose()
        {
            RunspacePool?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}