using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class PlannedPositionedBarrelCalibrator
    {
        private readonly CMFImplantDirector director;

        public PlannedPositionedBarrelCalibrator(CMFImplantDirector director)
        {
            this.director = director;
        }
        
        public Dictionary<Screw, BrepProperties> CalibrateScrewsBarrelOnPlannedPosition(CasePreferenceDataModel casePreferenceData)
        {
            var screwBarrelList = new Dictionary<Screw, BrepProperties>();

            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var screwsObj = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

            var registrator = new CMFBarrelRegistrator(director);

            var query = new ConstraintMeshQuery(objectManager);

            var plannedBones = query.GetPlannedBones();

            var plannedBonesProperties = new List<MeshProperties>();

            foreach (var rhinoObj in plannedBones)
            {
                var mesh = ((Mesh) rhinoObj.Geometry).DuplicateMesh();
                var layerPath = rhinoObj.Document.Layers[rhinoObj.Attributes.LayerIndex].FullPath;
                plannedBonesProperties.Add(new MeshProperties(mesh, layerPath));
            }

            var implantSupportManager = new ImplantSupportManager(objectManager);
            var supportMesh = implantSupportManager.GetImplantSupportMesh(casePreferenceData);
            implantSupportManager.ImplantSupportNullCheck(supportMesh, casePreferenceData);

            var skippedLevelingScrewBarrels = new List<Screw>();

            foreach (var screwObj in screwsObj)
            {
                var screw = (Screw)screwObj;

                Transform alignmentTransform;
                bool isMeetingSpecs;
                bool isLevelingSkipped;
                PointUtilities.PointDistance distance;
                Curve leveledBarrelRef;

                //passing Transform.Identity as registrationTrans because no registration is needed
                var leveledBarrel = registrator.CalibrateBarrel(supportMesh, screw, Transform.Identity, true,
                    out alignmentTransform, out isMeetingSpecs, out distance, out leveledBarrelRef, out isLevelingSkipped);
                var color = CMFBarrelRegistrator.GetBarrelColor(director.CasePrefManager.SurgeryInformation.ScrewBrand, screw.ScrewType, isMeetingSpecs, distance.Distance);

                screwBarrelList.Add(screw, new BrepProperties(leveledBarrel, alignmentTransform, color));

                if (isLevelingSkipped)
                {
                    skippedLevelingScrewBarrels.Add(screw);
                }
            }

            if (skippedLevelingScrewBarrels.Any()) //Verrrry unlikely this will happen, exception could have thrown awayyyy before it reaches here. So it suppose to work if it reaches here.
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, 
                    $"Leveling are not carried out for {skippedLevelingScrewBarrels.Count} Planned Barrel(s) " +
                    $"usually because implant support mesh is invalid or not yet imported");
            }

            plannedBonesProperties.ForEach(x => x.Mesh.Dispose());
            plannedBonesProperties.Clear();

            registrator.Dispose();

            return screwBarrelList;
        }           
    }
}
