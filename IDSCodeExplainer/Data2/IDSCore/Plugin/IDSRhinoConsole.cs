using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Interface.Tools;

namespace IDS.Core.Plugin
{
    public class IDSRhinoConsole: IConsole
    {
        public void WriteErrorLine(string message, params object[] formatArgs)
        {
            IDSPluginHelper.WriteLine(LogCategory.Error, message, formatArgs);
        }

        public void WriteWarningLine(string message, params object[] formatArgs)
        {
            IDSPluginHelper.WriteLine(LogCategory.Warning, message, formatArgs);
        }

        public void WriteDiagnosticLine(string message, params object[] formatArgs)
        {
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, message, formatArgs);
        }

        public void WriteLine(string message, params object[] formatArgs)
        {
            IDSPluginHelper.WriteLine(LogCategory.Default, message, formatArgs);
        }
    }
}
