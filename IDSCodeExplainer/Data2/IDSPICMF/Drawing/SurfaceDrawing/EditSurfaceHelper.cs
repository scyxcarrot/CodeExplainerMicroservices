using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Drawing
{
    public class EditSurfaceHelper
    {
        public EditSurfaceResult EditSurfaceResult { get; set; }

        private readonly Mesh _constraintMesh;
        private readonly CMFImplantDirector _director;

        public EditSurfaceHelper(Mesh constraintMesh, CMFImplantDirector director)
        {
            _constraintMesh = constraintMesh;
            _director = director;
        }

        public virtual bool Execute(IEnumerable<PatchData> patchDatas)
        {
            var patchSurfaces = patchDatas
                .Where(s => s.GuideSurfaceData is PatchSurface)
                .ToList();
            var skeletonSurfaces = patchDatas
                .Where(s => s.GuideSurfaceData is SkeletonSurface)
                .ToList();

            if (!patchSurfaces.Any() && !skeletonSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There must be at least a surface!");
                return false;
            }

            var oldSurfaces = new Dictionary<Guid, Mesh>();
            var editedSurfaceCounter = 0;
            EditSurfaceResult = new EditSurfaceResult();

            while (true)
            {
                var conduit = new SurfaceConduit(patchSurfaces, skeletonSurfaces)
                {
                    IsHighlighted = true,
                    Enabled = true
                };
                _director.Document.Views.Redraw();

                var res = SelectSurface(
                    out var mesh,
                    out var selectedId);
                if (mesh != null)
                {
                    var selectedSurface = FindSelectedSurface(mesh, patchSurfaces, skeletonSurfaces);
                    if (selectedSurface == null)
                    {
                        continue;
                    }

                    conduit.IsHighlighted = false;

                    var duplicatedSurface = DuplicateData(selectedSurface);
                    var dataContext = new DrawSurfaceDataContext();
                    var editSurface = new EditSurface(
                        _constraintMesh,
                        duplicatedSurface,
                        ref dataContext);
                    var prompt = "Drag control point(s) to reposition.";
                    editSurface.SetCommandPrompt(prompt);

                    var executed = editSurface.Execute();
                    conduit.Enabled = false;

                    if (executed && editSurface.ResultOfSurfaceEdit != null)
                    {
                        editedSurfaceCounter++;
                        EditSurfaceResult.Surfaces[selectedId] = editSurface.ResultOfSurfaceEdit;

                        if (editSurface.ResultOfSurfaceEdit.GuideSurfaceData is SkeletonSurface)
                        {
                            skeletonSurfaces.Remove(selectedSurface);
                            skeletonSurfaces.Add(editSurface.ResultOfSurfaceEdit);
                        }

                        if (editSurface.ResultOfSurfaceEdit.GuideSurfaceData is PatchSurface)
                        {
                            patchSurfaces.Remove(selectedSurface);
                            patchSurfaces.Add(editSurface.ResultOfSurfaceEdit);
                        }

                    }
                }
                else if (res == GetResult.Cancel) //ESC
                {
                    if (editedSurfaceCounter > 0)
                    {
                        var dialogResult = MessageBox.Show(
                            "Pressing Esc will delete the surfaces that you have drawn and are currently drawing. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);


                        if (dialogResult == DialogResult.OK)
                        {
                            RestoreDataInDoc(oldSurfaces);
                            EditSurfaceResult.Surfaces.Clear();
                            EditSurfaceResult = null;
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
                    conduit.Enabled = false;
                    return true;
                }
            }
        }

        protected static GetResult SelectSurface(out Mesh mesh, out Guid id)
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

        private static PatchData FindSelectedSurface(Mesh surface, List<PatchData> patchSurfaces, List<PatchData> skeletonSurfaces)
        {
            var surfaces = patchSurfaces.ToList();
            surfaces.AddRange(skeletonSurfaces);
            return surfaces.FirstOrDefault(s => s.Patch.IsEqual(surface));
        }

        protected static PatchData DuplicateData(PatchData data)
        {
            var duplicatedData = new PatchData(data.Patch.DuplicateMesh());
            duplicatedData.GuideSurfaceData = (IGuideSurface)data.GuideSurfaceData.Clone();
            return duplicatedData;
        }

        protected void RestoreDataInDoc(Dictionary<Guid, Mesh> list)
        {
            foreach (var item in list)
            {
                _director.Document.Objects.Replace(item.Key, item.Value);
            }
        }
    }
}
