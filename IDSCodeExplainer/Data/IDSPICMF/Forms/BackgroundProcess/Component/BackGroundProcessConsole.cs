using IDS.Core.Enumerators;
using IDS.Interface.Tools;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class BackGroundProcessConsole : IConsole
    {
        private readonly LogPanelViewModel _logPanelViewModel;

        public BackGroundProcessConsole(LogPanelViewModel logPanelViewModel)
        {
            _logPanelViewModel = logPanelViewModel;
        }

        private string FormatLog(string message, params object[] formatArgs)
        {
            return string.Format(message, formatArgs);
        }

        private void AddLog(LogCategory category, string log)
        {
            _logPanelViewModel.AddLog(category, log);
        }

        public void WriteErrorLine(string message, params object[] formatArgs)
        {
            AddLog(LogCategory.Error, FormatLog($"[Error]: {message}", formatArgs));
        }

        public void WriteWarningLine(string message, params object[] formatArgs)
        {
            AddLog(LogCategory.Warning, FormatLog($"[Warning]: {message}", formatArgs));
        }

        public void WriteDiagnosticLine(string message, params object[] formatArgs)
        {
            AddLog(LogCategory.Diagnostic, FormatLog($"[Diagnose]: {message}", formatArgs));
        }

        public void WriteLine(string message, params object[] formatArgs)
        {
            AddLog(LogCategory.Default, FormatLog($"[IDS]: {message}", formatArgs));
        }
    }
}
