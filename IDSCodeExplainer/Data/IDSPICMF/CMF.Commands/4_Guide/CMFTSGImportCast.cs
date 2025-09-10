using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometry;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("B4364DAB-FC1B-4155-8AA9-C4641B20B38D")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFTSGImportCast : CmfCommandBase
    {
        public CMFTSGImportCast()
        {
            TheCommand = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static CMFTSGImportCast TheCommand { get; private set; }

        public override string EnglishName => "CMFTSGImportCast";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var filePath = string.Empty;
            if (!GetFilePath(out filePath))
            {
                return Result.Failure;
            }

            var castGuid = Guid.Empty;
            if (!StlUtilitiesV2.StlBinaryToIDSMesh(filePath, out var mesh))
            {
                return Result.Failure;
            }

            var castNamePattern = Path.GetFileNameWithoutExtension(filePath);
            var castObjectMesh = RhinoMeshConverter.ToRhinoMesh(mesh);
            var transform = Transform.Identity;
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            if (!proPlanImportComponent.IsExistsInProPlan(castNamePattern) || !proPlanImportComponent.IsCastPartType(castNamePattern))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Not a valid teeth cast components.");
                return Result.Failure;
            }

            if (!proPlanImportComponent.IsBlockRequired(castNamePattern))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Import not allowed: The part '{castNamePattern}' is not valid for import.");
                return Result.Failure;
            }

            var block = proPlanImportComponent.GetProPlanImportBuildingBlock(castNamePattern);
            var parentId = IdsDocumentUtilities.TSGRootGuid;
            //Remove existing cast object
            if (objectManager.HasBuildingBlock(block))
            {
                var existingCast = objectManager.GetBuildingBlockId(block);
                var rhinoObj = objectManager.GetBuildingBlock(block);
                CMFObjectManager.GetTransformationMatrixFromPart(rhinoObj, out transform);
                director.IdsDocument.Delete(existingCast);
            }

            castGuid = IdsDocumentUtilities.AddNewGeometryBuildingBlockWithTransform(
                objectManager,
                director.IdsDocument,
                block,
                parentId,
                castObjectMesh,
                transform);

            if (castGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to import cast object.");
                return Result.Failure;
            }

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                guidePreferenceDataModel.Graph.NotifyBuildingBlockHasChanged(
                    new[] { IBB.TeethBlock });
            }

            return Result.Success;
        }

        private static bool GetFilePath(out string stlCastFilePath)
        {
            var filePath = string.Empty;
            var getPath = new GetString();
            getPath.SetCommandPrompt("Enter filepath (press Enter for file browsing)");
            getPath.AcceptNothing(true);
            var pathResult = getPath.Get();

            if (pathResult == GetResult.Nothing || string.IsNullOrEmpty(getPath.StringResult()))
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "*.stl";
                openFileDialog.Title = "Select a file";

                if (openFileDialog.ShowOpenDialog())
                {
                    filePath = openFileDialog.FileName;
                }
            }
            else
            {
                filePath = getPath.StringResult();
            }

            if (File.Exists(filePath))
            {
                stlCastFilePath = filePath;
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, $"File not found.");
            stlCastFilePath = string.Empty;
            return false;
        }
    }
}