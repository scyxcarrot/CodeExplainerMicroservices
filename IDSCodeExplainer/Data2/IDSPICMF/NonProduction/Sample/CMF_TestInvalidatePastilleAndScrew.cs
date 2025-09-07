#if (INTERNAL)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.NonProduction;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("64682C95-3009-4C1E-894D-3939CB3F4A89")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ImplantSupport)]
    public class CMF_TestInvalidatePastilleAndScrew : CmfCommandBase
    {
        public CMF_TestInvalidatePastilleAndScrew()
        {
            Instance = this;
        }

        public static CMF_TestInvalidatePastilleAndScrew Instance { get; private set; }

        public override string EnglishName => "CMF_TestInvalidatePastilleAndScrew";
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Invalidate(director);
            return Result.Success;
        }
        
        private static void Invalidate(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw);

            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var constraintMesh = implantSupportManager.GetImplantSupportMesh(casePreference);
                if (constraintMesh == null)
                {
                    continue;
                }
                constraintMesh.FaceNormals.ComputeFaceNormals();

                var implantDataModel = casePreference.ImplantDataModel;
                if (implantDataModel != null && implantDataModel.DotList.Any())
                {
                    var pastilleList = implantDataModel.DotList.Where(dot => dot is DotPastille).ToList();

                    for (var i = 0; i < pastilleList.Count; i++)
                    {
                        var pastille = (DotPastille)pastilleList[i];
                        var existingLocation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);

                        var meshPoint = constraintMesh.ClosestMeshPoint(existingLocation, 1.0);
                        var newLocation = meshPoint.Point;
                        var averageNormal = VectorUtilities.FindAverageNormal(constraintMesh, newLocation, ScrewAngulationConstants.AverageNormalRadiusPastille);

                        var existingDirection = RhinoVector3dConverter.ToVector3d(pastille.Direction);
                        var newDirection = averageNormal;
                        pastille.Direction = RhinoVector3dConverter.ToIVector3D(newDirection);
                        pastille.Location = RhinoPoint3dConverter.ToIPoint3D(newLocation);

                        InternalUtilities.AddPoint(existingLocation, "Existing", Color.Red);
                        InternalUtilities.AddVector(existingLocation, existingDirection, 50, Color.Red);

                        InternalUtilities.AddPoint(newLocation, "New", Color.Blue);
                        InternalUtilities.AddVector(newLocation, newDirection, 50, Color.Blue);

                        var refScrew = (Screw)screws.First(s => s.Id == pastille.Screw.Id);
                        var headPoint = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                        var tipPoint = Point3d.Add(headPoint, Vector3d.Multiply(refScrew.Direction, refScrew.Length));
                        var screw = new Screw(director, headPoint, tipPoint, refScrew.ScrewAideDictionary, refScrew.Index, refScrew.ScrewType, refScrew.BarrelType);

                        var screwCalibrator = new ScrewCalibrator(constraintMesh);
                        if (screwCalibrator.LevelHeadOnTopOfMesh(screw, casePreference.CasePrefData.PlateThicknessMm, true))
                        {
                            screw = screwCalibrator.CalibratedScrew;
                        }

                        var screwData = new ScrewData
                        {
                            Id = pastille.Screw.Id
                        };

                        var screwManager = new ScrewManager(director);
                        screwManager.ReplaceExistingScrewInDocument(screw, ref refScrew, casePreference, true);

                        pastille.Screw = screwData;

                        director.ImplantManager.InvalidateConnectionBuildingBlock(casePreference);
                        director.ImplantManager.InvalidateLandmarkBuildingBlock(casePreference);
                        casePreference.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.Screw }, IBB.Connection, IBB.Landmark, IBB.RegisteredBarrel);
                    }
                }
            }
        }
    }
}
#endif