using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Importer;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Amace.Importers
{
    public class ReadAMaceScrewXmlComponent : IReadScrewXmlComponent<Screw>
    {
        public void OnReadScrewNodeXml(RhinoDoc doc, List<string> screwNameParts, int screwIndex,
            string screwRadius, Point3d screwHead, Point3d screwTip, out Screw screw)
        {
            var screwBrandType = FindScrewBrandType(screwNameParts[0] + '_' + screwNameParts[1]);
            ScrewAlignment screwAlignment = FindScrewAlignment(screwNameParts[2]);

            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            screw = new Screw(director, screwHead, screwTip, screwBrandType, screwAlignment, screwIndex); 

            if (screw.IsBicortical)
            {
                screw.FixedLength = 0.0;
            }
            else
            {
                screw.FixedLength = (screwTip - screwHead).Length;
            }
        }

        private ScrewBrandType FindScrewBrandType(string findString)
        {
            ScrewBrandType screwBrandType;
            ScrewBrandType.TryParse(findString, out screwBrandType);
            return screwBrandType;
        }

        private ScrewAlignment FindScrewAlignment(string findString)
        {
            ScrewAlignment screwAlignment;
            try
            {
                screwAlignment = (ScrewAlignment)Enum.Parse(typeof(ScrewAlignment), findString);
            }
            catch
            {
                return ScrewAlignment.Invalid;
            }
            return screwAlignment;
        }
    }
}
