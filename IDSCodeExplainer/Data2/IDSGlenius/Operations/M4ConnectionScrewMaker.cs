using IDS.Core.Operations;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.Glenius.Operations
{
    internal class M4ConnectionScrewMaker
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly ImporterViaRunScript importer;

        public M4ConnectionScrewMaker(GleniusImplantDirector director)
        {
            this.director = director;
            objectManager = new GleniusObjectManager(director);
            importer = new ImporterViaRunScript();
        }

        public bool CreateM4ConnectionScrew()
        {
            var resources = new Resources();

            var imported = Import(resources.Screws.ScrewM4Connection.ScrewSTLFile, IBB.M4ConnectionScrew);
            if (imported)
            {
                imported = Import(resources.Screws.ScrewM4Connection.SafetyZone, IBB.M4ConnectionSafetyZone);
            }

            return imported;
        }

        public void AlignM4ConnectionScrewToDefaultPosition()
        {
            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();

            var alignment = new M4ConnectionScrewAlignment(headCoordinateSystem);
            var transform = alignment.GetDefaultPositionTransform();

            var components = BuildingBlocks.GetM4ConnectionScrewComponents();
            foreach (var ibb in components)
            {
                var guid = objectManager.GetBuildingBlockId(ibb);
                if (guid != Guid.Empty)
                {
                    var geometry = objectManager.GetBuildingBlock(ibb).Geometry;
                    geometry.Transform(transform);
                    objectManager.SetBuildingBlock(ibb, geometry, guid);
                }
            }

            var coordinateSystem = alignment.GetM4ConnectionScrewCoordinateSystem();
            objectManager.SetBuildingBlockCoordinateSystem(IBB.M4ConnectionScrew, coordinateSystem);

            director.Document.Views.Redraw();
        }

        private bool Import(string filePath, IBB ibb)
        {
            var guid = importer.Import(filePath).FirstOrDefault();
            if (guid != Guid.Empty)
            {
                var rhObj = director.Document.Objects.Find(guid);
                objectManager.DeleteObject(guid);
                if (rhObj.Geometry is Mesh || rhObj.Geometry is Brep)
                {
                    if (!objectManager.HasBuildingBlock(ibb))
                    {
                        objectManager.AddNewBuildingBlock(ibb, rhObj.Geometry);
                    }
                    else
                    {
                        objectManager.SetBuildingBlock(ibb, rhObj.Geometry, objectManager.GetBuildingBlockId(ibb));
                    }
                    return true;
                }
            }

            return false;
        }
    }
}