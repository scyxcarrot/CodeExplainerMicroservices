using IDS.Core.Enumerators;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class LogPanelViewModel : INotifyPropertyChanged
    {
        private const int MaxRowLog = -1;

        private readonly List<LogViewModel> _logs = new List<LogViewModel>();

        public ObservableCollection<LogViewModel> Logs
        {
            get
            {
                lock (_logs)
                {
                    return new ObservableCollection<LogViewModel>(_logs);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddLog(LogCategory category, string log)
        {
            lock (_logs)
            {
                _logs.Add(new LogViewModel(category, log));
                // If MaxRowLog <= 0, unlimited rows
                while (MaxRowLog > 0 && _logs.Count > MaxRowLog)
                {
                    _logs.RemoveAt(0);
                }
                OnPropertyChanged(nameof(Logs));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
