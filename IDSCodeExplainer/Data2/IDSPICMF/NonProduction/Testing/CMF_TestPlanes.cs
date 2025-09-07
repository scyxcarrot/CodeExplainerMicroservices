#if (STAGING)

using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.NonProduction;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("CFB6CE1B-B24A-4412-A221-4E7900434B91")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestPlanes : CmfCommandBase
    {
        public CMF_TestPlanes()
        {
            Instance = this;
        }

        public static CMF_TestPlanes Instance { get; private set; }

        public override string EnglishName => "CMF_TestPlanes";

        private List<DisplayConduit> _displayConduits = new List<DisplayConduit>();

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var planes = new List<string>
            {
                "Sagittal",
                "Axial",
                "Coronal",
                "MidSagittal",
                "ToggleOFF"
            };

            var go = new GetOption();
            go.SetCommandPrompt("Choose plane OR toggle OFF");
            go.AcceptNothing(true);
            go.AddOptionList("PlaneORToggleOFF", planes, 0);

            var selectedPlane = planes[0];

            // Get user input
            while (true)
            {
                var result = go.Get();
                if (result == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (result == GetResult.Nothing)
                {
                    break;
                }

                if (result == GetResult.Option)
                {
                    selectedPlane = planes[go.Option().CurrentListOptionIndex];
                    break;
                }
            }

            var workingDir = DirectoryStructure.GetWorkingDir(doc) + "\\";

            switch (selectedPlane)
            {
                case "Sagittal":
                    View.SetSagittalView(doc);
                    SetupPlane(director.MedicalCoordinateSystem.SagittalPlane, selectedPlane, Color.Red, workingDir);
                    RhinoApp.WriteLine("SagittalPlane in RED");
                    break;
                case "Axial":
                    View.SetAxialView(doc);
                    SetupPlane(director.MedicalCoordinateSystem.AxialPlane, selectedPlane, Color.Green, workingDir);
                    RhinoApp.WriteLine("AxialPlane in GREEN");
                    break;
                case "Coronal":
                    View.SetCoronalView(doc);
                    SetupPlane(director.MedicalCoordinateSystem.CoronalPlane, selectedPlane, Color.Blue, workingDir);
                    RhinoApp.WriteLine("CoronalPlane in BLUE");
                    break;
                case "MidSagittal":
                    if (director.MedicalCoordinateSystem.MidSagittalPlane.IsValid)
                    {
                        SetupPlane(director.MedicalCoordinateSystem.MidSagittalPlane, selectedPlane, Color.Purple, workingDir);
                        RhinoApp.WriteLine("MidSagittalPlane in PURPLE");
                    }
                    else
                    {
                        RhinoApp.WriteLine("There is NO MidSagittalPlane info!");
                    }
                    break;
                case "ToggleOFF":
                    if (_displayConduits.Any())
                    {
                        foreach (var conduit in _displayConduits)
                        {
                            conduit.Enabled = false;
                        }

                        _displayConduits.Clear();
                        RhinoApp.WriteLine("Toggle OFF");
                    }
                    break;
            }

            return Result.Success;
        }

        private void SetupPlane(Plane plane, string planeName, Color color, string workingDir)
        {
            var size = 100;
            _displayConduits.Add(InternalUtilities.AddPlane(plane, color, size * 2));
            _displayConduits.Add(InternalUtilities.AddVector(plane.Origin, plane.Normal, size, color));
            InternalUtilities.ExportPlaneXml(plane, planeName, workingDir, size);
            var planeMesh = Mesh.CreateFromPlane(plane, new Interval(-size, size), new Interval(-size, size), 10, 10);
            StlUtilities.RhinoMesh2StlBinary(planeMesh, $@"{workingDir}\{planeName}.stl");
        }
    }
}

#endif