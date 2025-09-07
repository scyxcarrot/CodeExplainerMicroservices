using IDS.CMF.Forms;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class ProPlanCheckViewModel : INotifyPropertyChanged, IDisposable
    {

        private ObservableCollection<DisplayStringModel> _sppcObjectNames;
        public ObservableCollection<DisplayStringModel> SppcObjectNames
        {
            get { return _sppcObjectNames; }
            set
            {
                _sppcObjectNames = value;
                OnPropertyChanged("SppcObjectNames");
            }
        }

        public ProPlanCheckViewModel()
        {
            SppcObjectNames = new ObservableCollection<DisplayStringModel>();
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
