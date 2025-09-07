using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("3BE2E0CE-5639-4622-9BB3-8748EAD1C793")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestViewLinkedGuides : CmfCommandBase
    {
        private Dictionary<RhinoObject, Color> linkedGuidesEntities;
        private readonly Color unassignedColor = Color.Yellow;

        public CMF_TestViewLinkedGuides()
        {
            TheCommand = this;
            linkedGuidesEntities = new Dictionary<RhinoObject, Color>();
        }
        
        public static CMF_TestViewLinkedGuides TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestViewLinkedGuides";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            doc.UndoRecordingEnabled = false;

            DisplayLinkedGuidesEntities(director);

            string dummyInput = "";
            var result = RhinoGet.GetString("Press <Enter> to finish", true, ref dummyInput);
            return result;
        }

        private void DisplayLinkedGuidesEntities(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);

            var showPaths = new List<string>();
            showPaths.AddRange(objectManager.GetAllImplantBuildingBlocks(IBB.Screw).Select(x => x.Layer));
            showPaths.AddRange(objectManager.GetAllImplantBuildingBlocks(IBB.RegisteredBarrel).Select(x => x.Layer));
            IDS.Core.Visualization.Visibility.SetVisible(director.Document, showPaths);

            var rhinoObjects = new List<RhinoObject>();
            rhinoObjects.AddRange(objectManager.GetAllBuildingBlocks(IBB.Screw));

            foreach (Screw screw in rhinoObjects)
            {
                linkedGuidesEntities.Add(screw, screw.GetMaterial(true).AmbientColor);
                UpdateColor(screw, unassignedColor);

                if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                {
                    var registeredBarrelObject = director.Document.Objects.Find(screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel]);
                    linkedGuidesEntities.Add(registeredBarrelObject, registeredBarrelObject.GetMaterial(true).AmbientColor);
                    UpdateColor(registeredBarrelObject, unassignedColor);
                }
            }

            foreach (var guidePref in director.CasePrefManager.GuidePreferences)
            {
                foreach (var implantScrewID in guidePref.LinkedImplantScrews)
                {
                    if (!linkedGuidesEntities.Any(r => r.Key.Id == implantScrewID))
                    {
                        continue;
                    }

                    var info = linkedGuidesEntities.First(r => r.Key.Id == implantScrewID);
                    UpdateColor(info.Key, CasePreferencesHelper.GetColor(guidePref.NCase));

                    if (((Screw)info.Key).ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                    {
                        var registeredBarrelObject = linkedGuidesEntities.First(r => r.Key.Id == ((Screw)info.Key).ScrewGuideAidesInDocument[IBB.RegisteredBarrel]).Key;
                        UpdateColor(registeredBarrelObject, CasePreferencesHelper.GetColor(guidePref.NCase));
                    }
                }
            }

            director.Document.Views.Redraw();
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            CleanUp(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            CleanUp(doc);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            CleanUp(doc);
        }

        private void CleanUp(RhinoDoc doc)
        {
            foreach (var entity in linkedGuidesEntities)
            {
                UpdateColor(entity.Key, entity.Value);
            }

            linkedGuidesEntities.Clear();
            doc.Views.Redraw();

            doc.UndoRecordingEnabled = true;
        }

        private void UpdateColor(RhinoObject rhinoObject, Color color)
        {
            var mat = rhinoObject.GetMaterial(true);
            mat.AmbientColor = color;
            mat.DiffuseColor = color;
            mat.SpecularColor = color;
            mat.CommitChanges();
        }
    }

#endif
}