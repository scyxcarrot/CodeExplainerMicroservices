using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Graph;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using Rhino.Collections;
using System;
using System.Collections.Generic;

namespace IDS.CMF.CasePreferences
{
    public class CasePreferenceDataModel : ICaseData, ISerializable<ArchivableDictionary>, IValueDataError, IDisposable
    {
        public string SerializationLabel => "CasePreferenceDataModel";

        private readonly string KeyCaseGuid = "CaseGuid";
        private readonly string KeyNCase = "NCase";
        private readonly string KeyCaseName = "CaseName";       
        private readonly string KeyImplantDataModel = "ImplantDataModel";

        //Guid should be the same as the one in case preference panel.
        public Guid CaseGuid { get; protected set; }
        public bool IsActive { get; set; }
        public int NCase { get; set; }
        public string CaseName { get; set; }

        public virtual void SetCaseNumber(int number)
        {
        }

        public delegate void OnImplantDataModelChange(List<IConnection> newConnections);

        public OnImplantDataModelChange ImplantDataModelChanged { get; set; }

        public CMFGraph Graph { get; set; }

        //private readonly ImporterViaRunScript importer = new ImporterViaRunScript();
        private ScrewAideDataModel _screwAideData;
        public ScrewAideDataModel ScrewAideData
        {
            get
            {
                if (_screwAideData == null)
                {
                    _screwAideData = new ScrewAideDataModel(CasePrefData.ScrewTypeValue);
                    _screwAideData.Update();
                }
                return _screwAideData;
            }
            protected set
            {
                _screwAideData = value;
            }
        }

        private BarrelAideDataModel _barrelAideData;
        public BarrelAideDataModel BarrelAideData
        {
            get
            {
                if (_barrelAideData == null)
                {
                    _barrelAideData = new BarrelAideDataModel(CasePrefData.ScrewTypeValue, CasePrefData.BarrelTypeValue);
                    _barrelAideData.Update();
                }
                return _barrelAideData;
            }
            protected set
            {
                _barrelAideData = value;
            }
        }

        public CasePreferenceData CasePrefData { get; set; }

        private ImplantDataModel _implantDataModel;
        public ImplantDataModel ImplantDataModel 
        {
            get
            {
                if (_implantDataModel == null)
                {
                    _implantDataModel = new ImplantDataModel(new List<IConnection>());
                }

                return _implantDataModel;
            }
            set
            {
                ImplantDataModelChanged?.Invoke(value.ConnectionList);
                _implantDataModel = value;
            }
        }
        public bool HasPlateThicknessError { get; set; }
        public bool HasPlateWidthError { get; set; }
        public bool HasLinkWidthError { get; set; }

        public CasePreferenceDataModel(Guid guid)
        {
            CaseGuid = guid;
            ImplantDataModel = new ImplantDataModel();
        }

        public void InvalidateGraph(CMFImplantDirector director)
        {
            if (Graph != null)
            {
                Graph.UnsubscribeForGraphInvalidation();
                Graph = null;
            }

            Graph = new CMFGraph(director, this);
            Graph.SubscribeForGraphInvalidation();
        }

        public void InvalidateEvents(CMFImplantDirector director)
        {
            if (ImplantDataModelChanged != null)
            {
                ImplantDataModelChanged = null;
            }

            ImplantDataModelChanged += (newConnections) => director.ImplantManager.UpdateIDotAndIConnection(this, newConnections);

            ImplantDataModel.ConnectionListChanged = ImplantDataModelChanged;
        }

        public void Dispose()
        {
            if (ImplantDataModelChanged != null)
            {
                ImplantDataModelChanged = null;
            }

            if (ImplantDataModel.ConnectionListChanged != null)
            {
                ImplantDataModel.ConnectionListChanged = null;
            }
        }

        public void UpdateScrewAide()
        {
            //screw aide brep might be null when load existing file because import command been blocked at that moment.
            ScrewAideData = new ScrewAideDataModel(CasePrefData.ScrewTypeValue);
            ScrewAideData.Update();
        }

        public void UpdateBarrelAide()
        {
            BarrelAideData = new BarrelAideDataModel(CasePrefData.ScrewTypeValue, CasePrefData.BarrelTypeValue);
            BarrelAideData.Update();
        }

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(KeyCaseGuid, CaseGuid);

            serializer.Set(KeyNCase, NCase);
            serializer.Set(KeyCaseName, CaseName);
            if (CasePrefData != null)
            {
                CasePrefData.Serialize(serializer);
            }

            if (ImplantDataModel != null)
            {
                var impDataModelArc = SerializationFactory.CreateSerializedArchive(ImplantDataModel);
                serializer.Set(KeyImplantDataModel, impDataModelArc);
            }
            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            CaseGuid = serializer.GetGuid(KeyCaseGuid);
            IsActive = false;

            NCase = serializer.GetInteger(KeyNCase);
            CaseName = RhinoIOUtilities.GetStringValue(serializer, KeyCaseName);

#if (INTERNAL)
            //TODO: Remove before release
            //temporary fixes for existing file due to changes to implant type name
            CaseName = CaseName.Replace("Lefort1", "Lefort");
            CaseName = CaseName.Replace("BSSO Single", "BSSOSingle");
            CaseName = CaseName.Replace("BSSO Double", "BSSODouble");
            CaseName = CaseName.Replace("Short Osteotomy", "ShortOsteotomy");
            CaseName = CaseName.Replace("Small Mandible", "MandibleSmall");
            CaseName = CaseName.Replace("Large Mandible", "MandibleLarge");
#endif

            CasePrefData = new CasePreferenceData();
            CasePrefData.DeSerialize(serializer);

            if (serializer.ContainsKey(KeyImplantDataModel))
            {
                ImplantDataModel = new ImplantDataModel(new List<IConnection>());
                ImplantDataModel.DeSerialize((ArchivableDictionary)serializer[KeyImplantDataModel]);
            }

            return true;
        }

        public bool IsValid()
        {
            return !HasPlateThicknessError && !HasPlateWidthError && !HasLinkWidthError;
        }
    }
}
