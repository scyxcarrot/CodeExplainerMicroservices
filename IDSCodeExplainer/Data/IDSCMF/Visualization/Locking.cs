using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class Locking : Core.Operations.Locking
    {
        public static void UnlockImplants(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.PlanningImplant });
        }

        public static void UnlockImplant(RhinoDoc doc, Guid id)
        {
            ManageUnlockedPartOf(doc, IBB.PlanningImplant, id);
        }

        public static void UnlockAllImplantPreview(RhinoDoc doc)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);

            var rhinoObjects = new List<RhinoObject>();
            var objManager = new CMFObjectManager(director);
            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                var implantObject = objManager.GetImplantObject(x);
                rhinoObjects.Add(implantObject);
            });

            rhinoObjects.ForEach(x => doc.Objects.Unlock(x, true));
        }

        public static void UnlockScrews(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.Screw });
        }

        public static void UnlockLandmarks(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.Landmark });
        }

        public static void UnlockGuideFixationScrews(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.GuideFixationScrew });
        }

        public static void UnlockGuideFixationScrewsExceptShared(CMFImplantDirector director)
        {
            UnlockGuideFixationScrews(director.Document);
            
            var screwManager = new ScrewManager(director);
            var allGuideFixationScrews = screwManager.GetAllScrews(true);

            var sharedGuideFixationScrewsList = new List<List<Screw>>();

            allGuideFixationScrews.ForEach(s =>
            {
                var screwItSharedWith = s.GetScrewItSharedWith();
                if (screwItSharedWith.Any() && !sharedGuideFixationScrewsList.Any(l => l.Contains(s)))
                {
                    if (!screwItSharedWith.Contains(s))
                    {
                        screwItSharedWith.Add(s);
                    }
                    sharedGuideFixationScrewsList.Add(screwItSharedWith);
                }
            });

            sharedGuideFixationScrewsList.ForEach(screws =>
            {
                for (var i = 1; i < screws.Count; i++)
                {
                    var x = screws[i];
                    director.Document.Objects.Lock(x, true);
                }
            });
        }
        
        public static void UnlockRegisteredBarrels(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.RegisteredBarrel });
        }

        public static void UnlockGuideLabelTags(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.GuideFixationScrewLabelTag });
        }

        public static void UnlockGuideFlanges(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.GuideFlange });
        }

        public static void UnlockGuideBridge(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.GuideBridge });
        }
        public static void UnlockReferenceEntities(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.ReferenceEntities });
        }

        public static void UnlockImplantMargin(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB>() {IBB.ImplantMargin});
        }

        public static void UnlockImplantSupportGuidingOutline(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB>() { IBB.ImplantSupportGuidingOutline });
        }

        public static void UnlockImplantTransition(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB>() { IBB.ImplantTransition });
        }

        public static void UnlockTeethBlock(RhinoDoc doc)
        {
            ManageUnlockedPartOf(doc, new List<IBB> { IBB.TeethBlock });
        }

        private static void ManageUnlockedPartOf(RhinoDoc doc, IBB block, Guid id)
        {
            // Lock all
            foreach (RhinoObject obj in doc.Objects)
            {
                doc.Objects.Lock(obj.Id, true);
            }

            // Unlock required building blocks (if any)
            if (block != null)
            {
                var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
                var objectManager = new CMFObjectManager(director);

                IEnumerable<Guid> blockIDs = objectManager.GetAllBuildingBlockIds(block);
                if (blockIDs.Contains(id))
                {
                    doc.Objects.Unlock(id, true);
                }
            }
        }

        public static void ManageUnlockedPartOf(RhinoDoc doc, List<IBB> blocks)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var objectManager = new CMFObjectManager(director);

            var implantBuildingBlocks = new List<ImplantBuildingBlock>();
            foreach (var block in blocks)
            {
                implantBuildingBlocks.AddRange(objectManager.GetAllImplantBuildingBlocks(block));
            }

            ManageUnlocked(doc, implantBuildingBlocks);
        }
    }
}
