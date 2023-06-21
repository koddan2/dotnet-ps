using System;
using System.Threading.Tasks;

namespace dyncs
{
    public interface IScriptInterface
    {
        Task<ScriptInvocationResult> ExecuteAsync(IServiceProvider serviceProvider);
    }
}