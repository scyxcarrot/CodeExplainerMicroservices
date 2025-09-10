using System;
using System.Collections.Generic;
using System.Linq;
using IDS.CMFImplantCreation.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class CurveUtilitiesTests
    {
        [TestMethod]
        public void Assert_Remove_Noise_Curve_Test()
        {
            var console = new TestConsole();
            var curves = new List<ICurve>
            {
                new IDSCurve(new List<IPoint3D>
                {
                    new IDSPoint3D(0, 0, 0),
                    new IDSPoint3D(5, 0, 0)
                }),
                new IDSCurve(new List<IPoint3D>
                {
                    new IDSPoint3D(0, 0, 0),
                    new IDSPoint3D(2, 0, 0)
                })
            };

            var output = CurveUtilities.FilterNoiseCurves(console, curves, 2);

            Assert.IsTrue(output.Count == 1, "Output Curve from FilterNoiseCurves should only have 1!");
            Assert.IsTrue(output.Last().Points.Last().Equals(new IDSPoint3D(5, 0, 0)), "Output Curve from FilterNoiseCurves has the wrong curve!");
        }

        [TestMethod]
        public void Assert_Maintain_Valid_curve_Test()
        {
            var console = new TestConsole();
            var curves = new List<ICurve>
            {
                new IDSCurve(new List<IPoint3D>
                {
                    new IDSPoint3D(0, 0, 0),
                    new IDSPoint3D(2.1, 0, 0)
                }),
                new IDSCurve(new List<IPoint3D>
                {
                    new IDSPoint3D(0, 0, 0),
                    new IDSPoint3D(2.1, 0, 0)
                })
            };

            var output = CurveUtilities.FilterNoiseCurves(console, curves, 2);

            Assert.IsTrue(output.Count == 2, "Output Curve from FilterNoiseCurves should return 2!");
        }

        [TestMethod]
        public void Assert_ICurve_Is_Closed()
        {
            var curves = new IDSCurve
            {
                Points =
                {
                    new IDSPoint3D(12.32, 35.2, 234.6),
                    new IDSPoint3D(23.1, 546.1, 34.2),
                    new IDSPoint3D(434, 42.3, 163.34),
                    new IDSPoint3D(12.32, 35.2, 234.6)
                }
            };

            Assert.IsTrue(curves.IsClosed(), "Curve is closed!");
        }

        [TestMethod]
        public void Assert_ICurve_Is_Not_Closed()
        {
            var curves = new IDSCurve
            {
                Points =
                {
                    new IDSPoint3D(12.32, 35.2, 234.6),
                    new IDSPoint3D(23.1, 546.1, 34.2),
                    new IDSPoint3D(434, 42.3, 163.34),
                    new IDSPoint3D(12.22, 35.2, 234.6)
                }
            };

            Assert.IsFalse(curves.IsClosed(), "Curve is not closed!");
        }

        [TestMethod]
        public void Assert_Make_Closed_Curve()
        {
            var curves = new IDSCurve
            {
                Points =
                {
                    new IDSPoint3D(12.32, 35.2, 234.6),
                    new IDSPoint3D(23.1, 546.1, 34.2),
                    new IDSPoint3D(434, 42.3, 163.34),
                    new IDSPoint3D(12.22, 35.2, 234.6)
                }
            };

            curves.MakeClosed(0.1);
            Assert.IsTrue(curves.IsClosed(), "Curve should be closed!");
        }

        [TestMethod]
        public void Assert_Remain_Open_Curve()
        {
            var curves = new IDSCurve
            {
                Points =
                {
                    new IDSPoint3D(12.32, 35.2, 234.6),
                    new IDSPoint3D(23.1, 546.1, 34.2),
                    new IDSPoint3D(434, 42.3, 163.34),
                    new IDSPoint3D(12.12, 35.2, 234.6)
                }
            };

            curves.MakeClosed(0.1);
            Assert.IsFalse(curves.IsClosed(), "Curve should be closed!");
        }
    }

#endif
}
