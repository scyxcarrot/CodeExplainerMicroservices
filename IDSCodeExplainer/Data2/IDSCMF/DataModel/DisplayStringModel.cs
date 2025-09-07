using System;
using System.ComponentModel;

namespace IDS.CMF.Forms
{
    public class DisplayStringModel : INotifyPropertyChanged, IDisposable
    {
        private string _displayString;
        public string DisplayString
        {
            get { return _displayString; }
            set
            {
                _displayString = value;
                OnPropertyChanged("DisplayString");
            }
        }

        private string _displayGroup;
        public string DisplayGroup
        {
            get { return _displayGroup; }
            set
            {
                _displayGroup = value;
                OnPropertyChanged("DisplayGroup");
            }
        }

        public DisplayStringModel()
        {
            DisplayString = "";
            DisplayGroup = "";
        }

        #region INotifyPropertyChanged Members 

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Dispose()
        {

        }
    }
}
