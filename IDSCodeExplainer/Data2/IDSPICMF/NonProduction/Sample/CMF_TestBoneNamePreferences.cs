using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("E20DF485-B73E-42C3-9D39-378C279B945F")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestBoneNamePreferences : CmfCommandBase
    {
        public CMF_TestBoneNamePreferences()
        {
            TheCommand = this;
        }

        public static CMF_TestBoneNamePreferences TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestBoneNamePreferences";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {

            var HandleUnlocking = new Action(() =>
            {
                Locking.LockAll(doc);

                var rhObjects = BoneThicknessAnalyzableObjectManager.GetBoneThicknessAnalyzableRhinoObjects(doc);
                rhObjects.ForEach(x => doc.Objects.Unlock(x.Id, true));
            });

            var handleOnPlannedLayerChanged = new EventHandler<Rhino.DocObjects.Tables.LayerTableEventArgs>((s, e) =>
            {
                HandleUnlocking();
            });

            RhinoDoc.LayerTableEvent += handleOnPlannedLayerChanged;

            var selectEntities = new GetObject();
            selectEntities.EnablePreSelect(false, false);
            selectEntities.EnablePostSelect(true);
            selectEntities.AcceptNothing(true);
            selectEntities.EnableTransparentCommands(false);
            selectEntities.EnableHighlight(false);

            RhinoObject selectedObject = null;
            while (true)
            {
                selectEntities.SetCommandPrompt("Select the bone for testing:");

                HandleUnlocking();
                var res = selectEntities.Get();
                if (res == GetResult.Object)
                {
                    var selectedObj = doc.Objects.GetSelectedObjects(false, false).FirstOrDefault();
                    if (selectedObj != null)
                    {

                        if (selectedObj.Geometry is Mesh)
                        {
                            selectedObject = selectedObj;
                            director.Document.Views.Redraw();
                            var name = BoneNamePreferencesManager.Instance.GetPreferenceBoneName(director, selectedObject);
                            IDSPluginHelper.WriteLine(LogCategory.Default, $"Bone Name is: \"{name}\"");
                            continue;
                        }

                        IDSPluginHelper.WriteLine(LogCategory.Error, "Unsupported object type! Please select a Mesh type object.");
                        continue;
                    }
                }

                if (res == GetResult.Nothing)
                {
                    director.Document.Views.Redraw();
                    RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
                    return Result.Success;
                }

                if (res == GetResult.Cancel)
                {
                    RhinoDoc.LayerTableEvent -= handleOnPlannedLayerChanged;
                    return Result.Cancel;
                }
            }
        }
    }
#endif
}
