using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class GuidePreferenceControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CMFImplantDirector _director;

        private GuidePreferenceModel _model;

        public GuidePreferenceModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                OnPropertyChanged("Model");
            }
        }

        public void InvalidateUI()
        {
            if (PropertyChanged == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide Preference control is not initialized for UI invalidation!");
                return;
            }

            OnPropertyChanged("Model");
        }

        public GuidePreferenceControlViewModel(CMFImplantDirector director)
        {
            _director = director;
            Model = new GuidePreferenceModel(_director.ScrewBrandCasePreferences);
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
