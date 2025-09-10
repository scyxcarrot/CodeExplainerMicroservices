using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.PICMF.Forms.AutoDeployment
{
    public class DownloadProgressDataModel: INotifyPropertyChanged, IProgress<double>
    {
        public double Value { get; private set; }

        public string ProgressText { get; }

        public DownloadProgressDataModel(string progressText)
        {
            Value = 0.0;
            ProgressText = progressText;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Report(double value)
        {
            if (value > Value)
            {
                Value = value;
            }
            OnPropertyChanged(nameof(Value));
        }
    }
}
