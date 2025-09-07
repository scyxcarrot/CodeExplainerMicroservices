using Rhino.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.PICMF.Commands
{
    public class CommandCounter: INotifyPropertyChanged
    {
        private static CommandCounter _instance;

        public static CommandCounter Instance => _instance ?? (_instance = new CommandCounter());

        private int _numCommandRunning;

        public bool NoCommandRunning => _numCommandRunning == 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CommandCounter()
        {
            _numCommandRunning = 0;
            Command.BeginCommand += Command_BeginCommand;
            Command.EndCommand += Command_EndCommand;
        }

        private void Command_BeginCommand(object sender, CommandEventArgs e)
        {
            _numCommandRunning++;
            OnPropertyChanged(nameof(NoCommandRunning));
        }

        private void Command_EndCommand(object sender, CommandEventArgs e)
        {
            _numCommandRunning = (_numCommandRunning == 0) ? 0 : _numCommandRunning - 1;
            OnPropertyChanged(nameof(NoCommandRunning));
        }
    }
}
