using IDS.Core.Enumerators;
using System;
using System.Windows.Media;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class LogViewModel
    {
        private static readonly Brush ErrorForegroundColor = Brushes.DarkRed;
        private static readonly Brush WarningForegroundColor = Brushes.DarkOrange;
        private static readonly Brush DiagnosticForegroundColor = Brushes.DimGray;
        private static readonly Brush DefaultForegroundColor = Brushes.Black;

        public string Log { get; }

        public Brush ForegroundColor { get; }

        public LogViewModel(LogCategory category, string log)
        {
            ForegroundColor = GetForegroundColor(category);
            Log = log;
        }
        private Brush GetForegroundColor(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Error:
                    return ErrorForegroundColor;
                case LogCategory.Warning:
                    return WarningForegroundColor;
                case LogCategory.Diagnostic:
                    return DiagnosticForegroundColor;
                case LogCategory.Default:
                    return DefaultForegroundColor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
