using IDS.Core.V2.Visualization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class ColorScaleTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception),
            "Red color channel have different length with other was allowed")]
        public void RefColorChannelLacking()
        {
            var redChannel = new[] { 0.0, 0.0 };
            var greenChannel = new[] { 0.0, 0.0, 0.0 };
            var blueChannel = new[] { 0.0, 0.0, 0.0 };

            var colorScale = new ColorScale(redChannel, greenChannel, blueChannel);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception),
            "Green color channel have different length with other was allowed")]
        public void GreenColorChannelLacking()
        {
            var redChannel = new [] {0.0, 0.0, 0.0};
            var greenChannel = new [] { 0.0, 0.0 };
            var blueChannel = new [] { 0.0, 0.0, 0.0 };

            var colorScale = new ColorScale(redChannel, greenChannel, blueChannel);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception),
            "Blue color channel have different length with other was allowed")]
        public void BlueColorChannelLacking()
        {
            var redChannel = new[] { 0.0, 0.0, 0.0 };
            var greenChannel = new[] { 0.0, 0.0, 0.0 };
            var blueChannel = new[] { 0.0, 0.0 };

            new ColorScale(redChannel, greenChannel, blueChannel);
        }
    }
}
