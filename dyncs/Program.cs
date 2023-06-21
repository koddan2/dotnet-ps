using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using somelib;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Westwind.Scripting;

namespace dyncs
{
    public record ArgsAccessor(string[] Args);
    public class Program
    {
        private readonly string[] _args;
        private readonly IServiceProvider _serviceProvider;

        public Program(ArgsAccessor argsAccessor, IServiceProvider serviceProvider)
        {
            _args = argsAccessor.Args;
            _serviceProvider = serviceProvider;
        }

        static async Task<int> Main(string[] args)
        {
            try
            {
                var hostBuiler = Host.CreateApplicationBuilder(args);
                hostBuiler.Services.AddTransient<Program>();
                hostBuiler.Services.AddSingleton<ArgsAccessor>((_) => new ArgsAccessor(args));
                hostBuiler.Services.AddSingleton<Thingamajig<int>>((_) => new Thingamajig<int>(1, 2, 3));
                var host = hostBuiler.Build();
                var program = host.Services.GetRequiredService<Program>();
                await program.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private async Task RunAsync()
        {
            var script = new CSharpScriptExecution() { SaveGeneratedCode = false };
            script.AddDefaultReferencesAndNamespaces();
            script.AddLoadedReferences();
            script.AddNetCoreDefaultReferences();
            script.AddAssembly(typeof(HttpClient));

            var csharpCode = await File
                .ReadAllTextAsync(Path.Combine(_args[0], "script.cs"));

            IScriptInterface? cls = script.CompileClass(csharpCode) as IScriptInterface;

            //Console.WriteLine(script.GeneratedClassCodeWithLineNumbers);
            File.WriteAllText(Path.Combine(_args[0], "generated-class.cs.txt"), script.GeneratedClassCodeWithLineNumbers);
            Debug.Assert(!script.Error, script.ErrorMessage);
            Debug.Assert(cls is not null);

            //string result = await cls.DoAsync(_serviceProvider);
            //Console.WriteLine($"{result}");

            //var ar = cls.GetArecord();
            //Console.WriteLine($"{ar}");
            if (cls is not null)
            {
                var task = cls?.ExecuteAsync(_serviceProvider);
                if (task is not null)
                {
                    Console.WriteLine(await task);
                }
            }
        }

        ////public record Globals (IServiceProvider ServiceProvider);
        ////private async Task RunAsync()
        ////{
        ////    var csLines = ImmutableList.Create(File
        ////        .ReadAllLines(Path.Combine(_args[0], "script.csx")));
        ////    csLines = AddRef(typeof(Thingamajig<>), csLines);
        ////    csLines = AddRef(typeof(HttpClient), csLines);
        ////    var csharpCode = string.Join("\n", csLines);
        ////    using var loader = new InteractiveAssemblyLoader();
        ////    loader.RegisterDependency(typeof(HttpClient).Assembly);
        ////    CSharpScriptExecution
        ////    var script = CSharpScript.Create(
        ////        csharpCode,
        ////        options: ScriptOptions.Default,
        ////        globalsType: typeof(Globals),
        ////        assemblyLoader: loader);
        ////    var compilation = script.GetCompilation();
        ////    bool isError = false;
        ////    foreach (var diag in compilation.GetDiagnostics())
        ////    {
        ////        Console.Error.WriteLine(diag);
        ////        if (diag.WarningLevel == 0)
        ////        {
        ////            isError = true;
        ////        }
        ////    }
        ////    if (!isError)
        ////    {
        ////        await script.RunAsync(new Globals(_serviceProvider));
        ////    }
        ////    else
        ////    {
        ////        Console.Error.WriteLine("Script contains errors!");
        ////    }

        ////    ////Console.WriteLine("Executed script below");
        ////    ////Console.WriteLine("------");
        ////    ////Console.WriteLine(cs);
        ////    File.WriteAllText(Path.Combine(_args[0], "executed-script.cs"), csharpCode);
        ////}

        ////private static ImmutableList<string> AddRef(Type type, ImmutableList<string> csLines)
        ////{
        ////    var assem = type.Assembly.GetName();
        ////    return csLines.Insert(0, $@"#r ""{assem}""");
        ////}
    }
}