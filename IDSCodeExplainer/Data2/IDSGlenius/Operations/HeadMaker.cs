using IDS.Core.Operations;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.Glenius.Operations
{
    internal class HeadMaker
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly ImporterViaRunScript importer;

        public HeadMaker(GleniusImplantDirector director)
        {
            this.director = director;
            objectManager = new GleniusObjectManager(director);
            importer = new ImporterViaRunScript();
        }

        public bool CreateHead(HeadType type)
        {
            var imported = ChangeHead(type);
            if (imported)
            {
                var resources = new Resources();
                imported = ImportHeadComponents(resources);
            }

            return imported;
        }

        public bool ChangeHead(HeadType type)
        {
            var resources = new Resources();
            var headPath = resources.Head36StepFile; //Default

            switch (type)
            {
                case HeadType.TYPE_38_MM:
                    headPath = resources.Head38StepFile;
                    break;

                case HeadType.TYPE_42_MM:
                    headPath = resources.Head42StepFile;
                    break;
                default:
                    break;
            }

            return ImportHead(headPath, type);
        }

        private bool ImportHead(string filePath, HeadType type)
        {
            var guid = importer.Import(filePath).FirstOrDefault(); 
            if (guid != Guid.Empty)
            {
                var rhObj = director.Document.Objects.Find(guid);
                objectManager.DeleteObject(guid);
                if (rhObj.Geometry is Brep)
                {
                    if (objectManager.HasBuildingBlock(IBB.Head))
                    {
                        var headOld = objectManager.GetBuildingBlock(IBB.Head) as Head;
                        var headGeometry = rhObj.Geometry as Brep;
                        AlignGeometryCurrentPosition(headGeometry, headOld.CoordinateSystem);
                        var head = new Head(director, headGeometry, type, headOld);
                        objectManager.SetBuildingBlock(IBB.Head, head, objectManager.GetBuildingBlockId(IBB.Head));
                    }
                    else
                    {
                        var head = new Head(director, rhObj.Geometry as Brep, type);
                        objectManager.AddNewBuildingBlock(IBB.Head, head, true);
                    }
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool Import(string filePath, IBB ibb)
        {
            var guid = importer.Import(filePath).FirstOrDefault();
            if (guid != Guid.Empty)
            {
                var rhObj = director.Document.Objects.Find(guid);
                objectManager.DeleteObject(guid);
                if (rhObj.Geometry is Brep)
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

        private bool ImportHeadComponents(Resources resources)
        {
            var imported = Import(resources.CommonTaperAndSafetyStepFile, IBB.TaperMantleSafetyZone);
            if (imported)
            {
                objectManager.ResetBuildingBlockCoordinateSystemToWcs(IBB.TaperMantleSafetyZone);

                imported = Import(resources.CommonCylinderStepFile, IBB.CylinderHat);
                if (imported)
                {
                    objectManager.ResetBuildingBlockCoordinateSystemToWcs(IBB.CylinderHat);

                    imported = Import(resources.CommonProductionRodStepFile, IBB.ProductionRod);
                    if (imported)
                    {
                        objectManager.ResetBuildingBlockCoordinateSystemToWcs(IBB.ProductionRod);
                    }
                }
            }
            return imported;
        }

        private void AlignGeometryCurrentPosition(Brep geometry, Plane currentPosition)
        {
            var transform = Transform.ChangeBasis(currentPosition, Plane.WorldXY);
            geometry.Transform(transform);
        }
    }
}