using System.Management.Automation;

namespace EmbedPSExample
{
    public interface IStreamCallbacks
    {
        ////Action<DebugRecord>? Debug { get; }
        ////Action<ErrorRecord>? Error { get; }
        ////Action<InformationRecord>? Information { get; }
        ////Action<ProgressRecord>? Progress { get; }
        ////Action<VerboseRecord>? Verbose { get; }
        ////Action<WarningRecord>? Warning { get; }
        void Debug(DebugRecord record);
        void Error(ErrorRecord record);
        void Information(InformationRecord record);
        void Progress(ProgressRecord record);
        void Verbose(VerboseRecord record);
        void Warning(WarningRecord record);
    }
}