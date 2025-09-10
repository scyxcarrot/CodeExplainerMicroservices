using IDS.CMF.V2.Logics;
using IDS.EnlightCMFIntegration.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.Loader
{
    public static class EnlightCMFLoaderUtilities
    {
        private const string ReferenceObjectName = "Ref object";

        public static List<IObjectProperties> FilterParts(List<IObjectProperties> objectProperties)
        {
            var partObjects = UpdatePartNames(objectProperties);
            //Last retrieved object will be taken if there are duplicated name 
            partObjects = partObjects.GroupBy(part => part.Name.ToLower()).Select(parts => parts.LastOrDefault()).ToList();

            var proPlanImportComponent = new ProPlanImportComponentV2();
            var filteredPartNames = proPlanImportComponent.GetRequiredPartNames(partObjects.Select(part => part.Name));
            var filteredParts = partObjects.Where(part => filteredPartNames.Contains(part.Name)).ToList();
            return filteredParts;
        }

        public static List<IObjectProperties> UpdatePartNames(List<IObjectProperties> objectProperties)
        {
            /*
            First, filter out parts without internal name.
            IDS checks if there are any objects with 'Ref object' as internal name.
				1. If no, it fully uses internal names.
                2. If yes, it looks at the UI names of these objects. If it recognizes '03MAX' it overwrites the workflow object '03MAX'
            */
            var referenceObjects = objectProperties.Where(part => IsReferenceObject(part)).ToList();
            var nonReferenceInternalObjects = objectProperties.Where(part => !string.IsNullOrEmpty(part.InternalName) && !IsReferenceObject(part)).ToList();
            var transferredObjects = new List<IObjectProperties>();

            for (var i = 0; i < nonReferenceInternalObjects.Count; i++)
            {
                var internalObject = nonReferenceInternalObjects[i];
                if (!referenceObjects.Any(part => part.Name.ToLower().Equals(internalObject.InternalName.ToLower())))
                {
                    //update name for internalObject
                    internalObject.Name = internalObject.InternalName;
                    transferredObjects.Add(internalObject);
                }
            }

            referenceObjects.AddRange(transferredObjects);
            return referenceObjects;
        }

        public static bool IsReferenceObject(IObjectProperties objectProperties)
        {
            return !string.IsNullOrEmpty(objectProperties.InternalName) && objectProperties.InternalName.ToLower().Equals(ReferenceObjectName.ToLower());
        }
    }
}
