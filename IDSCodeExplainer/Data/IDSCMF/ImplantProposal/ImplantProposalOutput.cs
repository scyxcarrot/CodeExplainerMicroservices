using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.MTLS.Operation;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using System;
using System.Collections.Generic;

namespace IDS.CMF.ImplantProposal
{
    public class ImplantProposalOutput
    {
        private readonly CMFImplantDirector _director;

        public ImplantProposalOutput(CMFImplantDirector director)
        {
            _director = director;
        }

        public void CreateScrewsAndDotPastilles(
            AutoImplantProposalResult autoImplantProposalResult,
            ref ImplantPreferenceModel implantPreferenceModel)
        {
            var screwNumberDotPastilleMap = new Dictionary<int, DotPastille>();
            for (var index = 0;
                 index < autoImplantProposalResult.ScrewHeads.GetLength(0);
                 index++)
            {
                var pastilleDirection = new IDSVector3D(
                    autoImplantProposalResult.ScrewHeads[index, 0] - autoImplantProposalResult.ScrewTips[index, 0],
                    autoImplantProposalResult.ScrewHeads[index, 1] - autoImplantProposalResult.ScrewTips[index, 1],
                    autoImplantProposalResult.ScrewHeads[index, 2] - autoImplantProposalResult.ScrewTips[index, 2]);
                pastilleDirection.Unitize();
                var dotPastille = new DotPastille()
                {
                    Location = new IDSPoint3D(
                        autoImplantProposalResult.ScrewHeads[index, 0],
                        autoImplantProposalResult.ScrewHeads[index, 1],
                        autoImplantProposalResult.ScrewHeads[index, 2]),
                    Id = Guid.NewGuid(),
                    Diameter = implantPreferenceModel.CasePrefData.PastilleDiameter,
                    Thickness = implantPreferenceModel.CasePrefData.PlateThicknessMm,
                    Direction = pastilleDirection,
                };

                screwNumberDotPastilleMap.Add(
                    (int)autoImplantProposalResult.ScrewNumbers[index], dotPastille);
            }

            var connectionList = new List<IConnection>();
            for (var index = 0;
                 index < autoImplantProposalResult.LinkConnections.GetLength(0);
                 index++)
            {
                var connection = new ConnectionLink()
                {
                    A = screwNumberDotPastilleMap[(int)autoImplantProposalResult.LinkConnections[index, 0]],
                    B = screwNumberDotPastilleMap[(int)autoImplantProposalResult.LinkConnections[index, 1]],
                    Thickness = implantPreferenceModel.CasePrefData.PlateThicknessMm,
                    Width = implantPreferenceModel.CasePrefData.LinkWidthMm,
                    Id = Guid.NewGuid(),
                };

                connectionList.Add(connection);
            }

            for (var index = 0;
                 index < autoImplantProposalResult.PlateConnections.GetLength(0);
                 index++)
            {
                var connection = new ConnectionPlate()
                {
                    A = screwNumberDotPastilleMap[(int)autoImplantProposalResult.PlateConnections[index, 0]],
                    B = screwNumberDotPastilleMap[(int)autoImplantProposalResult.PlateConnections[index, 1]],
                    Thickness = implantPreferenceModel.CasePrefData.PlateThicknessMm,
                    Width = implantPreferenceModel.CasePrefData.PlateWidthMm,
                    Id = Guid.NewGuid(),
                };

                connectionList.Add(connection);
            }

            implantPreferenceModel.ImplantDataModel.Update(connectionList);
            _director.ImplantManager.HandleAddNewImplant(implantPreferenceModel, true);
            _director.ImplantManager.HandleAllPlanningImplantRelatedItemsInvalidation(implantPreferenceModel);

            var screwManager = new ScrewManager(_director);
            var allScrews = screwManager.GetAllScrews(false);
            foreach (var screw in allScrews)
            {
                foreach (var screwNumberDotPastilleKvp in screwNumberDotPastilleMap)
                {
                    var dotPastille = screwNumberDotPastilleKvp.Value;
                    if (dotPastille.Screw.Id == screw.Id)
                    {
                        screw.Index = screwNumberDotPastilleKvp.Key;
                        break;
                    }
                }
            }
        }
    }
}
