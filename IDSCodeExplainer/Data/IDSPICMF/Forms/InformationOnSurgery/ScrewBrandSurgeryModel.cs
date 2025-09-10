using IDS.CMF;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class ScrewBrandSurgeryModel : INotifyPropertyChanged
    {
        protected CMFImplantDirector _director;

        public EScrewBrand ScrewBrand
        {
            get
            {
                return _director?.CasePrefManager.SurgeryInformation.ScrewBrand ?? EScrewBrand.Synthes;
            }
            set
            {
                if (_director == null)
                {
                    return;
                }
                _director.CasePrefManager.SurgeryInformation.ScrewBrand = value;
                OnPropertyChanged("ScrewBrand");
                OnPropertyChanged("ScrewBrandDisplayString");
            }
        }

        public string ScrewBrandDisplayString => GeneralUtilities.ScrewBrandEnumToDisplayString(ScrewBrand);
        public string SurgeryTypeDisplayString => GeneralUtilities.SurgeryTypeEnumToDisplayString(SurgeryType);

        public ESurgeryType SurgeryType
        {
            get
            {
                return _director?.CasePrefManager.SurgeryInformation.SurgeryType ?? ESurgeryType.Orthognathic;
            }
            set
            {
                if (_director == null)
                {
                    return;
                }

                _director.CasePrefManager.SurgeryInformation.SurgeryType = value;
                OnPropertyChanged("SurgeryType");
            }
        }
        
        public ScrewBrandSurgeryModel(CMFImplantDirector director)
        {
            _director = director;
        }

        public ScrewBrandSurgeryModel()
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
