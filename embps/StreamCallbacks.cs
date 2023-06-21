using System.Management.Automation;

namespace EmbedPSExample
{
    public class StreamCallbacks : IStreamCallbacks
    {
        public void Debug(DebugRecord record)
        {
        }

        public void Error(ErrorRecord record)
        {
        }

        public void Information(InformationRecord record)
        {
        }

        public void Progress(ProgressRecord record)
        {
        }

        public void Verbose(VerboseRecord record)
        {
        }

        public void Warning(WarningRecord record)
        {
        }
    }
}