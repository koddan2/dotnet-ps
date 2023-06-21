using System.Diagnostics;
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
                return await RunAsync(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine("Initializing ...");
            // instantiate the runspace manager (this does not initialize anything, so is relatively cheap).
            using var manager = new PsRunspaceManager();

            // specify variables that should be available to the powershell code
            List<SessionStateVariableEntry> sessionStateVariables = new()
            {
                // $testvar # this is a [string]
                new SessionStateVariableEntry("testvar", "testvar_value", "iss test 1"),
                // $testvar_copmlex # this is an object of type [TestVarComplex]
                new SessionStateVariableEntry("testvar_complex", new TestVarComplex(), "iss test 2"),
            };

            // initialize the manager's runspace pool, and pass the list of variables that we defined above.
            await manager.InitializeAsync(maxRunspaces: 1, sessionStateVariables: sessionStateVariables);

            Console.WriteLine("Initialized. Timing: {0}", sw.Elapsed);

            // get a hold of some powershell code
            var pscode = File.ReadAllText(Path.Combine(args[0], "stuff.ps1"));

            // execute the code
            var output = new ConsoleStreamCallbacks();
            ////var output = new TextWriterStreamCallbacks(Console.Out);
            var result = await manager.ExecutePowershellCodeAsync(pscode, streamCallbacks: output);

            // do something with the result (i.e. the pipeline objects that the powershell code yielded)
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            Console.WriteLine("Done! Timing: {0}", sw.Elapsed);

            return 0;
        }
    }
}