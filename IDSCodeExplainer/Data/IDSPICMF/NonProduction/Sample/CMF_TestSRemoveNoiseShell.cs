using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("752FDCB6-5FD2-423D-BB3D-09EC01957910")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestRemoveNoiseShell : CmfCommandBase
    {
        public CMF_TestRemoveNoiseShell()
        {
            Instance = this;
        }

        public static CMF_TestRemoveNoiseShell Instance { get; private set; }

        public override string EnglishName => "CMF_TestRemoveNoiseShell";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Guid id;
            var selectedMesh = SelectedMesh(doc, out id);

            if (selectedMesh == null)
            {
                return Result.Cancel;
            }

            var removeNoiseDisjointedShellEditor = new RemoveNoiseDisjointedShellEditor();
            var finalMeshesDictionary = removeNoiseDisjointedShellEditor.Execute(new Dictionary<Guid, Mesh>(){{id, selectedMesh}});
            removeNoiseDisjointedShellEditor.CleanUp();

            if (finalMeshesDictionary.Count != 1)
            {
                return Result.Failure;
            }

            doc.Objects.Replace(id, finalMeshesDictionary[id]);

            return Result.Success;
        }

        private Mesh SelectedMesh(RhinoDoc doc, out Guid id)
        {
            foreach (RhinoObject obj in doc.Objects)
            {
                if (!(obj.Geometry is Mesh))
                {
                    continue;
                }

                doc.Objects.Unlock(obj.Id, true);
            }

            id = Guid.Empty;
            var selectedPart = new GetObject();
            selectedPart.SetCommandPrompt("Select a disjointable mesh for edit");
            selectedPart.EnablePreSelect(false, false);
            selectedPart.EnablePostSelect(true);
            selectedPart.AcceptNothing(true);
            selectedPart.EnableTransparentCommands(false);

            var res = selectedPart.Get();
            if (res != GetResult.Object)
            {
                return null;
            }

            var selectedObject = selectedPart.Object(0).Object();
            var selectedMesh = selectedObject.Geometry.Duplicate() as Mesh;
            id = selectedObject.Id;
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return selectedMesh;
        }
    }
#endif
}
