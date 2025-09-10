using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Enumerators;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System;
using System.ComponentModel;

namespace IDS.PICMF.Forms
{
    public class ImplantPreferenceControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CMFImplantDirector _director;

        private ImplantPreferenceModel _model;

        public ImplantPreferenceModel Model
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
                IDSPluginHelper.WriteLine(LogCategory.Error, "Case Preference control is not initialized for UI invalidation!");
                return;
            }

            OnPropertyChanged("Model");
        }

        public ImplantPreferenceControlViewModel(CMFImplantDirector director)
        {
            _director = director;
            Model = new ImplantPreferenceModel(_director.CasePrefManager.SurgeryInformation.SurgeryType, _director.ScrewBrandCasePreferences, _director.ScrewLengthsPreferences);

            _director.CasePrefManager.OnCaseActivateEventHandler = data =>
            {
                var casePrefModel = (ImplantPreferenceModel)data;
                casePrefModel.NotifyIsCasePanelActiveChanged();
            };

            _director.CasePrefManager.OnCaseDeactivateEventHandler = data =>
            {
                var casePrefModel = (ImplantPreferenceModel)data;
                casePrefModel.NotifyIsCasePanelActiveChanged();
            };
        }

        public bool ActivateCase()
        {
            return _director.CasePrefManager.ActivateCase(Model,
                data =>
                {
                    Model.IsCasePanelActive = true;

                    if (_director.CurrentDesignPhase == DesignPhase.Planning)
                    {
                        Model.IsCasePanelFieldEditable = true;
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"{data.CaseName} is activated!");
                },
                data =>
                {
                    var casePrefModel = (ImplantPreferenceModel)data;
                    casePrefModel.IsCasePanelActive = false;
                    if (_director.CurrentDesignPhase == DesignPhase.Planning)
                    {
                        casePrefModel.IsCasePanelFieldEditable = true;
                    }
                    else
                    {
                        casePrefModel.IsCasePanelFieldEditable = false;
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"{data.CaseName} is deactivated.");
                }
                );
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
