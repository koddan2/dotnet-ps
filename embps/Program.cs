using PsEmbed;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

/*
<ItemGroup>
    <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.3.4" />
</ItemGroup>
*/

namespace EmbedPSExample
{
    internal record TestVarComplex
    {
        public int IntVal { get; set; } = 42;
        public string StringVal { get; set; } = "hello, world!";
    }

    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var pscode = File.ReadAllText(Path.Combine(args[0], "stuff.ps1"));
                ////await RunScript(pscode);

                using var manager = new PsRunspaceManager();
                List<SessionStateVariableEntry> sessionStateVariables = new()
                {
                    new SessionStateVariableEntry("testvar", "testvar_value", "iss test 1"),
                    new SessionStateVariableEntry("testvar_complex", new TestVarComplex(), "iss test 1"),
                };
                await manager.InitializeAsync(sessionStateVariables: sessionStateVariables);
                var result = await manager.ExecutePowershellCodeAsync(pscode, streamCallbacks: new StreamCallbacks
                {
                    Information = (record) => Console.WriteLine("I: {0}", record),
                    Verbose = (record) => Console.WriteLine("V: {0}", record),
                    Warning = (record) => Console.WriteLine("W: {0}", record),
                    Error = (record) => Console.WriteLine("E: {0}", record),

                    Progress = (record) => Console.Write($"\rP: {record.PercentComplete,3}%{(record.PercentComplete >= 100 ? Environment.NewLine : "")}"),
                });

                foreach (var item in result)
                {
                    Console.WriteLine(item);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        /// <summary>
        /// Runs a PowerShell script with parameters and prints the resulting pipeline objects to the console output.
        /// More info at
        /// https://keithbabinec.com/2020/02/15/how-to-run-powershell-core-scripts-from-net-core-applications/
        /// https://github.com/keithbabinec/PowerShellHostedRunspaceStarterkits
        /// </summary>
        /// <param name="powershellCode">The powershell code.</param>
        /// <param name="scriptParameters">A dictionary of parameter names and parameter values.</param>
        public static async Task RunScript(string powershellCode, Dictionary<string, object>? scriptParameters = null)
        {
            scriptParameters ??= new();

            // see https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-an-initialsessionstate?view=powershell-7.3
            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Variables.Add(
                new SessionStateVariableEntry("testvar", "testvar_value", "iss test 1"));

            initialSessionState.Variables.Add(
                new SessionStateVariableEntry("testvar_complex", new TestVarComplex(), "iss test 1"));

            // Call RunspaceFactory.CreateRunspace(InitialSessionState) to 
            // create the runspace where the pipeline is run.
            using var rs = RunspaceFactory.CreateRunspace(initialSessionState);
            rs.Open();

            // create a new hosted PowerShell instance using the default runspace.
            // wrap in a using statement to ensure resources are cleaned up.
            using var ps = PowerShell.Create(rs);

            ps.AddScript(powershellCode);
            ps.AddParameters(scriptParameters);

            ps.Streams.Verbose.DataAdded += Stream_DataAdded;
            ps.Streams.Information.DataAdded += Stream_DataAdded;
            ps.Streams.Warning.DataAdded += Stream_DataAdded;
            ps.Streams.Error.DataAdded += Stream_DataAdded;
            ps.Streams.Debug.DataAdded += Stream_DataAdded;
            ps.Streams.Progress.DataAdded += Stream_DataAdded;

            var pipelineObjects = await ps.InvokeAsync().ConfigureAwait(false);

            // print the resulting pipeline objects to the console.
            foreach (var item in pipelineObjects)
            {
                Console.WriteLine(item.BaseObject.ToString());
            }
        }

        private static void Stream_DataAdded(object? sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<VerboseRecord> verboses)
            {
                var currentStreamRecord = verboses[e.Index];
                Console.Write("V: ");
                Console.WriteLine(currentStreamRecord);
            }
            else if (sender is PSDataCollection<ProgressRecord> progresses)
            {
                var currentStreamRecord = progresses[e.Index];
                ////Console.Write("P: ");
                ////Console.WriteLine(currentStreamRecord);
                Console.Write($"\rP: {currentStreamRecord.PercentComplete,3}% - {currentStreamRecord.Activity} ({currentStreamRecord.StatusDescription}) {currentStreamRecord.CurrentOperation}");
                if (currentStreamRecord.PercentComplete >= 100)
                {
                    Console.WriteLine();
                }
            }
            else if (sender is PSDataCollection<DebugRecord> debugs)
            {
                var currentStreamRecord = debugs[e.Index];
                Console.Write("D: ");
                Console.WriteLine(currentStreamRecord);
            }
            else if (sender is PSDataCollection<InformationRecord> infos)
            {
                var currentStreamRecord = infos[e.Index];
                Console.Write("I: ");
                Console.WriteLine(currentStreamRecord);
            }
            else if (sender is PSDataCollection<WarningRecord> warnings)
            {
                var currentStreamRecord = warnings[e.Index];
                Console.Write("W: ");
                Console.WriteLine(currentStreamRecord);
            }
            else if (sender is PSDataCollection<ErrorRecord> errors)
            {
                var currentStreamRecord = errors[e.Index];
                Console.Write("E: ");
                Console.WriteLine(currentStreamRecord);
            }
            else
            {
                Console.Error.WriteLine("Unknown type of data");
            }
        }
    }
}