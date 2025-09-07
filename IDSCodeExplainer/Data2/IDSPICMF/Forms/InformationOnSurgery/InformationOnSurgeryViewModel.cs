using IDS.CMF;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class InformationOnSurgeryViewModel : INotifyPropertyChanged, IDisposable
    {
        public InformationOnSurgeryModel _infoOnSurgery;
        public InformationOnSurgeryModel InfoOnSurgery
        {
            get
            {
                return _infoOnSurgery;
            }
            set
            {
                _infoOnSurgery = value;
                OnPropertyChanged("InfoOnSurgery");
            }
        }

        public void InvalidateUI()
        {
            if (PropertyChanged == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Information on Surgery control is not initialized for UI invalidation!");
                return;
            }

            OnPropertyChanged("InfoOnSurgery");
        }

        public InformationOnSurgeryViewModel(CMFImplantDirector director)
        {
            _infoOnSurgery = new InformationOnSurgeryModel(director);
            InfoOnSurgery.ScrewBrand = director.CasePrefManager.SurgeryInformation.ScrewBrand;
            InfoOnSurgery.SurgeryType = director.CasePrefManager.SurgeryInformation.SurgeryType;
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
