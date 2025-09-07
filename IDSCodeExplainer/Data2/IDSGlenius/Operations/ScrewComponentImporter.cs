using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using Rhino;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ScrewComponentImporter
    {
        private readonly IDS.Glenius.Resources resources = new IDS.Glenius.Resources();
        private readonly ImporterViaRunScript importer = new ImporterViaRunScript();

        private Brep GetBrep(Guid guid)
        {
            Brep brepTmp = null;
            if (guid != Guid.Empty)
            {
                //Any imported .Stp files will be added in the document, and layer will be created.
                //Have to delete it once a copy is made into the memory
                var rhObj = RhinoDoc.ActiveDoc.Objects.Find(guid);

                if (rhObj.Geometry is Brep)
                {
                    brepTmp = (Brep)rhObj.Geometry;
                }
                else
                {
                    throw new IDSException("ScrewBrepComponents.FindBrep is not a Brep!");
                }
            }

            if (brepTmp != null)
            {
                Brep brep = new Brep();
                brep.Append(brepTmp);
                RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
                return brep;
            }
            else
            {
                return null;
            }
        }


        public Brep ImportScrewHeadBrep(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw3Dot5Locking.HeadCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0Locking.HeadCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0NonLocking.HeadCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public Brep ImportScrewMantleBrep(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw3Dot5Locking.Mantle).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0Locking.Mantle).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0NonLocking.Mantle).FirstOrDefault();
                        return GetBrep(guid);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public Brep ImportScrewSafetyZoneBrep(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw3Dot5Locking.SafetyZone).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0Locking.SafetyZone).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0NonLocking.SafetyZone).FirstOrDefault();
                        return GetBrep(guid);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public Brep ImportScrewDrillGuideCylinderBrep(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw3Dot5Locking.GuideCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0Locking.GuideCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        var guid = importer.Import(resources.Screws.Screw4Dot0NonLocking.GuideCylinder).FirstOrDefault();
                        return GetBrep(guid);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

    }
}
