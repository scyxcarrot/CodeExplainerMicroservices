using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Logics;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Query
{
    public struct QcDocAnatomicalObstacleData
    {
        public string PartName { get; private set; }
        public string PreOpPart { get; private set; }
        public string OriginalPart { get; private set; }
        public string PlannedPart { get; set; }
        public bool IsPreOpPartAnatomicalObstacle { get; private set; }
        public bool IsOriginalPartAnatomicalObstacle { get; private set; }
        public bool IsPlannedPartAnatomicalObstacle { get; private set; }

        public QcDocAnatomicalObstacleData(string partName, string preOpPart, string originalPart, string plannedPart,
            bool isPreOpPartAnatomicalObstacle, bool isOriginalPartAnatomicalObstacle,
            bool isPlannedPartAnatomicalObstacle)
        {
            PartName = partName;
            PreOpPart = preOpPart;
            OriginalPart = originalPart;
            PlannedPart = plannedPart;
            IsPreOpPartAnatomicalObstacle = isPreOpPartAnatomicalObstacle;
            IsOriginalPartAnatomicalObstacle = isOriginalPartAnatomicalObstacle;
            IsPlannedPartAnatomicalObstacle = isPlannedPartAnatomicalObstacle;
        }
    }

    public struct QcDocAnatomicalObstacleModel
    {
        private QcDocAnatomicalObstacleData _data;

        public QcDocAnatomicalObstacleModel(QcDocAnatomicalObstacleData data)
        {
            _data = data;
        }
        
        public string PartName => _data.PartName;
        public string PreOpPart => _data.PreOpPart;
        public string OriginalPart => _data.OriginalPart;
        public string PlannedPart => _data.PlannedPart;
        public bool IsPreOpPartAnatomicalObstacle => _data.IsPreOpPartAnatomicalObstacle;
        public bool IsOriginalPartAnatomicalObstacle => _data.IsOriginalPartAnatomicalObstacle;
        public bool IsPlannedPartAnatomicalObstacle => _data.IsPlannedPartAnatomicalObstacle;
    }
    
    public class QcDocAnatomicalObstacleQuery
    {
        private readonly CMFImplantDirector director;

        public QcDocAnatomicalObstacleQuery(CMFImplantDirector director)
        {
            this.director = director;
        }

        public List<QcDocAnatomicalObstacleModel> GenerateAnatomicalObstacleModels()
        {
            var res = new List<QcDocAnatomicalObstacleModel>();

            var objectManager = new CMFObjectManager(director);
            var proPlanImports = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport).ToList();
            var anatomicalObstacles = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles).ToList();

            var datas = GenerateAnatomicalObstacleDatas(proPlanImports, anatomicalObstacles);
            datas.ForEach(x =>
            {
                res.Add(new QcDocAnatomicalObstacleModel(x));
            });

            return res;
        }

        public List<QcDocAnatomicalObstacleData> GenerateAnatomicalObstacleDatas(List<RhinoObject> proPlanImports, List<RhinoObject> anatomicalObstacles)
        {
            var res = new List<QcDocAnatomicalObstacleData>();

            var proPlanImportComponent = new ProPlanImportComponent();

            var groups = proPlanImports.GroupBy(
                part => ProPlanPartsUtilitiesV2.GetPartNameWithoutSurgeryStage(proPlanImportComponent.GetPartName(part.Name)).ToLower(),
                part => part,
                (partName, parts) => new { PartName = partName, List = parts.ToList() }
                );
            foreach (var group in groups)
            {
                var preOpPart = GetPart(ProPlanPartsUtilitiesV2.IsPreopPart, group.List);
                var originalPart = GetPart(ProPlanPartsUtilitiesV2.IsOriginalPart, group.List);
                var plannedPart = GetPart(ProPlanPartsUtilitiesV2.IsPlannedPart, group.List);

                var partName = group.PartName;
                var preOpPartString = preOpPart == null ? string.Empty : proPlanImportComponent.GetPartName(preOpPart.Name);
                var originalPartString = originalPart == null ? string.Empty : proPlanImportComponent.GetPartName(originalPart.Name);
                var plannedPartString = plannedPart == null ? string.Empty : proPlanImportComponent.GetPartName(plannedPart.Name);
                var isPreOpPartAnatomicalObstacle = IsAnatomicalObstacle(anatomicalObstacles, preOpPart);
                var isOriginalPartAnatomicalObstacle = IsAnatomicalObstacle(anatomicalObstacles, originalPart);
                var isPlannedPartAnatomicalObstacle = IsAnatomicalObstacle(anatomicalObstacles, plannedPart);

                var info = new QcDocAnatomicalObstacleData(partName, preOpPartString, originalPartString, plannedPartString, 
                    isPreOpPartAnatomicalObstacle, isOriginalPartAnatomicalObstacle, isPlannedPartAnatomicalObstacle);

                res.Add(info);
            }
            
            return res;
        }

        private RhinoObject GetPart(Func<string, bool> func, List<RhinoObject> parts)
        {
            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var part in parts)
            {
                var partName = proPlanImportComponent.GetPartName(part.Name);                
                if (func(partName))
                {
                    return part;
                }
            }

            return null;
        }

        private bool IsAnatomicalObstacle(List<RhinoObject> anatomicalObstacles, RhinoObject part)
        {
            if (!anatomicalObstacles.Any() || part == null)
            {
                return false;
            }

            var found = AnatomicalObstacleUtilities.GetAnatomicalObstacle(anatomicalObstacles, part);
            return found != null;
        }
    }
}
