using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("B48BD705-822C-44C9-911E-E98F981EAAE4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestExportImplantScrewIntermediates : CmfCommandBase
    {
        public CMF_TestExportImplantScrewIntermediates()
        {
            Instance = this;
        }

        public static CMF_TestExportImplantScrewIntermediates Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportImplantScrewIntermediates";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;
            if (mode == RunMode.Scripted)
            {
                //skip prompts and get folder path from command line
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }
            }
            else
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = "Select a folder to export the implant screw intermediate stls";
                var rc = dialog.ShowDialog();
                if (rc != DialogResult.OK)
                {
                    return Result.Cancel;
                }

                folderPath = Path.GetFullPath(dialog.SelectedPath);
            }
            
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected folder: {folderPath}");

            var objectManager = new CMFObjectManager(director);

            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(s => s as Screw);
            if (screws.Any(s => s.Index <= 0))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please assign screw numbers!");
                return Result.Failure;
            }

            var console = new IDSRhinoConsole();
            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                var implantComponentEiBB = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
                var implantScrews = objectManager.GetAllBuildingBlocks(implantComponentEiBB).Select(s => s as Screw);

                foreach (var screw in implantScrews)
                {
                    var index = $"I{casePreferenceData.NCase}_{screw.Index}";
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(screw.BrepGeometry, true), $"{folderPath}\\ImplantScrew-{index}.stl");

                    var container = screw.GetScrewContainer();
                    //InternalUtilities.AddObject(container, $"Testing::ImplantScrewContainer-{index}");
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(container, true), $"{folderPath}\\Container-{index}.stl");

                    var stamp = screw.GetScrewStamp();
                    //InternalUtilities.AddObject(stamp, $"Testing::ImplantScrewStamp-{index}");
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(stamp, true), $"{folderPath}\\Stamp-{index}.stl");

                    var casePreference = objectManager.GetCasePreference(screw);
                    var pastille = casePreference.ImplantDataModel.DotList.Where(dot => (dot as DotPastille)?.Screw != null && screw.Id == (dot as DotPastille).Screw.Id).FirstOrDefault();
                    var centerOfRotation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
                    var referenceDirection = RhinoVector3dConverter.ToVector3d(pastille.Direction);
                    var cone = CreateCone(centerOfRotation, -referenceDirection, (screw.HeadPoint - screw.TipPoint).Length);
                    var coneBrep = cone.ToBrep(true);
                    //InternalUtilities.AddObject(coneBrep, $"Testing::ImplantScrewCone-{index}");
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(coneBrep, true), $"{folderPath}\\Cone-{index}.stl");

                    var qcCylinder = ScrewQcUtilities.GenerateQcScrewCylinderBrep(screw);
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(qcCylinder, true), $"{folderPath}\\QcScrewCylinder-{index}.stl");
                    StlUtilities.RhinoMesh2StlBinary(MeshUtilities.ConvertBrepToMesh(qcCylinder, true, MeshParameters.GetForScrewMinDistanceCheck()), $"{folderPath}\\QcScrewCylinderForScrewMinDistanceCheck-{index}.stl");

                    var qcCapsule = ScrewQcUtilities.GenerateQcScrewCapsuleMesh(console, screw);
                    StlUtilities.RhinoMesh2StlBinary(qcCapsule, $"{folderPath}\\QcScrewCapsule-{index}.stl");
                }
            }

            return Result.Success;
        }

        private Cone CreateCone(Point3d centerOfRotation, Vector3d referenceDirection, double length)
        {
            var plane = new Plane(centerOfRotation, referenceDirection);
            var trigoAngle = RhinoMath.ToRadians(15);
            var radius = Math.Tan(trigoAngle) * length;
            var cone = new Cone(plane, length, radius);
            return cone;
        }
    }

#endif
}
