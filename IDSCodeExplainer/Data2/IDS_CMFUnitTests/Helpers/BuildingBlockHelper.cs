using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)
    public class BuildingBlockHelper
    {
        public static ImplantBuildingBlock CreateBuildingBlock(string name)
        {
            return CreateBuildingBlock(name, name);
        }

        public static ImplantBuildingBlock CreateBuildingBlock(string name, string layer)
        {
            return new ImplantBuildingBlock
            {
                ID = 0,
                Name = name,
                GeometryType = ObjectType.Mesh,
                Layer = layer
            };
        }

        public static Brep CreateSphereBrep(double radius)
        {
            var sphereMesh = CreateSphereMesh(radius);

            return Brep.CreateFromMesh(sphereMesh, true); ;
        }

        public static Mesh CreateSphereMesh(double radius)
        {
            return Mesh.CreateFromSphere(new Sphere(Point3d.Origin, radius), 10, 10);
        }

        public static Mesh CreateRectangleMesh(Point3d min, Point3d max, double resolution)
        {
            var box = new BoundingBox(min, max);
            var xLength = Math.Abs(max.X - min.X);
            var yLength = Math.Abs(max.Y - min.Y);
            var zLength = Math.Abs(max.Z - min.Z);
            var xCount = Convert.ToInt32(xLength / resolution);
            var yCount = Convert.ToInt32(yLength / resolution);
            var zCount = Convert.ToInt32(zLength / resolution);
            return Mesh.CreateFromBox(new BoundingBox(min, max), xCount, yCount, zCount);
        }

        public static Guid AddNewBuildingBlock(ImplantBuildingBlock buildingBlock, ObjectManager objectManager)
        {
            var mesh = CreateSphereMesh(5.0);

            return AddNewBuildingBlock(buildingBlock, mesh, objectManager);
        }

        public static Guid AddNewBuildingBlock(ImplantBuildingBlock buildingBlock, Mesh mesh, ObjectManager objectManager)
        {
            return objectManager.AddNewBuildingBlock(buildingBlock, mesh);
        }

        public static List<ImplantBuildingBlock> CreateAndAddBuildingBlocks(int count, ObjectManager objectManager)
        {
            var list = new List<ImplantBuildingBlock>();

            for (var i = 0; i < count; i++)
            {
                var buildingBlock = CreateBuildingBlock($"Building{i}", $"Layer {i}::Building {i}");
                AddNewBuildingBlock(buildingBlock, objectManager);
                list.Add(buildingBlock);
            }

            return list;
        }
    }
#endif
}
