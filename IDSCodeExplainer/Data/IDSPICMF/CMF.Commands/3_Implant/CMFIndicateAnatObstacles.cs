using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("5475F930-2B6A-45FB-B467-E22EB04BB5AA")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFIndicateAnatObstacles : CmfCommandBase
    {
        private List<AnatomicalObstaclesEntity> PartSelectionList { get; set; }
        private Color AnatomicalObstaclesColor => Colors.AnatomicalObstacles;

        public CMFIndicateAnatObstacles()
        {
            TheCommand = this;
            VisualizationComponent = new CMFIndicateAnatObstaclesVisualization();
        }
        public static CMFIndicateAnatObstacles TheCommand { get; private set; }
        public override string EnglishName => CommandEnglishName.CMFIndicateAnatObstacles;

        private void UpdateMaterial(Material mat, Color color)
        {
            mat.AmbientColor = color;
            mat.DiffuseColor = color;
            mat.SpecularColor = color;
            mat.CommitChanges();
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            doc.Views.Redraw();

            doc.UndoRecordingEnabled = false;

            var objectManager = new CMFObjectManager(director);
            PrepareSelectionEntities(objectManager);
            PartSelectionList.ForEach(part => doc.Objects.Unlock(part.Object.Id, true));

            var selectEntities = new GetObject();
            selectEntities.SetCommandPrompt("Select/Deselect anatomical obstacles.");
            selectEntities.EnablePreSelect(false, false);
            selectEntities.EnablePostSelect(true);
            selectEntities.AcceptNothing(true);
            selectEntities.EnableTransparentCommands(false);
            selectEntities.EnableHighlight(false);

            while (true)
            {
                var res = selectEntities.Get();

                if (res == GetResult.Object)
                {
                    var selectedObj = doc.Objects.GetSelectedObjects(false, false).FirstOrDefault();
                    if (selectedObj != null)
                    {
                        var selectedId = PartSelectionList.FindIndex(x => x.Object.Id == selectedObj.Id);
                        PartSelectionList[selectedId].IsAnatObstacles = !PartSelectionList[selectedId].IsAnatObstacles;
                        var mat = PartSelectionList[selectedId].Object.GetMaterial(true);
                        var color = PartSelectionList[selectedId].IsAnatObstacles
                            ? AnatomicalObstaclesColor
                            : PartSelectionList[selectedId].OriginalColor;
                        UpdateMaterial(mat, color);
                        director.Document.Views.Redraw();
                    }
                }

                if (res == GetResult.Nothing)
                {
                    var anatomicalObstacles = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles).ToList();
                    foreach (var selectedPart in PartSelectionList)
                    {
                        if (selectedPart.IsAnatObstacles)
                        {
                            if (!selectedPart.IsAnatObstaclesLayer)
                            {
                                var mat = selectedPart.Object.GetMaterial(true);
                                UpdateMaterial(mat, selectedPart.OriginalColor);
                                AnatomicalObstacleUtilities.AddAsAnatomicalObstacle(objectManager, selectedPart.Object);
                            }
                        }
                        else
                        {
                            if (selectedPart.IsAnatObstaclesLayer)
                            {
                                var found = anatomicalObstacles.Find(x =>
                                    MeshUtilities.IsEqual((Mesh)x.Geometry, (Mesh)selectedPart.Object.Geometry));
                                if (found != null)
                                {
                                    objectManager.DeleteObject(found.Id);
                                }
                            }
                            else
                            {
                                UpdateMaterial(selectedPart.Object.GetMaterial(true), selectedPart.OriginalColor);
                            }
                        }
                    }
                    director.Document.Views.Redraw();
                    break;
                }

                if (res == GetResult.Cancel)
                {
                    foreach (var selectedPart in PartSelectionList)
                    {
                        var color = selectedPart.IsAnatObstaclesLayer
                            ? AnatomicalObstaclesColor
                            : selectedPart.OriginalColor;
                        var mat = selectedPart.Object.GetMaterial(true);
                        UpdateMaterial(mat, color);
                    }
                    break;
                }
            }

            doc.UndoRecordingEnabled = true;
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            base.OnCommandExecuteSuccess(doc, director);
            if (director.ImplantScrewQcLiveUpdateHandler == null)
            {
                return;
            }

            var screwQcCheckManager =
                new ScrewQcCheckerManager(director, new[] { new ImplantScrewAnatomicalObstacleProxyChecker(director) });
            var screwManager = new ScrewManager(director);
            var implantScrews = screwManager.GetAllScrews(false);
            director.ImplantScrewQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckManager, implantScrews);
        }

        private void PrepareSelectionEntities(CMFObjectManager objectManager)
        {
            PartSelectionList = new List<AnatomicalObstaclesEntity>();
            var proPlanBlocks = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport);

            var anatomicalPartBlocks = proPlanBlocks.Distinct();
            var anatObstaclesBlocks = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles).ToList();

            foreach (var block in anatomicalPartBlocks)
            {
                var findMatch = AnatomicalObstacleUtilities.GetAnatomicalObstacle(anatObstaclesBlocks, block);
                if (findMatch == null)
                {
                    PartSelectionList.Add(new AnatomicalObstaclesEntity(block, block.GetMaterial(true).AmbientColor, false, false));
                }
                else
                {
                    PartSelectionList.Add(new AnatomicalObstaclesEntity(findMatch, block.GetMaterial(true).AmbientColor, true, true));
                }
            }
        }
    }
}
