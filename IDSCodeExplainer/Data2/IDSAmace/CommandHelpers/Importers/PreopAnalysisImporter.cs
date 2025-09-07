using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.Core.Visualization;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Amace.Operations
{
    /*
     * PreopAnalysisImporter provides functionality for importing preop analysis files
     */

    public class PreopAnalysisImporter
    {
        /*
         * Convert preop mat-file to a pickle file using CPython
         */

        public static bool ConvertPreopMat(string matFile, out string pickleFile)
        {
            // Specify command and arguments
            pickleFile = Path.Combine(Path.GetTempPath(), string.Format("IDS_MatToPickleConverter_{0}.p", Guid.NewGuid()));
            Resources resources = new Resources();
            string cPythonCommand =
                $"\"{resources.GetCPythonScriptPath("ReadPreopMatFile")}\" \"{matFile}\" \"{pickleFile}\"";

            bool convertedToPickle = ExternalToolInterop.RunCPythonScript(cPythonCommand);

            // Check if exited as expected
            if (!convertedToPickle)
            {
                RhinoApp.WriteLine("Importing pre-op data Failed...");
                return false;
            }

            // Success
            return true;
        }

        /**
         * Import data from a Python pickle file into an inspector object
         *   defined in the Python script.
         * @return       The Python PreOpInspector object used to access the loaded data
         */

        public bool ImportPythonData(RhinoDoc doc, string picklePath, out PreOpInspector inspector)
        {
            // init
            inspector = new PreOpInspector(doc);

            // Load/Compile the script if necessary
            PythonScript theScript = ExternalToolInterop.LoadIronPythonScript(doc, "ImportPreOpData.py");

            // Execute the python function to import data
            string pickleFile = picklePath.Replace(@"\", @"\\");
            string pycmd = "inspector = browseAndLoadData('" + pickleFile + "')";
            try
            {
                // Do the function call
                theScript.ExecuteScript(pycmd);

                // Get the result
                if (!theScript.ContainsVariable("inspector"))
                {
                    throw new IDSOperationFailed("Could not find the inspector variable in the PreOpInspector script scope");
                }
                inspector = theScript.GetVariable("inspector") as PreOpInspector;
                if (null == inspector)
                {
                    throw new IDSOperationFailed("Inspector is of unexpected type!");
                }
            }
            catch (IDSOperationFailed exc)
            {
                RhinoApp.WriteLine("Exception thrown during execution of python script: \n" + exc.ToString());
                return false;
            }

            // Postprocess the inspector
            inspector = PostProcessInspector(inspector);

            // Data was loaded successfully
            return true;
        }

        /*
         * Post-process the newly imported preop data
         */

        private static PreOpInspector PostProcessInspector(PreOpInspector inspector)
        {
            // Copy inspector
            PreOpInspector inspectorProcessed = inspector;

            // Setup transforms
            Transform.ChangeBasis(inspectorProcessed.AxialPlane, Plane.WorldXY);
            Transform.ChangeBasis(Plane.WorldXY, inspectorProcessed.AxialPlane);

            // Round COR values
            inspectorProcessed.ContralateralFemurCenterOfRotationAhjc = PointUtilities.RoundPointCoordinates(inspector.ContralateralFemurCenterOfRotationAhjc);
            inspectorProcessed.ContralateralPelvisCenterOfRotationAhjc = PointUtilities.RoundPointCoordinates(inspector.ContralateralPelvisCenterOfRotationAhjc);
            inspectorProcessed.ContralateralFemurCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.ContralateralFemurCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);
            inspectorProcessed.ContralateralPelvisCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.ContralateralPelvisCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);
            inspectorProcessed.ContralateralSsmCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.ContralateralSsmCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);
            inspectorProcessed.DefectFemurCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.DefectFemurCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);
            inspectorProcessed.DefectPelvisCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.DefectPelvisCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);
            inspectorProcessed.DefectSsmCenterOfRotation = 
                PointUtilities.RoundPointCoordinatesInOtherCoordinateSystem(inspector.DefectSsmCenterOfRotation, Plane.WorldXY, inspector.AxialPlane);

            // Success
            return inspectorProcessed;
        }

        /*
         * Setup the preop meshes in the document
         */

        public static void SetupPreopMeshes(ImplantDirector director)
        {
            // Init
            PreOpInspector inspector = director.Inspector;
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Defect pelvis and design pelvis are clones at import
            Guid defectPelvisId = objectManager.GetBuildingBlockId(IBB.DefectPelvis);
            Guid designPelvisId = objectManager.GetBuildingBlockId(IBB.DesignPelvis);
            Mesh defectPelvis = objectManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            objectManager.SetBuildingBlock(IBB.DefectPelvis, defectPelvis, defectPelvisId);
            objectManager.SetBuildingBlock(IBB.DesignPelvis, defectPelvis, designPelvisId);

            // Contralateral pelvis
            if (inspector.ContralateralMeshId != Guid.Empty)
            {
                Guid clatPelvisId = objectManager.GetBuildingBlockId(IBB.ContralateralPelvis);
                Mesh clatPelvis = objectManager.GetBuildingBlock(IBB.ContralateralPelvis).Geometry as Mesh;
                objectManager.SetBuildingBlock(IBB.ContralateralPelvis, clatPelvis, clatPelvisId);
            }

            // Contralateral femur
            if (inspector.ContralateralFemurId != Guid.Empty)
            {
                Guid clatFemurId = objectManager.GetBuildingBlockId(IBB.ContralateralFemur);
                Mesh clatFemur = objectManager.GetBuildingBlock(IBB.ContralateralFemur).Geometry as Mesh;
                objectManager.SetBuildingBlock(IBB.ContralateralFemur, clatFemur, clatFemurId);
            }

            // Defect femur
            if (inspector.DefectFemurId != Guid.Empty)
            {
                Guid defectFemurId = objectManager.GetBuildingBlockId(IBB.DefectFemur);
                Mesh defectFemur = objectManager.GetBuildingBlock(IBB.DefectFemur).Geometry as Mesh;
                objectManager.SetBuildingBlock(IBB.DefectFemur, defectFemur, defectFemurId);
            }
        }

        /*
         * Setup the thickness maps meshes in the document
         */

        public static void CreateThiBuildingBlocks(ImplantDirector director)
        {
            // Init
            RhinoDoc doc = director.Document;
            PreOpInspector inspector = director.Inspector;
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Get defect pelvis object
            RhinoObject pelvisObj = doc.Objects.Find(objectManager.GetBuildingBlockId(IBB.DefectPelvis));
            if (null == pelvisObj)
            {
                return;
            }

            // Init variables
            Mesh pelvisMesh;
            List<System.Drawing.Color> colors;

            // Cortex Thi
            if (inspector.DefectCortexThickness != null)
            {
                pelvisMesh = (Mesh)pelvisObj.Geometry.Duplicate();
                List<double> cortThi = inspector.DefectCortexThickness.ToList();
                if (cortThi.Count != pelvisMesh.Vertices.Count)
                {
                    return;
                }
                colors = DrawUtilitiesV2.GetColors(cortThi, inspector.DefectCortexThicknessMinimum, 
                    inspector.DefectCortexThicknessMaximum, DrawUtilities.GetColorScale(ColorMap.Jet));
                pelvisMesh.VertexColors.SetColors(colors.ToArray());
                Guid thiCortexId = objectManager.GetBuildingBlockId(IBB.DefectPelvisTHICortex);
                objectManager.SetBuildingBlock(IBB.DefectPelvisTHICortex, pelvisMesh, thiCortexId);
            }

            // Wall Thi
            if (inspector.DefectWallThickness != null)
            {
                pelvisMesh = (Mesh)pelvisObj.Geometry.Duplicate();
                List<double> wallThi = inspector.DefectWallThickness.ToList();
                if (wallThi.Count != pelvisMesh.Vertices.Count)
                {
                    return;
                }
                colors = DrawUtilitiesV2.GetColors(wallThi, 0.0, 40.0, DrawUtilities.GetColorScale(ColorMap.Jet));
                pelvisMesh.VertexColors.SetColors(colors.ToArray());
                Guid thiWallId = objectManager.GetBuildingBlockId(IBB.DefectPelvisTHIWall);
                objectManager.SetBuildingBlock(IBB.DefectPelvisTHIWall, pelvisMesh, thiWallId);
            }

            // Bone Quality
            if (inspector.DefectBoneQuality != null)
            {
                pelvisMesh = (Mesh)pelvisObj.Geometry.Duplicate();
                List<double> boneQual = inspector.DefectBoneQuality.ToList();
                if (boneQual.Count != pelvisMesh.Vertices.Count)
                {
                    return;
                }
                colors = DrawUtilitiesV2.GetColors(boneQual, 0.0, 1.0, DrawUtilities.GetColorScale(ColorMap.Quality));
                pelvisMesh.VertexColors.SetColors(colors.ToArray());
                Guid boneQualId = objectManager.GetBuildingBlockId(IBB.DefectPelvisTHIBQual);
                objectManager.SetBuildingBlock(IBB.DefectPelvisTHIBQual, pelvisMesh, boneQualId);
            }
        }
    }
}