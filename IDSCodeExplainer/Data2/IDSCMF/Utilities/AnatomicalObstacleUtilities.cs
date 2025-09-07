using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public static class AnatomicalObstacleUtilities
    {
        private static string keyAnatomicalObstacleOrigin = "anatomical_obstacle_origin";
        
        public static RhinoObject GetAnatomicalObstacle(CMFObjectManager objectManager, RhinoObject rhinoObject)
        {
            var anatomicalObstacles = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles).ToList();
            return GetAnatomicalObstacle(anatomicalObstacles, rhinoObject);
        }

        public static RhinoObject GetAnatomicalObstacle(List<RhinoObject> anatomicalObstacles, RhinoObject rhinoObject)
        {
            var identicalList = anatomicalObstacles.Where(x => ((Mesh)x.Geometry).IsEqual((Mesh)rhinoObject.Geometry));
            var duplicateList = identicalList.ToList();
            foreach (var obj in identicalList)
            {
                if (obj.Attributes.UserDictionary.ContainsKey(keyAnatomicalObstacleOrigin))
                {
                    if (obj.Attributes.UserDictionary.GetString(keyAnatomicalObstacleOrigin) == rhinoObject.Name)
                    {
                        return obj;
                    }
                    else
                    {
                        duplicateList.Remove(obj);
                    }
                }
            }
            return duplicateList.FirstOrDefault();
        }

        public static void AddAsAnatomicalObstacle(CMFObjectManager objectManager, RhinoObject rhinoObject)
        {
            var duplicatedMesh = ((Mesh)rhinoObject.Geometry).DuplicateMesh();
            var guid = objectManager.AddNewBuildingBlock(IBB.AnatomicalObstacles, duplicatedMesh);

            var anatomicalObstacleObject = rhinoObject.Document.Objects.Find(guid);

            UserDictionaryUtilities.ModifyUserDictionary(anatomicalObstacleObject, keyAnatomicalObstacleOrigin,
                rhinoObject.Name);
        }

        public static void AddAsAnatomicalObstacle(CMFObjectManager objectManager, Mesh anatomicalObstacleOriginMesh, 
            string anatomicalObstacleOriginName)
        {
            var duplicatedMesh = anatomicalObstacleOriginMesh.DuplicateMesh();
            var guid = objectManager.AddNewBuildingBlock(IBB.AnatomicalObstacles, duplicatedMesh);

            if (string.IsNullOrEmpty(anatomicalObstacleOriginName))
            {
                return;
            }

            var anatomicalObstacleObject = objectManager.GetDirector().Document.Objects.Find(guid);

            UserDictionaryUtilities.ModifyUserDictionary(anatomicalObstacleObject, keyAnatomicalObstacleOrigin,
                anatomicalObstacleOriginName);
        }

        public static List<string> GetAnatomicalObstacleOriginPartNames(CMFObjectManager objectManager)
        {
            var anatomicalObstacles = objectManager.GetAllBuildingBlocks(IBB.AnatomicalObstacles).ToList();
            var list = new List<string>();

            foreach (var obj in anatomicalObstacles)
            {
                var rhinoObjectName = GetAnatomicalObstacleOriginPartName(obj);
                if (!string.IsNullOrEmpty(rhinoObjectName))
                {
                    var originPartName = rhinoObjectName.Replace(ProPlanImport.ObjectPrefix, string.Empty);
                    list.Add(originPartName);
                }
            }

            return list;
        }

        public static string GetAnatomicalObstacleOriginPartName(RhinoObject anatomicalObstacle)
        {
            if (anatomicalObstacle.Attributes.UserDictionary.ContainsKey(keyAnatomicalObstacleOrigin))
            {
                return anatomicalObstacle.Attributes.UserDictionary.GetString(keyAnatomicalObstacleOrigin);
            }
            return String.Empty;
        }

        // Returns NAN if no object is passed
        // this can happen if somehow there is no anatomical obstacles indicated by user
        public static double GetScrewMinDistance(Brep qcCylinder, Mesh objects, bool iterateAllVertices)
        {
            var distance = double.NaN;

            if (objects == null || !objects.Vertices.Any())
            {
                return distance;
            }

            var meshingParameters = MeshParameters.GetForScrewMinDistanceCheck();
            var qcCyl = MeshUtilities.ConvertBrepToMesh(qcCylinder, true, meshingParameters);
            distance = GetScrewMinDistance(qcCyl, objects, iterateAllVertices);
            qcCyl.Dispose();
            return distance;
        }

        // Returns NAN if no object is passed
        // this can happen if somehow there is no anatomical obstacles indicated by user
        public static double GetScrewMinDistance(Mesh qcCylinder, Mesh obstacleObj, bool iterateAllVertices)
        {
            var distance = double.NaN;

            if (obstacleObj == null)
            {
                return distance;
            }

            if (!iterateAllVertices)
            {
                distance = MeshUtilities.Mesh2MeshMinimumDistance(qcCylinder, obstacleObj, Constants.QCValues.MinDistance);
            }
            else
            {
                distance = MeshUtilities.Mesh2MeshMinimumDistance(qcCylinder, obstacleObj);
            }
            return distance;
        }

        public static string FormatScrewAnatomicalObstacleResult(double distanceToAnatomicalObstacles)
        {
            return !double.IsNaN(distanceToAnatomicalObstacles) ? string.Format(CultureInfo.InvariantCulture, "{0:0.##}", distanceToAnatomicalObstacles) : "N/A";
        }

        public static QcDocCellColor DistToTableDataColor(double distanceToAnatomicalObstacles)
        {
            QcDocCellColor cellColor;

            if (distanceToAnatomicalObstacles >= 1)
            {
                cellColor = QcDocCellColor.Green;
            }
            else if (distanceToAnatomicalObstacles >= 0.5)
            {
                cellColor = QcDocCellColor.Yellow;
            }
            else if (distanceToAnatomicalObstacles > 0)
            {
                cellColor = QcDocCellColor.Orange;
            }
            else if (double.IsNaN(distanceToAnatomicalObstacles))
            {
                cellColor = QcDocCellColor.Green;
            }
            else
            {
                cellColor = QcDocCellColor.Red;
            }

            return cellColor;
        }
    }
}