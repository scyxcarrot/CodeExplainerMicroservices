using System;

namespace IDS.Core.V2.Visualization
{
    public class ColorScale
    {
        public double[] RedChannel { get; }
        public double[] GreenChannel { get; }
        public double[] BlueChannel { get; }

        public ColorScale(double[] redChannel, double[] greenChannel, double[] blueChannel)
        {
            if (redChannel.Length != greenChannel.Length || greenChannel.Length != blueChannel.Length)
            {
                throw new Exception("Color scale channels must have equal length.");
            }

            RedChannel = redChannel;
            GreenChannel = greenChannel;
            BlueChannel = blueChannel;
        }

        public int ChannelLength => RedChannel.Length;
    }
}
