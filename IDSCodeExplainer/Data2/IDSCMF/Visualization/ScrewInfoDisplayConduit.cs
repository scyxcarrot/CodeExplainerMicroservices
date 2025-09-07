using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class ScrewNumberBubbleConduit
    {
        public static double GetScrewNumberBubbleConduitSize(ImplantPreferenceModel pref)
        {
            return GetScrewNumberBubbleConduitSize(pref.CasePrefData);
        }

        public static double GetScrewNumberBubbleConduitSize(CasePreferenceData casePrefData)
        {
            return (casePrefData.PastilleDiameter / 2) * 0.75;
        }

        public static double GetScrewNumberDisplayConduitSize(ImplantPreferenceModel pref)
        {
            return GetScrewNumberDisplayConduitSize(pref.CasePrefData);
        }

        public static double GetScrewNumberDisplayConduitSize(CasePreferenceData casePrefData)
        {
            return GetScrewNumberBubbleConduitSize(casePrefData);
        }

        //Both of these should have the same index and count
        private readonly List<Screw> _screwRef;
        private readonly List<NumberBubbleConduit> _conduits = new List<NumberBubbleConduit>();

        public ScrewNumberBubbleConduit(List<Screw> screws, Color textColor, ScrewManager screwManager) : this(screws, textColor, screwManager, true)
        {
            //
        }

        public ScrewNumberBubbleConduit(List<Screw> screws, Color textColor, ScrewManager screwManager, bool isForImplantScrew)
        {
            if (isForImplantScrew)
            {
                screws.ForEach(x =>
                {
                    var pref = screwManager.GetImplantPreferenceTheScrewBelongsTo(x);
                    if (pref != null) //Can be null if implant is deleted
                    {
                        var conduit = new NumberBubbleConduit(x.HeadPoint, x.Index, textColor,
                            CasePreferencesHelper.GetColor(pref.CaseNumber));
                        conduit.BubbleRadius = GetScrewNumberBubbleConduitSize(pref);
                        conduit.DisplaySize = GetScrewNumberDisplayConduitSize(pref);
                        _conduits.Add(conduit);
                    }
                });

                _screwRef = screws;
            }
            else
            {
                var sharedScrews = new List<List<Screw>>();
                screws.ForEach(x =>
                {
                    if (!sharedScrews.Any(s => s.Any(y => y.Id == x.Id)))
                    {
                        var screwsItSharedWith = x.GetScrewItSharedWith();
                        if (screwsItSharedWith.Any())
                        {
                            var list = new List<Screw>();

                            list.Add(x);
                            screwsItSharedWith.ForEach(s =>
                            {
                                if (s != null && s.Id != x.Id)
                                {
                                    list.Add(s);
                                }
                            });

                            sharedScrews.Add(list);
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (!sharedScrews.Any(s => s.Any(y => y.Id == x.Id)))
                    {
                        var list = new List<Screw>();
                        list.Add(x);
                        sharedScrews.Add(list);
                    }
                });

                var orderedScrews = new List<Screw>();
                sharedScrews.ForEach(list =>
                {
                    if (list.Count == 1)
                    {
                        var singleScrew = list.First();
                        var pref = GetGuidePreferenceTheScrewBelongsTo(screwManager, singleScrew);
                        if (pref != null)
                        {
                            var conduit = new NumberBubbleConduit(singleScrew.HeadPoint, singleScrew.Index, textColor,
                                CasePreferencesHelper.GetColor(pref.NCase));
                            conduit.BubbleRadius = 2.0;
                            conduit.DisplaySize = 2.0;
                            _conduits.Add(conduit);
                            orderedScrews.Add(singleScrew);
                        }
                    }
                    else
                    {
                        var angle = 360 / list.Count;
                        var startAngle = list.Count % 2 == 0 ? 0: -90;

                        for (var i = 0; i < list.Count; i++)
                        {
                            var screw = list[i];
                            var pref = GetGuidePreferenceTheScrewBelongsTo(screwManager, screw);
                            if (pref != null)
                            {
                                var conduit = new GuideFixationScrewNumberBubbleConduit(screw.HeadPoint, screw.Index, textColor,
                                CasePreferencesHelper.GetColor(pref.NCase), (angle * i) + startAngle);
                                conduit.BubbleRadius = 2.0;
                                conduit.DisplaySize = 2.0;
                                conduit.BubbleBorderThickness = 3;
                                _conduits.Add(conduit);
                                orderedScrews.Add(screw);
                            }
                        }
                    }
                });

                _screwRef = orderedScrews;
            }
        }

        public bool HasScrew()
        {
            return _screwRef.Any();
        }

        public void Show(bool isEnabled)
        {
            _conduits.ForEach(x => x.Enabled = isEnabled);
        }

        public bool IsShowing()
        {
            return _conduits.Any() && _conduits[0].Enabled;
        }

        public void InvalidateBubbleNumberToScrew()
        {
            Debug.Assert(_screwRef.Count == _conduits.Count);

            for (var i = 0; i < _conduits.Count; ++i)
            {
                _conduits[i].Number = _screwRef[i].Index;
            }
        }

        public void UpdateScrewNumberToBubble()
        {
            Debug.Assert(_screwRef.Count == _conduits.Count);

            for (var i = 0; i < _conduits.Count; ++i)
            {
                _screwRef[i].Index = _conduits[i].Number;
            }
        }

        public void ResetAllBubbleNumber()
        {
            _conduits.ForEach(x => x.Number = -1);
            _conduits.ForEach(x => x.BubbleColor = Color.Red);
        }

        public void OverrideBubbleNumber(Screw screw, int number, Color bubbleColor, Color borderColor)
        {
            var cond = _conduits[_screwRef.IndexOf(screw)];
            cond.Number = number;
            cond.BubbleColor = bubbleColor;
            cond.BorderColor = borderColor;
        }

        public void OverrideBubbleNumber(Screw screw, int number, Color bubbleColor)
        {
            OverrideBubbleNumber(screw, number, bubbleColor, Color.Black);
        }

        public NumberBubbleConduit GetScrewBubbleConduit(Screw screw)
        {
            if (_conduits.Count == _screwRef.Count)
            {
                return _conduits[_screwRef.IndexOf(screw)];
            }

            return null;
        }

        private GuidePreferenceDataModel GetGuidePreferenceTheScrewBelongsTo(ScrewManager screwManager, Screw screw)
        {
            //screw name can be null/empty when user perform undo/redo
            if (string.IsNullOrEmpty(screw.Name))
            {
                return null;
            }

            return screwManager.GetGuidePreferenceTheScrewBelongsTo(screw);
        }
    }
}
