using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class SplineInterpolationTests
    {
        private const double Epsilon = 0.001;

        [TestMethod]
        public void Check_FitCurve_Returns_Same_Mesh_As_Original_Algo()
        {
            // Arrange
            var pastille0 = new DotPastille { Location = new IDSPoint3D(1, 0, 0) };
            var pastille1 = new DotPastille { Location = new IDSPoint3D(0, 0, 0) };
            var pastille2 = new DotPastille { Location = new IDSPoint3D(0, 1, 0) };
            var pastille3 = new DotControlPoint { Location = new IDSPoint3D(0.5, 0.5, 0) };
            var pastille4 = new DotControlPoint { Location = new IDSPoint3D(0.75, 1, 0) };
            var pastille5 = new DotPastille { Location = new IDSPoint3D(1, 2, 0) };
            var pastilleList = new List<IDot>
            {
                pastille0,
                pastille1,
                pastille2,
                pastille3,
                pastille4,
                pastille5,
            };
            pastilleList.ForEach(pastille => 
                pastille.Direction = new IDSVector3D(0, 0, 1));

            var originalConnectionList = new List<IConnection>()
            {
                new ConnectionPlate() { A = pastilleList[0], B = pastilleList[1] },
                new ConnectionPlate() { A = pastilleList[1], B = pastilleList[2] },
                new ConnectionPlate() { A = pastilleList[1], B = pastilleList[3] },
                new ConnectionPlate() { A = pastilleList[3], B = pastilleList[4] },
                new ConnectionPlate() { A = pastilleList[4], B = pastilleList[5] },
            };
            var originalCurves = ImplantCreationUtilities.CreateImplantConnectionCurves(originalConnectionList, null, false);
            var originalCurvesIds = originalCurves.Select(originalCurve => originalCurve.ToICurve()).ToList();

            // Act
            var dotCluster =
                ConnectionUtilities.CreateDotCluster(originalConnectionList);
            var newCurves = new List<ICurve>();
            foreach (var cluster in dotCluster)
            {
                if (cluster.Count < 2)
                {
                    continue;
                }
                var points = cluster.Select(dot => dot.Location).ToList();
                var curve = CreateSplineUtilities.FitCurve(
                    points, 3, 0.01, SimplificationAlgorithm.None);

                newCurves.Add(curve);
            }

            // Assert
            foreach (var newCurve in newCurves)
            {
                // match newCurve to originalCurvesIds
                ICurve matchedOriginalCurveIds = new IDSCurve();
                foreach (var originalCurveIds in originalCurvesIds)
                {
                    if (originalCurveIds.Points.First()
                            .EpsilonEquals(newCurve.Points.First(), 0.01) &&
                        originalCurveIds.Points.Last()
                            .EpsilonEquals(newCurve.Points.Last(), 0.01))
                    {
                        matchedOriginalCurveIds = originalCurveIds;
                        break;
                    }
                }
                Assert.IsFalse(matchedOriginalCurveIds.Points.Count == 0, "There should be a matching curve for every newCurve");
                
                foreach (var pointA in matchedOriginalCurveIds.Points)
                {
                    var minimumDistance = double.MaxValue;
                    foreach (var pointB in newCurve.Points)
                    {
                        var distance = pointB.DistanceTo(pointA);
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                        }
                    }

                    Assert.IsTrue(minimumDistance < 0.02, "One of the points in the curve does not match");
                }
            }
        }

        [TestMethod]
        public void Check_FitCurve_Returns_Two_Points_Only_If_Two_Points_Given()
        {
            // Arrange
            var inputPoints = new List<IPoint3D>()
            {
                new IDSPoint3D(0, 0, 0),
                new IDSPoint3D(1, 0, 0),
            };

            // Act
            var fitCurve = CreateSplineUtilities.FitCurve(
                inputPoints, 3, 0.01, SimplificationAlgorithm.Linear);

            // Assert
            Assert.AreEqual(2, fitCurve.Points.Count);
            for (var index = 0; index < inputPoints.Count; index++)
            {
                Assert.IsTrue(inputPoints[index]
                    .EpsilonEquals(fitCurve.Points[index], Epsilon));
            }
        }

        [TestMethod]
        public void Check_FitCurve_Returns_User_Points_Even_If_Points_Are_In_Straight_Line()
        {
            // Arrange
            var inputPoints = new List<IPoint3D>()
            {
                new IDSPoint3D(0, 0, 0),
                new IDSPoint3D(1, 0, 0),
                new IDSPoint3D(4, 0, 0),
            };

            // Act
            var fitCurve = CreateSplineUtilities.FitCurve(
                inputPoints, 1, 0.01, SimplificationAlgorithm.Linear);

            // Assert
            Assert.AreEqual(3, fitCurve.Points.Count);
            for (var index = 0; index < inputPoints.Count; index++)
            {
                Assert.IsTrue(inputPoints[index]
                    .EpsilonEquals(fitCurve.Points[index], Epsilon));
            }
        }

        [TestMethod]
        public void Check_FitCurve_Returns_User_Points_If_Points_Are_Discontinuous()
        {
            // Arrange
            var inputPoints = new List<IPoint3D>()
            {
                new IDSPoint3D(0, 0, 0),
                new IDSPoint3D(1, 0, 0),
                new IDSPoint3D(0, 1, 0),
            };

            // Act
            var fitCurve = CreateSplineUtilities.FitCurve(
                inputPoints, 1, 0.01, SimplificationAlgorithm.Linear);

            // Assert
            Assert.AreEqual(3, fitCurve.Points.Count);
            for (var index = 0; index < inputPoints.Count; index++)
            {
                Assert.IsTrue(inputPoints[index]
                    .EpsilonEquals(fitCurve.Points[index], Epsilon));
            }
        }
    }
}
