using IDS.CMF.DataModel;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class UpdatePlanningViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ImportCheckboxModel> _dataGridImport;
        public ObservableCollection<ImportCheckboxModel> DataGridImport
        {
            get { return _dataGridImport; }
            set
            {
                _dataGridImport = value;
                OnPropertyChanged("DataGridImport");
            }
        }

        #region INotifyPropertyChanged Members 

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}