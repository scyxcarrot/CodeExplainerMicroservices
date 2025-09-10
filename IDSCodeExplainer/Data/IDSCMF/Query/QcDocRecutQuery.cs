using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Query
{
    public struct QcDocRecutData
    {
        public string PartName { get; private set; }
        public bool IsRecut { get; private set; }
        public double VolumeDifference { get; private set; }

        public QcDocRecutData(string partName, bool isRecut, double volumeDifference)
        {
            PartName = partName;
            IsRecut = isRecut;
            VolumeDifference = volumeDifference;
        }
    }

    public struct QcDocRecutModel
    {
        private QcDocRecutData _data;

        public QcDocRecutModel(QcDocRecutData data)
        {
            _data = data;
        }
        
        public string PartName => _data.PartName;
        public string IsRecut => _data.IsRecut ? "Changed" : "No change";
        public string VolumeDifference => _data.IsRecut ? string.Format(CultureInfo.InvariantCulture, "{0:F2}cc", _data.VolumeDifference) : "-";
    }
    
    public class QcDocRecutQuery
    {
        private readonly CMFImplantDirector _director;

        public QcDocRecutQuery(CMFImplantDirector director)
        {
            this._director = director;
        }

        public List<QcDocRecutModel> GenerateRecutModels()
        {
            var res = new List<QcDocRecutModel>();

            var objectManager = new CMFObjectManager(_director);
            var proPlanImports = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport).ToList();

            var datas = GenerateRecutDatas(proPlanImports);
            datas.ForEach(x =>
            {
                res.Add(new QcDocRecutModel(x));
            });

            return res;
        }

        public List<QcDocRecutData> GenerateRecutDatas(List<RhinoObject> proPlanImports)
        {
            var res = new List<QcDocRecutData>();

            var proPlanImportComponent = new ProPlanImportComponent();

            proPlanImports.ForEach(part =>
            {
                var partName = proPlanImportComponent.GetPartName(part.Name);
                var isRecut = part.Attributes.UserDictionary.ContainsKey(AttributeKeys.KeyIsRecut);
                var volumeDifference = GetVolumeDifferenceInCC(part);

                var info = new QcDocRecutData(partName, isRecut, volumeDifference);
                
                res.Add(info);
            });
            
            return res;
        }

        private double GetVolumeDifferenceInCC(RhinoObject part)
        {
            var keyOriginalVolume = "original_volume";

            if (part.Attributes.UserDictionary.ContainsKey(keyOriginalVolume))
            {
                var originalVolume = (double) part.Attributes.UserDictionary[keyOriginalVolume];
                var currentVolume = VolumeMassProperties.Compute((Mesh) part.Geometry).Volume;
                var volDiffInCubeMM = currentVolume - originalVolume;
                return volDiffInCubeMM / 1000;
            }

            return 0.0;
        }
    }
}
