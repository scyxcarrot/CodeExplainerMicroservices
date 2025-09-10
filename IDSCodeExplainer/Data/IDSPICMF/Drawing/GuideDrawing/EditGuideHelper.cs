using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class EditGuideHelper
    {
        public DrawGuideResult ResultOfGuideDrawing { get; set; }

        private readonly Mesh _lowLoDConstraintMesh;
        private readonly Mesh _guideSurfaceCreationLowLoDBase;
        private readonly RhinoDoc doc;
        private readonly CMFImplantDirector director;

        public EditGuideHelper(Mesh lowLoDConstraintMesh, Mesh guideSurfaceCreationLowLoDBase, RhinoDoc doc, CMFImplantDirector director)
        {
            this._lowLoDConstraintMesh = lowLoDConstraintMesh;
            this._guideSurfaceCreationLowLoDBase = guideSurfaceCreationLowLoDBase;
            this.doc = doc;
            this.director = director;
        }

        public bool Execute(GuidePreferenceDataModel prefData)
        {
            var positiveSurfaces = prefData.PositiveSurfaces.ToList();
            var negativeSurfaces = prefData.NegativeSurfaces.ToList();

            if (!positiveSurfaces.Any() && !negativeSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There must be at least a positive/negative surface!");
                return true;
            }

            var oldSurfaces = new Dictionary<Guid, Mesh>();
            var replacedSurfaces = new Dictionary<Guid, Mesh>();
            var drawGuideVisualization = new DrawGuideVisualization();
            var editedSurfaceCounter = 0;

            while (true)
            {
                var patchSurfaces = positiveSurfaces.Where(s => s.GuideSurfaceData is PatchSurface).ToList();
                var skeletonSurfaces = positiveSurfaces.Where(s => s.GuideSurfaceData is SkeletonSurface).ToList();
                var conduit = new GuideSurfacesConduit(patchSurfaces, negativeSurfaces, skeletonSurfaces, new List<PatchData>());

                drawGuideVisualization.SetGuideDrawingSurfacesVisibility(prefData, doc, true);
                conduit.IsHighlighted = true;
                conduit.Enabled = true;
                doc.Views.Redraw();

                Mesh mesh;
                Guid selectedId;
                var res = SelectSurface(out mesh, out selectedId);
                if (mesh != null)
                {
                    var selectedSurface = FindSelectedSurface(mesh, positiveSurfaces, negativeSurfaces);
                    if (selectedSurface == null)
                    {
                        conduit.ResetSurfaceToNotRender();
                        continue;
                    }

                    drawGuideVisualization.SetGuideDrawingSurfacesVisibility(prefData, doc, false);
                    conduit.IsHighlighted = false;
                    conduit.AddSurfaceToNotRender(selectedSurface);

                    var duplicatedSurface = DuplicateData(selectedSurface);
                    var factory = new DrawGuideDataContextFactory();
                    var dataContext = factory.CreateDrawGuideDataContextForGuideSurface();
                    var editGuide = new EditGuide(_lowLoDConstraintMesh, _guideSurfaceCreationLowLoDBase,  duplicatedSurface, ref dataContext);
                    var prompt = "Drag control point(s) to reposition. O to toggle support mesh transparency On/Off.";
                    editGuide.SetCommandPrompt(prompt);

                    var executed = editGuide.Execute();
                    conduit.Enabled = false;
                    conduit.ResetSurfaceToNotRender();

                    if (executed)
                    {
                        editedSurfaceCounter++;
                        ResultOfGuideDrawing = new DrawGuideResult();
                        ResultOfGuideDrawing.GuideBaseSurfaces.AddRange(positiveSurfaces);
                        ResultOfGuideDrawing.GuideBaseNegativeSurfaces.AddRange(negativeSurfaces);

                        ReplaceData(selectedSurface, editGuide.ResultOfGuideEdit, positiveSurfaces, negativeSurfaces);
                        ReplaceSurfaceInDoc(selectedId, editGuide.ResultOfGuideEdit);
                        UpdateSurfacesDictionary(selectedId, mesh, editGuide.ResultOfGuideEdit.Patch, ref oldSurfaces, ref replacedSurfaces);
                    }
                }
                else if (res == GetResult.Cancel) //ESC
                {
                    if (editedSurfaceCounter > 0)
                    {
                        var dlgRes = MessageBox.Show(
                            "Pressing Esc will delete the surfaces that you have drawn and are currently drawing. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);


                        if (dlgRes == DialogResult.OK)
                        {

                            RestoreDataInDoc(oldSurfaces);
                            if (ResultOfGuideDrawing != null)
                            {
                                ResultOfGuideDrawing.GuideBaseSurfaces.Clear();
                                ResultOfGuideDrawing.GuideBaseNegativeSurfaces.Clear();
                                ResultOfGuideDrawing = null;
                            }
                            conduit.Enabled = false;
                            return false;
                        }
                        conduit.Enabled = false;
                        continue;
                    }

                    conduit.Enabled = false;
                    return false;
                }
                else if (res == GetResult.Nothing)
                {
                    //ENTER
                    RestoreDataInDoc(oldSurfaces);
                    if (ResultOfGuideDrawing != null)
                    {
                        var roi = CreateRoIMesh(replacedSurfaces.Select(s => s.Value).ToList(), prefData);
                        ResultOfGuideDrawing.RoIMesh = roi;
                    }
                    conduit.Enabled = false;
                    return true;
                }
            }
        }

        private Mesh CreateRoIMesh(List<PatchData> additionalSurfaces, GuidePreferenceDataModel guidePrefModel)
        {
            return CreateRoIMesh(additionalSurfaces.Select(s => s.Patch).ToList(), guidePrefModel);
        }

        private Mesh CreateRoIMesh(List<Mesh> additionalSurfaces, GuidePreferenceDataModel guidePrefModel)
        {
            var existingSurfaces = GuideDrawingUtilities.CreateRoIDefinitionMesh(director, guidePrefModel);

            var roiDefinition = new Mesh();
            roiDefinition.Append(existingSurfaces);
            roiDefinition.Append(additionalSurfaces);

            return GuideDrawingUtilities.CreateRoiMesh(_guideSurfaceCreationLowLoDBase, roiDefinition);
        }
        
        public bool ExecuteEditLink(GuidePreferenceDataModel prefData)
        {
            var linkSurfaces = prefData.LinkSurfaces.ToList();

            var oldSurfaces = new Dictionary<Guid, Mesh>();
            var replacedSurfaces = new Dictionary<Guid, Mesh>();
            var drawGuideVisualization = new DrawGuideVisualization();

            var editedCounter = 0;
            while (true)
            {
                var patchSurfaces = linkSurfaces.Where(s => s.GuideSurfaceData is PatchSurface).ToList();
                var skeletonSurfaces = linkSurfaces.Where(s => s.GuideSurfaceData is SkeletonSurface).ToList();
                var conduit = new GuideSurfacesConduit(patchSurfaces, new List<PatchData>(), skeletonSurfaces, new List<PatchData>());

                drawGuideVisualization.SetLinkDrawingSurfacesVisibility(prefData, doc, true);
                conduit.IsHighlighted = true;
                conduit.Enabled = true;
                doc.Views.Redraw();

                Mesh mesh;
                Guid selectedId;
                var res = SelectSurface(out mesh, out selectedId);
                if (mesh != null)
                {
                    var selectedSurface = FindSelectedSurface(mesh, linkSurfaces, new List<PatchData>());
                    if (selectedSurface == null)
                    {
                        conduit.ResetSurfaceToNotRender();
                        continue;
                    }

                    drawGuideVisualization.SetLinkDrawingSurfacesVisibility(prefData, doc, false);
                    conduit.IsHighlighted = false;

                    conduit.AddSurfaceToNotRender(selectedSurface);
                    var duplicatedSurface = DuplicateData(selectedSurface);
                    var factory = new DrawGuideDataContextFactory();
                    var dataContext = factory.CreateDrawGuideDataContextForGuideLink();
                    var editGuide = new EditGuide(_lowLoDConstraintMesh, _guideSurfaceCreationLowLoDBase, duplicatedSurface, ref dataContext);
                    var prompt = "Drag control point(s) to reposition. O to toggle support mesh transparency On/Off.";
                    editGuide.SetCommandPrompt(prompt);

                    var executed = editGuide.Execute();
                    conduit.Enabled = false;
                    conduit.ResetSurfaceToNotRender();

                    if (executed)
                    {
                        editedCounter++;
                        ResultOfGuideDrawing = new DrawGuideResult();
                        ResultOfGuideDrawing.GuideBaseSurfaces.AddRange(linkSurfaces);

                        ReplaceData(selectedSurface, editGuide.ResultOfGuideEdit, linkSurfaces, new List<PatchData>());
                        ReplaceSurfaceInDoc(selectedId, editGuide.ResultOfGuideEdit);
                        UpdateSurfacesDictionary(selectedId, mesh, editGuide.ResultOfGuideEdit.Patch, ref oldSurfaces, ref replacedSurfaces);
                    }
                }
                else if (res == GetResult.Cancel)     //ESC
                {
                    if (editedCounter > 0)
                    {
                        var dlgRes = MessageBox.Show(
                            "Pressing Esc will delete the surfaces that you have drawn and are currently drawing. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                        if (dlgRes == DialogResult.OK)
                        {
                            RestoreDataInDoc(oldSurfaces);
                            if (ResultOfGuideDrawing != null)
                            {
                                ResultOfGuideDrawing.GuideBaseSurfaces.Clear();
                                ResultOfGuideDrawing.GuideBaseNegativeSurfaces.Clear();
                                ResultOfGuideDrawing = null;
                            }
                            conduit.Enabled = false;
                            return false;
                        }
                        conduit.Enabled = false;
                        continue;
                    }
                    conduit.Enabled = false;
                    return false;
                }
                else if (res == GetResult.Nothing)
                {
                    //ENTER
                    RestoreDataInDoc(oldSurfaces);
                    if (ResultOfGuideDrawing != null)
                    {
                        var roi = CreateRoIMesh(replacedSurfaces.Select(s => s.Value).ToList(), prefData);
                        ResultOfGuideDrawing.RoIMesh = roi;
                    }
                    conduit.Enabled = false;
                    return true;
                }
            }
        }

        private GetResult SelectSurface(out Mesh mesh, out Guid id)
        {
            var selectSurface = new GetObject();
            selectSurface.SetCommandPrompt("Select a surface to edit.");
            selectSurface.DisablePreSelect();
            selectSurface.AcceptNothing(true);
            selectSurface.EnableHighlight(false);
            selectSurface.EnableTransparentCommands(false);

            mesh = null;
            id = Guid.Empty;

            var res = selectSurface.Get();
            if (res == GetResult.Object)
            {
                var rhinoObj = selectSurface.Object(0).Object();
                id = rhinoObj.Id;
                mesh = (Mesh)rhinoObj.Geometry;
            }

            return res;
        }

        private PatchData FindSelectedSurface(Mesh surface, List<PatchData> PositiveSurfaces, List<PatchData> NegativeSurfaces)
        {
            var surfaces = PositiveSurfaces.ToList();
            surfaces.AddRange(NegativeSurfaces);
            return surfaces.FirstOrDefault(s => s.Patch.IsEqual(surface));
        }

        private PatchData DuplicateData(PatchData data)
        {
            var duplicatedData = new PatchData(data.Patch.DuplicateMesh());
            duplicatedData.GuideSurfaceData = (IGuideSurface)data.GuideSurfaceData.Clone();
            return duplicatedData;
        }

        private void ReplaceData(PatchData oldData, PatchData newData, List<PatchData> positiveSurfaces, List<PatchData> negativeSurfaces)
        {
            if (ResultOfGuideDrawing.GuideBaseSurfaces.Contains(oldData))
            {
                ResultOfGuideDrawing.GuideBaseSurfaces.Remove(oldData);
                ResultOfGuideDrawing.GuideBaseSurfaces.Add(newData);
            }
            else if (ResultOfGuideDrawing.GuideBaseNegativeSurfaces.Contains(oldData))
            {
                ResultOfGuideDrawing.GuideBaseNegativeSurfaces.Remove(oldData);
                ResultOfGuideDrawing.GuideBaseNegativeSurfaces.Add(newData);
            }

            if (positiveSurfaces.Contains(oldData))
            {
                positiveSurfaces.Remove(oldData);
                positiveSurfaces.Add(newData);
            }
            else if (negativeSurfaces.Contains(oldData))
            {
                negativeSurfaces.Remove(oldData);
                negativeSurfaces.Add(newData);
            }
        }

        private void ReplaceSurfaceInDoc(Guid id, PatchData newData)
        {
            director.Document.Objects.Replace(id, newData.Patch);
        }

        private void RestoreDataInDoc(Dictionary<Guid, Mesh> list)
        {
            foreach (var item in list)
            {
                director.Document.Objects.Replace(item.Key, item.Value);
            }
        }

        private void UpdateSurfacesDictionary(Guid selectedId, Mesh oldMesh, Mesh newMesh, ref Dictionary<Guid, Mesh> oldSurfaces, ref Dictionary<Guid, Mesh> replacedSurfaces)
        {
            if (!oldSurfaces.ContainsKey(selectedId))
            {
                oldSurfaces.Add(selectedId, oldMesh);
            }

            if (!replacedSurfaces.ContainsKey(selectedId))
            {
                replacedSurfaces.Add(selectedId, newMesh);
            }
            else
            {
                replacedSurfaces[selectedId] = newMesh;
            }
        }
    }
}
