using IDS.CMFImplantCreation.DTO;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.Helpers
{
    public static class ComponentInfoExtensions
    {
        public static T ToDefaultComponentInfo<T>(this IFileIOComponentInfo fileIOComponentInfo, IConsole console) where T : IComponentInfo, new()
        {
            var componentInfo = new T
            {
                Id = fileIOComponentInfo.Id,
                DisplayName = fileIOComponentInfo.DisplayName,
                IsActual = fileIOComponentInfo.IsActual,
                NeedToFinalize = fileIOComponentInfo.NeedToFinalize
            };

            try
            {
                if (!string.IsNullOrEmpty(fileIOComponentInfo.ClearanceMeshSTLFilePath))
                {
                    componentInfo.ClearanceMesh = ImportExport.LoadFromStlFile(console, fileIOComponentInfo.ClearanceMeshSTLFilePath);
                }

                componentInfo.Subtractors = new List<IMesh>();
                if (fileIOComponentInfo.SubtractorsSTLFilePaths != null)
                {
                    foreach (var stl in fileIOComponentInfo.SubtractorsSTLFilePaths)
                    {
                        componentInfo.Subtractors.Add(ImportExport.LoadFromStlFile(console, stl));
                    }
                }

                componentInfo.ComponentMeshes = new List<IMesh>();
                if (fileIOComponentInfo.ComponentMeshesSTLFilePaths != null)
                {
                    foreach (var stl in fileIOComponentInfo.ComponentMeshesSTLFilePaths)
                    {
                        componentInfo.ComponentMeshes.Add(ImportExport.LoadFromStlFile(console, stl));
                    }
                }
            }
            catch (Exception e)
            {
                console.WriteErrorLine($"Exception thrown: {fileIOComponentInfo.Id} - {e.Message}");
                throw e;
            }

            return componentInfo;
        }

        public static T ToActualComponentInfo<T>(this PastilleComponentInfo pastilleComponentInfo) where T : PastilleComponentInfo, new()
        {
            var componentInfo = new T
            {
                Id = pastilleComponentInfo.Id,
                DisplayName = pastilleComponentInfo.DisplayName,
                IsActual = pastilleComponentInfo.IsActual,
                NeedToFinalize = pastilleComponentInfo.NeedToFinalize,
                ClearanceMesh = pastilleComponentInfo.ClearanceMesh,
                Subtractors = pastilleComponentInfo.Subtractors,
                ComponentMeshes = pastilleComponentInfo.ComponentMeshes,
                ScrewHeadPoint = pastilleComponentInfo.ScrewHeadPoint,
                ScrewDirection = pastilleComponentInfo.ScrewDirection,
                Location = pastilleComponentInfo.Location,
                Direction = pastilleComponentInfo.Direction,
                Diameter = pastilleComponentInfo.Diameter,
                Thickness = pastilleComponentInfo.Thickness,
                ScrewType = pastilleComponentInfo.ScrewType,
                SupportRoIMesh = pastilleComponentInfo.SupportRoIMesh
            };

            return componentInfo;
        }

        public static T ToActualComponentInfo<T>(this ConnectionComponentInfo connectionComponentInfo) where T : ConnectionComponentInfo, new()
        {
            var componentInfo = new T
            {
                Id = connectionComponentInfo.Id,
                DisplayName = connectionComponentInfo.DisplayName,
                ClearanceMesh = connectionComponentInfo.ClearanceMesh,
                Subtractors = connectionComponentInfo.Subtractors,
                ComponentMeshes = connectionComponentInfo.ComponentMeshes,
                IsActual = connectionComponentInfo.IsActual,
                NeedToFinalize = connectionComponentInfo.NeedToFinalize,
                ConnectionCurve = connectionComponentInfo.ConnectionCurve,
                Width = connectionComponentInfo.Width,
                Thickness = connectionComponentInfo.Thickness,
                AverageConnectionDirection = connectionComponentInfo.AverageConnectionDirection,
                SupportRoIMesh = connectionComponentInfo.SupportRoIMesh,
                SupportMeshFull = connectionComponentInfo.SupportMeshFull,
            };

            return componentInfo;
        }
    }
}
