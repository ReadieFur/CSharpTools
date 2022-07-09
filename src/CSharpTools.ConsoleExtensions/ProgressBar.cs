using System;
using System.Threading.Tasks;
namespace CSharpTools.ConsoleExtensions
{
    public class ProgressBar : IDisposable
    {
        #region Private fields
        private string _prefix;
        private int _progress = 0;
        private string? _targetLineID;
        private int? _lineIndex;
        private bool _useNewLine;
        #endregion

        #region Private methods
        private void OnConsoleResized(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            if (!_useNewLine && _lineIndex != null && newWidth < oldWidth)
            {
                /*Clear the line if the console was shrunk.
                * This does not need to be done for automatically positioned bars (ones that use _targetLineID)
                * This is becuase their formatting is managed by the Helper class.
                */
                Output.ClearLine((int)_lineIndex, false).Wait();
            }
            SetProgress(_progress, true, false);
        }
        #endregion

        #region Public methods
        public ProgressBar(string prefix = "", bool queueTask = true)
        {
            _prefix = prefix;
            _targetLineID = Output.ReserveLine();
            _lineIndex = null;
            _useNewLine = false;

            Helpers.consoleResized += OnConsoleResized;
            SetProgress(0, true, queueTask);
        }

        public ProgressBar(int lineIndex, string prefix = "", bool queueTask = true)
        {
            _prefix = prefix;
            _targetLineID = null;
            _lineIndex = lineIndex;
            _useNewLine = false;

            if (_lineIndex < 0 || _lineIndex > Console.WindowHeight - 1) throw new ArgumentOutOfRangeException();

            Helpers.consoleResized += OnConsoleResized;
            SetProgress(0, true, queueTask);
        }

        public ProgressBar(bool useNewLine, string prefix = "", bool queueTask = true)
        {
            _prefix = prefix;
            _targetLineID = !useNewLine ? Output.ReserveLine() : null;
            _lineIndex = null;
            _useNewLine = useNewLine;

            if (!_useNewLine) Helpers.consoleResized += OnConsoleResized;
            SetProgress(0, true, queueTask);
        }

        public void Dispose()
        {
            if (!_useNewLine) Helpers.consoleResized -= OnConsoleResized;
            if (_targetLineID != null) Output.FreeLine(_targetLineID);
        }

        public void SetProgress(int progress, bool skipEqualCheck = false, bool queueTask = true)
        {
            if (progress < 0) { progress = 0; }
            else if (progress > 100) { progress = 100; }

            if (!skipEqualCheck && progress == _progress) { return; }
            _progress = progress;

            int consoleWidth = Helpers.GetSafeConsoleWidth(); //Allow some space to wrap around.
            if (consoleWidth < 4) { return; } //Not enough space to display anything, skip UI update.

            string prefix = _prefix.Substring(0, consoleWidth < _prefix.Length ? consoleWidth : _prefix.Length);
            int spaceAfterPrefix = consoleWidth - prefix.Length + 1;

            //If the progress text will be less than 3-4 characters long, add a trailing space to remove the % that may appear if the progress is reset.
            string progressText = $"{_progress}%";
            if (_progress < 10) progressText += "  ";
            else if (_progress < 100) progressText += " ";
            //progressText += $"{_progress}%";

            string line;
            if (spaceAfterPrefix >= 13) //Prefix + bar + percentage.
            {
                string spaceChar = prefix.Length != 0 ? " " : "";

                int barSpace = spaceAfterPrefix - 8 - spaceChar.Length; //-8 for " [] ***%"

                int progressCharCount = (int)Math.Round((double)_progress / 100 * barSpace, 0);
                string progressString = new string('=', progressCharCount);
                if (progressCharCount != 0 && _progress != 100) { progressString = progressString.Substring(0, progressString.Length - 1) + ">"; }

                int progressRemaininCharCount = barSpace - progressCharCount;
                string progressRemaininString = new string(' ', progressRemaininCharCount);

                line = $"{prefix}{spaceChar}[{progressString}{progressRemaininString}] {progressText}";
            }
            else if (spaceAfterPrefix >= 5) //Prefix + percentage.
            {
                string spaceChar = prefix.Length != 0 ? " " : "";
                line = $"{prefix}{spaceChar}{_progress}%";
            }
            else if (consoleWidth > 13) //Substring prefix + percentage (13 -> 5 for ' ***%', 3 for '...', 5 for at least 5 characters of the prefix).
            {
                string substr = prefix.Substring(0, consoleWidth - 8);
                line = $"{substr}... {progressText}";
            }
            else //Percentage.
            {
                line = progressText;
            }

            Task task;
            if (_useNewLine) task = Helpers.QueueAction((_) => { Console.WriteLine(line); });
            else if (_targetLineID != null) task = Output.UpdateLine(_targetLineID, line);
            else task = Output.WriteAt(0, (int)_lineIndex!, line);

            if (queueTask) task.Wait();
        }

        public void SetPrefix(string prefix)
        {
            _prefix = prefix;
            SetProgress(_progress, true, false);
        }
        #endregion
    }
}
