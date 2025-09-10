using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace IDS.Core.Utilities
{
    // Obtain thre results of various calculations on the desing
    public class Volume
    {
        public static double BuildingBlockVolume(IImplantDirector director, ImplantBuildingBlock blockType, bool inCC)
        {
            double vol = 0;
            var objectManager = new ObjectManager(director);
            try
            {
                foreach (Guid id in objectManager.GetAllBuildingBlockIds(blockType))
                    vol += RhinoObjectVolume(director.Document.Objects.Find(id), false);

                // Only convert to CC after total volume has been calculated
                if (inCC)
                    return CubeMMtoCC(vol);
                else
                    return vol;
            }
            catch
            {
                return double.MinValue;
            }
        }

        public static double RhinoObjectVolume(RhinoObject rhobj, bool inCC)
        {
            if (rhobj.ObjectType == ObjectType.Brep)
                return BrepVolume(rhobj.Geometry as Brep, inCC);
            else if (rhobj.ObjectType == ObjectType.Mesh)
                return MeshVolume(rhobj.Geometry as Mesh, inCC);
            else
                throw new Exception(string.Format("Cannot calculate volume of an object of type {0}", rhobj.ObjectType));
        }

        public static double BrepVolume(Brep brep, bool inCC)
        {
            try
            {
                double vol = VolumeMassProperties.Compute(brep).Volume;
                // Convert to CC if necessary
                if (inCC)
                    return CubeMMtoCC(vol);
                else
                    return vol;
            }
            catch
            {
                return double.MinValue;
            }
        }

        public static double MeshVolume(Mesh mesh, bool inCC)
        {
            try
            {
                double vol = VolumeMassProperties.Compute(mesh).Volume;
                // Convert to CC if necessary
                if (inCC)
                    return CubeMMtoCC(vol);
                else
                    return vol;
            }
            catch
            {
                return double.MinValue;
            }
        }

        public static double CubeMMtoCC(double volCubeMM)
        {
            return Math.Round(volCubeMM / 1000, 1);
        }
    }
}