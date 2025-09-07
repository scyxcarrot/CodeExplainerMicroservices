using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.V2.ScrewQc;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("12D298AF-34EB-4212-97E7-CEC146DA04C9")]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMF_TestDummyGuideScrewQcBubbleAndQcDoc : CMF_TestGuideScrewQcBubbleAndQcDoc
    {
        #region Dummy Class
        private class DummyImplantScrewInfoRecord : ScrewInfoRecord
        {
            private DummyImplantScrewInfoRecord(Screw screw, int nCase) : base(ScrewQcData.Create(screw))
            {
                CaseGuid = Guid.NewGuid();
                CaseName = string.Empty;
                NCase = nCase;
                IsGuideFixationScrew = false;
            }

            private DummyImplantScrewInfoRecord(DummyImplantScrewInfoRecord record) : base(record)
            {
                IsGuideFixationScrew = false;
                CaseGuid = record.CaseGuid;
                CaseName = record.CaseName;
                NCase = record.NCase;
            }

            public override Guid CaseGuid { get; }
            public override string CaseName { get; }
            public override int NCase { get; }
            public override bool IsGuideFixationScrew { get; }

            public override string GetScrewNumber()
            {
                return ScrewManager.GetScrewNumberWithImplantNumber(Index, NCase);
            }

            public override string GetScrewNumberForScrewQcBubble()
            {
                return $"{Index}";
            }

            public override object Clone()
            {
                return new DummyImplantScrewInfoRecord(this);
            }

            public static DummyImplantScrewInfoRecord CreateDummyImplantScrewInfoRecord(Screw screw, int index, int nCase)
            {
                var dummyScrew = new Screw(screw.Director, screw.HeadPoint, screw.TipPoint, screw.ScrewAideDictionary, index, screw.ScrewType)
                {
                    Id = Guid.NewGuid()
                };
                return new DummyImplantScrewInfoRecord(dummyScrew, nCase);
            }
        }

        private class DummyGuideScrewInfoRecord : ScrewInfoRecord
        {
            private DummyGuideScrewInfoRecord(Screw screw, int nCase) : base(ScrewQcData.Create(screw))
            {
                CaseGuid = Guid.NewGuid();
                CaseName = string.Empty;
                NCase = nCase;
                IsGuideFixationScrew = true;
            }

            private DummyGuideScrewInfoRecord(DummyGuideScrewInfoRecord record) : base(record)
            {
                IsGuideFixationScrew = true;
                CaseGuid = record.CaseGuid;
                CaseName = record.CaseName;
                NCase = record.NCase;
            }

            public override Guid CaseGuid { get; }
            public override string CaseName { get; }
            public override int NCase { get; }
            public override bool IsGuideFixationScrew { get; }

            public override string GetScrewNumber()
            {
                return ScrewManager.GetScrewNumberWithGuideNumber(Index, NCase);
            }

            public override string GetScrewNumberForScrewQcBubble()
            {
                return $"{Index}";
            }

            public override object Clone()
            {
                return new DummyGuideScrewInfoRecord(this);
            }

            public static DummyGuideScrewInfoRecord CreateDummyGuideScrewInfoRecord(Screw screw, int index, int nCase)
            {
                var dummyScrew = new Screw(screw.Director, screw.HeadPoint, screw.TipPoint, screw.ScrewAideDictionary, index, screw.ScrewType)
                {
                    Id = Guid.NewGuid()
                };
                return new DummyGuideScrewInfoRecord(dummyScrew, nCase);
            }
        }

        private class DummyClearanceVicinityChecker : ClearanceVicinityChecker
        {
            private readonly bool _getSuccessResult;

            public DummyClearanceVicinityChecker(CMFImplantDirector director, bool getSuccessResult) : base(director, false)
            {
                _getSuccessResult = getSuccessResult;
            }

            protected override ClearanceVicinityResult CheckForSharedScrew(Screw screw)
            {
                var content = new ClearanceVicinityContent();
                var random = new Random();

                if (!_getSuccessResult)
                {
                    content.ClearanceVicinityGuideScrews = new List<ScrewInfoRecord>()
                    {
                        DummyGuideScrewInfoRecord.CreateDummyGuideScrewInfoRecord(screw, random.Next(0, 1000),
                            random.Next(0, 1000))
                    };
                }

                return new ClearanceVicinityResult(ScrewQcCheckName, content);
            }

            protected override void CheckAndUpdateForNonSharedScrew(Screw screw, ClearanceVicinityResult result)
            {
                if (!_getSuccessResult)
                {
                    var random = new Random();
                    var vicinatedBarrels = new List<ScrewInfoRecord>()
                    {
                        DummyImplantScrewInfoRecord.CreateDummyImplantScrewInfoRecord(screw, random.Next(0, 1000),
                            random.Next(0, 1000))
                    };
                    result.UpdateResult(vicinatedBarrels);
                }
            }
        }

        private class DummyImplantScrewGaugeIntersectChecker : ImplantScrewGaugeIntersectChecker
        {
            private readonly bool _getSuccessResult;

            public DummyImplantScrewGaugeIntersectChecker(CMFImplantDirector director, bool getSuccessResult) : base(director, null)
            {
                _getSuccessResult = getSuccessResult;
            }

            protected override ImplantScrewGaugeIntersectResult CheckForSharedScrew(Screw screw)
            {
                var random = new Random();
                var content = new ImplantScrewGaugeIntersectContent();

                if (!_getSuccessResult)
                {
                    content.IntersectedImplantScrewGauges = new List<ScrewInfoRecord>()
                        {
                            DummyImplantScrewInfoRecord.CreateDummyImplantScrewInfoRecord(screw, random.Next(0, 1000),
                                random.Next(0, 1000))
                        };
                }

                return new ImplantScrewGaugeIntersectResult(ScrewQcCheckName, content);
            }
        }

        private class DummyGuideScrewIntersectChecker : GuideScrewIntersectChecker
        {
            private readonly bool _getSuccessResult;

            public DummyGuideScrewIntersectChecker(CMFImplantDirector director, bool getSuccessResult) : base(director)
            {
                _getSuccessResult = getSuccessResult;
            }

            protected override GuideScrewIntersectResult CheckForSharedScrew(Screw screw)
            {
                var content = new GuideScrewIntersectContent();
                var random = new Random();

                if (!_getSuccessResult)
                {
                    content.IntersectedGuideScrews = new List<ScrewInfoRecord>()
                    {
                        DummyGuideScrewInfoRecord.CreateDummyGuideScrewInfoRecord(screw, random.Next(0, 1000),
                            random.Next(0, 1000))
                    };
                }

                return new GuideScrewIntersectResult(ScrewQcCheckName, content);
            }
        }

        private class DummyImplantScrewIntersectChecker : ImplantScrewIntersectChecker
        {
            private readonly bool _getSuccessResult;

            public DummyImplantScrewIntersectChecker(CMFImplantDirector director, bool getSuccessResult) : base(director, null)
            {
                _getSuccessResult = getSuccessResult;
            }

            protected override ImplantScrewIntersectResult CheckForSharedScrew(Screw screw)
            {
                var random = new Random();

                var content = new ImplantScrewIntersectContent();
                
                if (!_getSuccessResult)
                {
                    content.IntersectedImplantScrews = new List<ScrewInfoRecord>()
                    {
                        DummyImplantScrewInfoRecord.CreateDummyImplantScrewInfoRecord(screw, random.Next(0, 1000),
                            random.Next(0, 1000))
                    };
                }

                return new ImplantScrewIntersectResult(ScrewQcCheckName, content);
            }
        }

        #endregion
        
        public CMF_TestDummyGuideScrewQcBubbleAndQcDoc()
        {
            Instance = this;
        }

        public new static CMF_TestDummyGuideScrewQcBubbleAndQcDoc Instance { get; private set; }

        public override string EnglishName => "CMF_TestDummyGuideScrewQcBubbleAndQcDoc";

        protected override string QcDocName => "Dummy_Guide_Screw_Qc_Doc.html";

        protected override ScrewQcCheckerManager CreateScrewQcCheckerManager(CMFImplantDirector director)
        {
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Select the expected screw QC result");
            getOption.AcceptNothing(true);
            var resultQc = new OptionToggle(true, "Failed", "Pass");
            getOption.AddOptionToggle("Result", ref resultQc);

            while (true)
            {
                var result = getOption.Get();

                if (result == GetResult.Cancel || result == GetResult.NoResult)
                {
                    return null;
                }

                if (result == GetResult.Nothing)
                {
                    break;
                }
            }

            var getSuccessResult = resultQc.CurrentValue;

            return ScrewQcUtilities.CreateScrewQcManager(director, new List<IScrewQcChecker>()
            {
                new DummyClearanceVicinityChecker(director, getSuccessResult),
                new DummyImplantScrewIntersectChecker(director, getSuccessResult),
                new DummyGuideScrewIntersectChecker(director, getSuccessResult),
                new DummyImplantScrewGaugeIntersectChecker(director, getSuccessResult),
            });
        }
    }
#endif
}
