using IDS.CMF.V2.CasePreferences;
using System;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class InitializationViewModel : IDisposable, INotifyPropertyChanged
    {
        private EScrewBrand _screwBrand;
        public EScrewBrand ScrewBrand
        {
            get => _screwBrand;
            set
            {
                _screwBrand = value;
                OnPropertyChanged("ScrewBrand");
            }
        }

        private ESurgeryType _surgeryType;
        public ESurgeryType SurgeryType
        {
            get => _surgeryType;
            set
            {
                _surgeryType = value;
                OnPropertyChanged("SurgeryType");
            }
        }

        public InitializationViewModel() 
        {
            ScrewBrand = EScrewBrand.Synthes;
            SurgeryType = ESurgeryType.Orthognathic;
        }

        public void Dispose()
        {

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
