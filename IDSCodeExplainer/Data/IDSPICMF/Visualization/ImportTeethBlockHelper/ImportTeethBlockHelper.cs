using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Logic;
using IDS.RhinoInterface.Converter;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Visualization
{
    public class ImportTeethBlockHelper : IImportTeethBlockHelper
    {
        private readonly ExtendedImplantBuildingBlock _extendedImplantBuildingBlock;
        private readonly CMFObjectManager _objectManager;
        private readonly IDSRhinoConsole _idsConsole;
        private readonly string _scriptedImportFilePath;

        public ImportTeethBlockHelper(CMFImplantDirector director,
            IDSRhinoConsole idsConsole,
            ExtendedImplantBuildingBlock extendedImplantBuildingBlock,
            string scriptedImportFilePath = "")
        {
            _objectManager = new CMFObjectManager(director);
            _idsConsole = idsConsole;
            _extendedImplantBuildingBlock = extendedImplantBuildingBlock;
            _scriptedImportFilePath = scriptedImportFilePath;
        }

        // this method prepares the parameters for both interactive and scripted runs
        public LogicStatus PrepareLogicParameters(out ImportTeethBlockParameters parameter)
        {
            parameter = new ImportTeethBlockParameters();

            if (!string.IsNullOrEmpty(_scriptedImportFilePath))
            {
                parameter.FileNames = new[] { _scriptedImportFilePath };
            }
            else
            {
                var fileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Title = @"Please select an STL file",
                    Filter = @"STL files (*.stl)|*.stl||",
                    InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
                };

                var result = fileDialog.ShowDialog();
                if (result != DialogResult.OK || fileDialog.FileNames.Any(n => Path.GetExtension(n).ToLower() != ".stl"))
                {
                    return LogicStatus.Cancel;
                }

                parameter.FileNames = fileDialog.FileNames;
            }
            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(ImportTeethBlockResults result)
        {
            try
            {
                foreach (var buildingBlockIds in _objectManager.GetAllBuildingBlockIds(_extendedImplantBuildingBlock))
                {
                    _objectManager.DeleteObject(buildingBlockIds);
                }

                foreach (var mesh in result.Meshes.Values)
                {
                    _objectManager.AddNewBuildingBlock(_extendedImplantBuildingBlock, RhinoMeshConverter.ToRhinoMesh(mesh));
                    var rhinoDoc = _objectManager.GetDirector().Document;

                    var fullLayerPath = _extendedImplantBuildingBlock.Block.Layer;
                    var index = rhinoDoc.Layers.FindByFullPath(fullLayerPath, -1);

                    if (index >= 0)
                    {
                        var newLayerSettings = rhinoDoc.Layers[index];
                        if (!newLayerSettings.IsVisible && newLayerSettings.IsValid)
                        {
                            rhinoDoc.Layers.ForceLayerVisible(newLayerSettings.Id);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _idsConsole.WriteErrorLine($"Exception caught while importing Teeth Block: {exception}", "");
                return LogicStatus.Failure;
            }
            return LogicStatus.Success;
        }
    }
}