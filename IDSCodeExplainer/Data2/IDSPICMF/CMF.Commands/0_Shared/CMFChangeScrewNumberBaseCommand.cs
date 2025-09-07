using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.Commands
{
    public abstract class CMFChangeScrewNumberBaseCommand : CmfCommandBase
    {
        protected class ScrewNumbering
        {
            public List<ICaseData> CasePrefs = new List<ICaseData>();
            public List<int> CaseNumber => CasePrefs.Select(x => x.NCase).ToList();
            public Dictionary<Screw, int> ScrewWithIndex = new Dictionary<Screw, int>();
            public Color Color { get; set; }

            public bool IsNumbered(Screw screw)
            {
                if (ScrewWithIndex.ContainsKey(screw))
                {
                    if (ScrewWithIndex[screw] == -1)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }

            public int GetNextNumber()
            {
                if (!ScrewWithIndex.Any())
                {
                    return 1;
                }

                int biggestNumber = -1;
                bool allNotNumbered = true;
                foreach (var keyValuePair in ScrewWithIndex)
                {
                    allNotNumbered &= keyValuePair.Value == -1;

                    if (keyValuePair.Value > biggestNumber)
                    {
                        biggestNumber = keyValuePair.Value;
                    }
                }

                if (allNotNumbered)
                {
                    return 1;
                }

                return biggestNumber + 1;
            }
        }

        protected ScrewNumberBubbleConduit _bubbleConduit;
        protected bool _blockFromRenumberAcrossCase;

        private bool ScrewAlreadyNumbered(Screw screw, List<ScrewNumbering> allScrewNumbered)
        {
            foreach (var screwNumbering in allScrewNumbered)
            {
                if (screwNumbering.IsNumbered(screw))
                {
                    return true;
                }
            }

            return false;
        }

        protected bool HandleRenumbering(List<Screw> allScrews, ref GetObject screwGetObj, RhinoDoc doc, ScrewManager screwManager, CMFImplantDirector director)
        {
            _bubbleConduit.ResetAllBubbleNumber();
            _bubbleConduit.Show(true);
            doc.Views.Redraw();

            var numbered = new List<ScrewNumbering>();
            ScrewNumbering activeNumbering = null;

            int n = 0;
            while (n <= allScrews.Count - 1)
            {
                screwGetObj.EnableTransparentCommands(false);
                var res = screwGetObj.Get();
                switch (res)
                {
                    case GetResult.Object:
                    {
                        var screw = (Screw)screwGetObj.Object(0).Object();

                        var pref = GetCaseDataTheScrewBelongsTo(screwManager, screw);

                        if (ScrewAlreadyNumbered(screw, numbered))
                        {
                            continue;
                        }

                        bool continueNumberingOnDifferentCase = !_blockFromRenumberAcrossCase && 
                                Control.ModifierKeys == Keys.Shift && !numbered.Exists(x => x.CasePrefs.Exists(y => y == pref));

                        if (!numbered.Exists(x =>
                            {
                                return x.CasePrefs.Any(casePreferenceModel => casePreferenceModel == pref);
                            }) && !continueNumberingOnDifferentCase) // When starting number on implant/guide without number
                        {
                            activeNumbering = new ScrewNumbering();
                            var screwNumber = activeNumbering.GetNextNumber();
                            activeNumbering.CasePrefs.Add(pref);
                            activeNumbering.ScrewWithIndex.Add(screw, screwNumber);

                            numbered.Add(activeNumbering);
                            OverrideBubbleNumber(screw, screwNumber, pref.NCase, numbered.IndexOf(activeNumbering));
                        }
                        else //on existing implant/guide, will continue also ehre, still buggy
                        {
                            ScrewNumbering numbering;
                                
                            if (continueNumberingOnDifferentCase && activeNumbering != null)
                            {
                                numbering = activeNumbering;

                                if (!numbering.CasePrefs.Exists(x => x == pref))
                                {
                                    numbering.CasePrefs.Add(pref);
                                }
                            }
                            else
                            {
                                numbering = numbered.Find(x =>
                                {
                                    return x.CasePrefs.Any(casePreferenceModel => casePreferenceModel == pref);
                                });
                            }

                            if (activeNumbering == null)
                            {
                                continue;
                            }

                            activeNumbering = numbering;

                            var screwNumber = numbering.GetNextNumber();
                            numbering.ScrewWithIndex.Add(screw, screwNumber);

                            OverrideBubbleNumber(screw, screwNumber, pref.NCase, numbered.IndexOf(numbering));
                        }

                        doc.Views.Redraw();
                        n++;
                        break;
                    }
                    case GetResult.Cancel:
                        _bubbleConduit.Show(false);

                        return false;
                    default:
                        break;
                }
            }

            SaveScrewNumbering(numbered, director);

            return true;
        }

        protected abstract ICaseData GetCaseDataTheScrewBelongsTo(ScrewManager screwManager, Screw screw);

        protected abstract void SaveScrewNumbering(List<ScrewNumbering> screwInGroups, CMFImplantDirector director);

        private void OverrideBubbleNumber(Screw screw, int screwNumber, int caseNumber, int groupIndex)
        {
            if (_blockFromRenumberAcrossCase)
            {
                _bubbleConduit.OverrideBubbleNumber(screw, screwNumber, CasePreferencesHelper.GetColor(caseNumber));
            }
            else
            {
                _bubbleConduit.OverrideBubbleNumber(screw, screwNumber, CasePreferencesHelper.GetColor(caseNumber), ScrewUtilities.GetScrewGroupColor(groupIndex));
            }
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            _bubbleConduit.UpdateScrewNumberToBubble();
            HandleOnExitVisualization(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            HandleOnExitVisualization(doc);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            HandleOnExitVisualization(doc);
        }

        private void HandleOnExitVisualization(RhinoDoc doc)
        {
            doc.Objects.UnselectAll();
            Locking.LockAll(doc);
            _bubbleConduit.Show(false);
            doc.Views.Redraw();
        }
    }
}
