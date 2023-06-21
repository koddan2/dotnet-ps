using System.Management.Automation;

namespace EmbedPSExample
{
    public class ConsoleStreamCallbacks : IStreamCallbacks
    {
        private static void WithColor(ConsoleColor color, Action action)
        {
            var reset = Console.ForegroundColor;
            Console.ForegroundColor = color;
            action?.Invoke();
            Console.ForegroundColor = reset;
        }

        public void Debug(DebugRecord record) =>
            WithColor(
                ConsoleColor.Cyan,
                () =>
                Console.WriteLine("DBG: {0}", record));

        public void Error(ErrorRecord record) =>
            WithColor(
                ConsoleColor.Red,
                () =>
                Console.Error.WriteLine("ERR: {0}", record));

        public void Information(InformationRecord record) => Console.WriteLine("INF: {0}", record);

        public void Verbose(VerboseRecord record) =>
            WithColor(
                ConsoleColor.DarkCyan,
                () =>
                Console.WriteLine("VRB: {0}", record));

        public void Warning(WarningRecord record) =>
            WithColor(
                ConsoleColor.DarkYellow,
                () =>
                Console.WriteLine("WRN: {0}", record));

        public void Progress(ProgressRecord record)
        {
            WithColor(
                ConsoleColor.Green,
                () =>
                {
                    var progressBar = string.Concat(Enumerable.Range(0, record.PercentComplete / 2).Select(_ => '#'));
                    var ending = record.PercentComplete >= 100 || record.RecordType == ProgressRecordType.Completed ? Environment.NewLine : "";
                    Console.Write($"\rPGS: {record.PercentComplete,3}% {record.Activity} [{progressBar,-50}] {record.RecordType,15}{ending}");
                });
        }
    }

    public class TextWriterStreamCallbacks : IStreamCallbacks, IDisposable
    {
        private readonly TextWriter _writer;

        public TextWriterStreamCallbacks(TextWriter writer)
        {
            _writer = writer;
        }

        public void Debug(DebugRecord record) => _writer.WriteLine("DBG: {0}", record);

        public void Error(ErrorRecord record) => _writer.WriteLine("ERR: {0}", record);

        public void Information(InformationRecord record) => _writer.WriteLine("INF: {0}", record);

        public void Verbose(VerboseRecord record) => _writer.WriteLine("VRB: {0}", record);

        public void Warning(WarningRecord record) => _writer.WriteLine("WRN: {0}", record);

        public void Progress(ProgressRecord record) => _writer.WriteLine("PGS: {0}", record);

        /// <summary>
        /// Disposes of the <see cref="StreamWriter"/>.
        /// </summary>
        public void Dispose()
        {
            _writer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}