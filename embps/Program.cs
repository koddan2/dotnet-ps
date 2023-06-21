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

    internal class Program
    {
        public Program(string[] args)
        {
            Args = args;
        }

        public string[] Args { get; }

        static async Task<int> Main(string[] args)
        {
            try
            {
                return await new Program(args).RunAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private async Task<int> RunAsync()
        {
            var pscode = File.ReadAllText(Path.Combine(Args[0], "stuff.ps1"));
            ////await RunScript(pscode);

            using var manager = new PsRunspaceManager();
            List<SessionStateVariableEntry> sessionStateVariables = new()
                {
                    new SessionStateVariableEntry("testvar", "testvar_value", "iss test 1"),
                    new SessionStateVariableEntry("testvar_complex", new TestVarComplex(), "iss test 2"),
                };
            await manager.InitializeAsync(sessionStateVariables: sessionStateVariables);
            var result = await manager.ExecutePowershellCodeAsync(pscode, streamCallbacks: new StreamCallbacks
            {
                Information = (record) => Console.WriteLine("I: {0}", record),
                Verbose = (record) => Console.WriteLine("V: {0}", record),
                Warning = (record) => Console.WriteLine("W: {0}", record),
                Error = (record) => Console.WriteLine("E: {0}", record),
                Debug = (record) => Console.WriteLine("E: {0}", record),
                Progress = (record) => Console.Write(
                    $"\rP: {record.PercentComplete,3}% {record.Activity} [{string.Concat(Enumerable.Range(0, record.PercentComplete / 2).Select(_ => '#')),-50}]{(record.PercentComplete >= 100 ? Environment.NewLine : "")}"),
            });

            foreach (var item in result)
            {
                Console.WriteLine(item);
            }

            return 0;
        }
    }
}