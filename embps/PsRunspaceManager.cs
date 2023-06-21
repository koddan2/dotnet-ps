using Microsoft.PowerShell;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace EmbedPSExample
{
    public class StreamCallbacks
    {
        public Action<InformationRecord>? Information { get; set; }
        public Action<VerboseRecord>? Verbose { get; set; }
        public Action<WarningRecord>? Warning { get; set; }
        public Action<ErrorRecord>? Error { get; set; }
        public Action<DebugRecord>? Debug { get; set; }
        public Action<ProgressRecord>? Progress { get; set; }
    }

    public enum InitialSessionStateCreationSetting
    {
        Default,
        Restricted,
    }

    public class PsRunspaceManager : IDisposable
    {
        /// <summary>
        /// The PowerShell runspace pool.
        /// </summary>
        private RunspacePool? RunspacePool { get; set; }

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
            // optionally load any modules (by name) that were supplied.
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

        public async Task<PSDataCollection<PSObject>> ExecutePowershellCodeAsync(
            string powershellCode,
            Dictionary<string, object>? scriptParameters = null,
            StreamCallbacks? streamCallbacks = null)
        {
            scriptParameters ??= new();

            if (RunspacePool is null)
            {
                throw new ApplicationException($"The runspace pool must be initialized before calling {nameof(ExecutePowershellCodeAsync)}.");
            }

            // create a new hosted PowerShell instance using a custom runspace.
            // wrap in a using statement to ensure resources are cleaned up.
            using PowerShell ps = PowerShell.Create();

            // Important: use the manager's runspace pool.
            ps.RunspacePool = RunspacePool;
            ps.AddScript(powershellCode);
            ps.AddParameters(scriptParameters);

            void Stream_DataAdded(object? sender, DataAddedEventArgs e)
            {
                if (streamCallbacks?.Verbose is Action<VerboseRecord> verboseCallback
                    && sender is PSDataCollection<VerboseRecord> verboses)
                {
                    var currentStreamRecord = verboses[e.Index];
                    verboseCallback(currentStreamRecord);
                }
                else if (streamCallbacks?.Progress is Action<ProgressRecord> progressCallback
                    && sender is PSDataCollection<ProgressRecord> progresses)
                {
                    var currentStreamRecord = progresses[e.Index];
                    progressCallback(currentStreamRecord);
                }
                else if (streamCallbacks?.Debug is Action<DebugRecord> debugCallback
                    && sender is PSDataCollection<DebugRecord> debugs)
                {
                    var currentStreamRecord = debugs[e.Index];
                    debugCallback(currentStreamRecord);
                }
                else if (streamCallbacks?.Information is Action<InformationRecord> infoCallback
                    && sender is PSDataCollection<InformationRecord> infos)
                {
                    var currentStreamRecord = infos[e.Index];
                    infoCallback(currentStreamRecord);
                }
                else if (streamCallbacks?.Warning is Action<WarningRecord> warningCallback
                    && sender is PSDataCollection<WarningRecord> warnings)
                {
                    var currentStreamRecord = warnings[e.Index];
                    warningCallback(currentStreamRecord);
                }
                else if (streamCallbacks?.Error is Action<ErrorRecord> errorCallback
                    && sender is PSDataCollection<ErrorRecord> errors)
                {
                    var currentStreamRecord = errors[e.Index];
                    errorCallback(currentStreamRecord);
                }
                ////else
                ////{
                ////    throw new InvalidOperationException($"Unknown type of data: {sender?.GetType()}");
                ////}
            }

            // subscribe to events from some of the streams
            ps.Streams.Verbose.DataAdded += Stream_DataAdded;
            ps.Streams.Information.DataAdded += Stream_DataAdded;
            ps.Streams.Warning.DataAdded += Stream_DataAdded;
            ps.Streams.Error.DataAdded += Stream_DataAdded;
            ps.Streams.Debug.DataAdded += Stream_DataAdded;
            ps.Streams.Progress.DataAdded += Stream_DataAdded;

            // execute the script and await the result.
            var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);

            // print the resulting pipeline objects to the console.
            return pipelineObjects ?? Array.Empty<PSObject>();
        }

        public void Dispose()
        {
            RunspacePool?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}