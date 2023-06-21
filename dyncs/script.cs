using System.Net.Http;
using System;
using System.Threading.Tasks;
using somelib;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using dyncs;

#nullable enable

namespace scripts
{
    public class Test : IScriptInterface
    {
        public record Arecord(int Val);
        readonly Arecord _inst = new(42);
        public Arecord GetArecord()
        {
            Console.WriteLine(_inst);
            return _inst;
        }

        public static async Task<string> DoAsync(IServiceProvider prov)
        {
            object? o = null;
            Console.WriteLine("Hello from script" + o);

            var a = new Thingamajig<int>(1, 2, 3);
            Console.WriteLine("Hello: {0}", a.Count);

            var http = new HttpClient();
            var resp = await http.GetAsync("http://example.org");
            Console.WriteLine(resp.StatusCode);
            var content = await resp.Content.ReadAsStringAsync();
            Console.WriteLine(content.Length);

            var p = prov.GetRequiredService<Thingamajig<int>>();
            Debug.Assert(p.Count == 3);

            throw new ApplicationException();
            await Task.Delay(10);
            return content;
        }

        async Task<ScriptInvocationResult> IScriptInterface.ExecuteAsync(IServiceProvider serviceProvider)
        {
            this.GetArecord();
            var msg = await DoAsync(serviceProvider);
            return new ScriptInvocationResult
            {
                Error = false,
                Message = msg
            };
        }
    }
}