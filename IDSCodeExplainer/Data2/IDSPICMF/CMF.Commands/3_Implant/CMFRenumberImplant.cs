using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Commands
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("6ffd9d00-497a-4b4a-b867-5564b7529941")]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantPreview)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFRenumberImplant : CmfCommandBase
    {
        static CMFRenumberImplant _instance;
        public CMFRenumberImplant()
        {
            _instance = this;
        }

        public static CMFRenumberImplant Instance => _instance;

        public override string EnglishName => "CMFRenumberImplant";

        private readonly List<ImplantNumberBubbleConduit> _conduits = new List<ImplantNumberBubbleConduit>();

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            Locking.UnlockAllImplantPreview(doc);

            var objManager = new CMFObjectManager(director);
            InitializeNumberConduits(director, true);
            _conduits.ForEach(x => x.Enabled = true);

            var implantGetObj = new GetObject();
            implantGetObj.SetCommandPrompt("Click the Implant one by one to assign Implant numbers.");
            implantGetObj.DisablePreSelect();
            implantGetObj.AcceptNothing(true);
            implantGetObj.EnableHighlight(false);

            doc.Views.Redraw();

            UnsetCasePreferenceNumber(director);
            var newIndex = 1;
            while (newIndex <= _conduits.Count)
            {
                var res = implantGetObj.Get(); // redraws before and after getting
                switch (res)
                {
                    case GetResult.Object:
                        {
                            // Also called when object was preselected
                            var implant = implantGetObj.Object(0).Object();

                            var casePreferenceModel = (ImplantPreferenceModel)objManager.GetCasePreference(implant);
                            director.CasePrefManager.HandleRenumberCaseNumber(casePreferenceModel, newIndex);
                            newIndex++;
                            InitializeNumberConduits(director, false);
                            _conduits.ForEach(x => x.Enabled = true);
                            doc.Views.Redraw();
                            break;
                        }
                    default:
                        break;
                }
            }

            _conduits.ForEach(x => x.Enabled = false);
            _conduits.Clear();
            Locking.LockAll(doc);
            doc.Views.Redraw();
            return Result.Success;
        }

        private void InitializeNumberConduits(CMFImplantDirector director, bool unset)
        {
            _conduits.ForEach(x => x.Enabled = false);
            _conduits.Clear();

            var objManager = new CMFObjectManager(director);
            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                var implantObject = objManager.GetImplantObject(x);
                var dataModel = objManager.GetImplantDataModel(implantObject);

                var num = unset ? -1 : x.NCase;

                var conduit =
                    new ImplantNumberBubbleConduit(dataModel, num, Color.AliceBlue, Color.Blue)
                    { Enabled = true };

                _conduits.Add(conduit);
            });
        }

        private void UnsetCasePreferenceNumber(CMFImplantDirector director)
        {
            director.CasePrefManager.CasePreferences.ForEach(x => { x.NCase = -1; });
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            CasePreferencePanel.GetView().InvalidateUI();
        }

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }

            var objManager = new CMFObjectManager(director);
            if (director.CasePrefManager.CasePreferences.Any(x => objManager.GetImplantObject(x) == null))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please create preview for all implants!");
                return false;
            }

            return true;
        }
    }

#endif
}
