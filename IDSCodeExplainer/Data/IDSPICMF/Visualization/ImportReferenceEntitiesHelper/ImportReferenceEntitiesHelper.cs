using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Logic;
using IDS.RhinoInterface.Converter;
using Rhino.Commands;
using Rhino.Input;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Visualization
{
    public class ImportReferenceEntitiesHelper : IImportReferenceEntitiesHelper
    {
        private readonly CMFObjectManager _objectManager;
        private readonly RunMode _runMode;
        private readonly IDSRhinoConsole _idsConsole;

        public ImportReferenceEntitiesHelper(CMFImplantDirector director, RunMode runMode, IDSRhinoConsole idsConsole)
        {
            _objectManager = new CMFObjectManager(director);
            _runMode = runMode;
            _idsConsole = idsConsole;
        }

        // this method prepares the parameters for both interactive and scripted runs
        public LogicStatus PrepareLogicParameters(out ImportReferenceEntitiesParameters parameter)
        {
            parameter = new ImportReferenceEntitiesParameters();

            if (_runMode == RunMode.Scripted)
            {
                var strFilePaths = string.Empty;
                var result = RhinoGet.GetString("FilePaths", false, ref strFilePaths);

                if (result != Result.Success)
                {
                    _idsConsole.WriteErrorLine($"Invalid folder path: {strFilePaths}", "");
                    return LogicStatus.Failure;
                }

                // the filePaths value should be a "|" delimited file path
                parameter.FileNames = strFilePaths.Split('|');
            }
            else
            {
                var fileDialog = new OpenFileDialog
                {
                    Multiselect = true,
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

        public LogicStatus ProcessLogicResult(ImportReferenceEntitiesResults result)
        {
            try
            {
                foreach (var meshDict in result.Meshes)
                {
                    var filepath = meshDict.Key;
                    var filename = Path.GetFileNameWithoutExtension(filepath);
                    var mesh = meshDict.Value;

                    // create our own custom implant building block here
                    // because reference entity layer name now follows the stl filename instead
                    var cloneReferenceBuildingBlock = BuildingBlocks.Blocks[IBB.ReferenceEntities].Clone();
                    cloneReferenceBuildingBlock.Layer = string.Format(cloneReferenceBuildingBlock.Layer, filename);

                    // Find reference entities by it's FullPath instead of Name
                    var foundObjects = _objectManager.FindLayerObjectsByFullPath(cloneReferenceBuildingBlock);
                    foundObjects?.ForEach(obj => _objectManager.DeleteObject(obj.Id));

                    _objectManager.AddNewBuildingBlock(cloneReferenceBuildingBlock, RhinoMeshConverter.ToRhinoMesh(mesh));
                }
            }
            catch (Exception exception)
            {
                _idsConsole.WriteErrorLine($"Exception caught while importing Reference Entity: {exception}", "");
                return LogicStatus.Failure;
            }
            return LogicStatus.Success;
        }
    }
}