using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Operations
{
    public static class ScrewAideDocumentOperation
    {
        private static List<IBB> PersistantGuideAides = new List<IBB>
        {
            IBB.RegisteredBarrel
        };

        public static void OnAddToDocument(RhinoDoc doc, Screw screw)
        {
            // Restore Persisted Guide Aide Id
            foreach (var aide in PersistantGuideAides)
            {
                var screwAideID = screw.Attributes.UserDictionary.GetGuid(GetPersistantKey(aide), Guid.Empty);
                if (screwAideID != Guid.Empty)
                {
                    screw.ScrewGuideAidesInDocument.Add(aide, screwAideID);
                    screw.Attributes.UserDictionary.Remove(GetPersistantKey(aide));
                }
            }
        }

        public static void OnDeleteFromDocument(RhinoDoc doc, Screw screw)
        {
            var objectManager = new CMFObjectManager(screw.Director);

            // Persist Guide Aide Id
            foreach (var aide in PersistantGuideAides)
            {
                if (screw.ScrewGuideAidesInDocument.ContainsKey(aide))
                {
                    var id = screw.ScrewGuideAidesInDocument[aide];
                    screw.Attributes.UserDictionary.Set(GetPersistantKey(aide), id);
                    screw.CommitChanges();
                    objectManager.DeleteObject(id);
                    screw.ScrewGuideAidesInDocument.Remove(aide);
                }
            }
        }

        private static string GetPersistantKey(IBB aide)
        {
            return $"Persistant_{GetKey(aide)}";
        }

        private static string GetKey(IBB aide)
        {
            return $"{aide}";
        }
    }
}
