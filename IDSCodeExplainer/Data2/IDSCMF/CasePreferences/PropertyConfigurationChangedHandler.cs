using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using System;
using System.Linq;

namespace IDS.CMF.CasePreferences
{
    public class PropertyConfigurationChangedHandler
    {
        private readonly CMFImplantDirector _director;
        private readonly PropertyHandler _propertyHandler;

        public PropertyConfigurationChangedHandler(CMFImplantDirector director)
        {
            _director = director;
            _propertyHandler = new PropertyHandler(director);
        }

        public void HandleImplantPropertyValueChanged()
        {
            var anyChanged = false;

            foreach (var casePreferenceDataModel in _director.CasePrefManager.CasePreferences)
            {
                var pastilleDiameterChanged = HandlePastilleDiameterChanged(casePreferenceDataModel);
                var plateThicknessChanged = HandlePlateThicknessChanged(casePreferenceDataModel);
                var plateWidthChanged = HandlePlateWidthChanged(casePreferenceDataModel);
                var linkWidthChanged = HandleLinkWidthChanged(casePreferenceDataModel);
                var plateWidthRangeChanged = HandlePlateWidthRangeChanged(casePreferenceDataModel);
                var linkWidthRangeChanged = HandleLinkWidthRangeChanged(casePreferenceDataModel);

                if (!pastilleDiameterChanged && !plateThicknessChanged && !plateWidthChanged && !linkWidthChanged && !plateWidthRangeChanged && !linkWidthRangeChanged)
                {
                    continue;
                }

                var data = (ImplantPreferenceModel) casePreferenceDataModel;

                if (pastilleDiameterChanged || plateThicknessChanged)
                {
                    //update implant planning and also pastille
                    _propertyHandler.UpdateImplantPlanning(data, true);
                }
                else
                {
                    //update implant planning and without updating pastille
                    _propertyHandler.UpdateImplantPlanning(data, false);
                }

                if (plateThicknessChanged)
                {
                    //plate thickness changed requires releveling of screws
                    _propertyHandler.RecalibrateImplantScrews(data, data.SelectedScrewType, false, true);
                }

                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Implant property value changed in {casePreferenceDataModel.CaseName}:" +
                    $"\nPastilleDiameterChanged={pastilleDiameterChanged}" +
                    $"\nPlateThicknessChanged={plateThicknessChanged}" +
                    $"\nPlateWidthChanged={plateWidthChanged}" +
                    $"\nLinkWidthChanged={linkWidthChanged}" +
                    $"\nPlateWidthRangeChanged={plateWidthRangeChanged}" +
                    $"\nLinkWidthRangeChanged={linkWidthRangeChanged}");

                anyChanged = true;
            }

            if (!anyChanged)
            {
                return;
            }

            _director.Document.ClearUndoRecords(true);
            _director.Document.ClearRedoRecords();
            RhinoLayerUtilities.DeleteEmptyLayers(_director.Document);
        }

        private bool HandlePastilleDiameterChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var newPastilleDiameter = Queries.PastilleDiameter(_director.ScrewBrandCasePreferences.ScrewBrand, casePreferenceData.ImplantTypeValue, casePreferenceData.ScrewTypeValue);
            if (Math.Abs(casePreferenceData.PastilleDiameter - newPastilleDiameter) < Constants.DistanceParameters.Epsilon3Decimal)
            {
                return false;
            }

            casePreferenceData.PastilleDiameter = newPastilleDiameter;
            _propertyHandler.HandleDotPastilleChanged((ImplantPreferenceModel) casePreferenceDataModel, false);

            return true;
        }

        public bool HandlePlateThicknessChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var implant = _director.ScrewBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == casePreferenceData.ImplantTypeValue);
            var newPlateThickness = implant.PlateThickness;
            if (Math.Abs(casePreferenceData.PlateThicknessMm - newPlateThickness) < Constants.DistanceParameters.Epsilon3Decimal)
            {
                return false;
            }

            var newPlateThicknessMin = implant.PlateThicknessMin;
            var newPlateThicknessMax = implant.PlateThicknessMax;
            if (casePreferenceData.PlateThicknessMm >= newPlateThicknessMin && casePreferenceData.PlateThicknessMm <= newPlateThicknessMax)
            {
                return false;
            }

            casePreferenceData.PlateThicknessMm = newPlateThickness;
            _propertyHandler.HandlePlateThicknessChanged((ImplantPreferenceModel) casePreferenceDataModel, false);
            return true;
        }

        public bool HandlePlateWidthChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var implant = _director.ScrewBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == casePreferenceData.ImplantTypeValue);
            var newPlateWidth = implant.PlateWidth;
            if (Math.Abs(casePreferenceData.PlateWidthMm - newPlateWidth) < Constants.DistanceParameters.Epsilon3Decimal)
            {
                return false;
            }

            var newPlateWidthMin = implant.PlateWidthMin;
            var newPlateWidthMax = implant.PlateWidthMax;
            if (casePreferenceData.PlateWidthMm >= newPlateWidthMin && casePreferenceData.PlateWidthMm <= newPlateWidthMax)
            {
                return false;
            }

            casePreferenceData.PlateWidthMm = newPlateWidth;
            _propertyHandler.HandlePlateWidthChanged((ImplantPreferenceModel) casePreferenceDataModel, false);
            return true;
        }

        public bool HandleLinkWidthChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var implant = _director.ScrewBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == casePreferenceData.ImplantTypeValue);
            var newLinkWidth = implant.LinkWidth;
            if (Math.Abs(casePreferenceData.LinkWidthMm - newLinkWidth) < Constants.DistanceParameters.Epsilon3Decimal)
            {
                return false;
            }

            var newLinkWidthMin = implant.LinkWidthMin;
            var newLinkWidthMax = implant.LinkWidthMax;
            if (casePreferenceData.LinkWidthMm >= newLinkWidthMin && casePreferenceData.LinkWidthMm <= newLinkWidthMax)
            {
                return false;
            }

            casePreferenceData.LinkWidthMm = newLinkWidth;
            _propertyHandler.HandleLinkWidthChanged((ImplantPreferenceModel) casePreferenceDataModel, false);
            return true;
        }

        public bool HandlePlateWidthRangeChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var outOfRangePlates = casePreferenceDataModel.ImplantDataModel.ConnectionList.Where(c =>
                c is ConnectionPlate &&
                !(c.Width >= ImplantParameters.OverrideConnectionMinWidth && c.Width <= ImplantParameters.OverrideConnectionMaxWidth)).ToList();
            if (!outOfRangePlates.Any())
            {
                return false;
            }

            var newPlateWidth = casePreferenceData.PlateWidthMm;
            outOfRangePlates.ForEach(c => c.Width = newPlateWidth);

            return true;
        }

        public bool HandleLinkWidthRangeChanged(CasePreferenceDataModel casePreferenceDataModel)
        {
            var casePreferenceData = casePreferenceDataModel.CasePrefData;

            var outOfRangeLinks = casePreferenceDataModel.ImplantDataModel.ConnectionList.Where(c =>
                c is ConnectionLink &&
                !(c.Width >= ImplantParameters.OverrideConnectionMinWidth && c.Width <= ImplantParameters.OverrideConnectionMaxWidth)).ToList();
            if (!outOfRangeLinks.Any())
            {
                return false;
            }

            var newLinkWidth = casePreferenceData.LinkWidthMm;
            outOfRangeLinks.ForEach(c => c.Width = newLinkWidth);

            return true;
        }
    }
}
