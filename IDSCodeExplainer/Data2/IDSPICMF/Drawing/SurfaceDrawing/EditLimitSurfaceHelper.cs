using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDSPICMF.Drawing.SurfaceDrawing
{
    public class EditLimitSurfaceHelper : EditSurfaceHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly Mesh _constraintMesh;
        public EditLimitSurfaceHelper(Mesh constraintMesh, CMFImplantDirector director) : base(constraintMesh, director)
        {
            _director = director;
            _constraintMesh = constraintMesh;
        }

        public override bool Execute(IEnumerable<PatchData> patchDatas)
        {
            var patchSurfaces = patchDatas.Where(s => s.GuideSurfaceData is PatchSurface).ToList();
            if (!patchSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There must be at least a surface!");
                return false;
            }

            var oldSurfaces = new Dictionary<Guid, Mesh>();
            var editedSurfaceCounter = 0;
            EditSurfaceResult = new EditSurfaceResult();

            while (true)
            {
                var conduit = new SurfaceConduit(patchSurfaces, new List<PatchData>()) { IsHighlighted = true, Enabled = true };
                _director.Document.Views.Redraw();

                var res = SelectSurface(out var mesh, out var selectedId);

                if (mesh != null)
                {
                    if (ProcessSurfaceEdit(patchSurfaces, conduit, selectedId))
                        editedSurfaceCounter++;
                }
                else
                {
                    bool? exitResult = HandleUserExit(res, editedSurfaceCounter, oldSurfaces, conduit);
                    if (exitResult.HasValue)
                        return exitResult.Value;
                }
            }
        }

        private bool ProcessSurfaceEdit(List<PatchData> patchSurfaces, SurfaceConduit conduit, Guid selectedId)
        {
            var selectedSurface = patchSurfaces[0];
            if (selectedSurface == null) return false;

            conduit.IsHighlighted = false;
            var duplicatedSurface = DuplicateData(selectedSurface);
            var dataContext = new DrawSurfaceDataContext();
            var editSurface = new EditSurface(_constraintMesh, duplicatedSurface, ref dataContext);
            editSurface.CurrentEditSurfaceMode = new EditLimitSurfaceMode(ref dataContext, (PatchSurface)duplicatedSurface.GuideSurfaceData);
            editSurface.SetCommandPrompt("Drag control point(s) to reposition.");

            var executed = editSurface.Execute();
            conduit.Enabled = false;

            if (executed && editSurface.ResultOfSurfaceEdit != null)
            {
                EditSurfaceResult.Surfaces[selectedId] = editSurface.ResultOfSurfaceEdit;
                if (editSurface.ResultOfSurfaceEdit.GuideSurfaceData is PatchSurface)
                {
                    patchSurfaces.Remove(selectedSurface);
                    patchSurfaces.Add(editSurface.ResultOfSurfaceEdit);
                }
                return true;
            }
            return false;
        }

        private bool? HandleUserExit(GetResult res, int editedSurfaceCounter, Dictionary<Guid, Mesh> oldSurfaces, SurfaceConduit conduit)
        {
            conduit.Enabled = false;

            if (res == GetResult.Cancel)
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
                        return false;
                    }
                    return null; // Continue loop
                }
                return false;
            }

            if (res == GetResult.Nothing)
            {
                RestoreDataInDoc(oldSurfaces);
                return true;
            }

            return null; // Continue loop
        }
    }
}
