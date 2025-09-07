using System;

namespace RhinoMtlsCommands.Utilities
{
    internal class ColorScale
    {
        public double[] RedChannel { get; }
        public double[] GreenChannel { get; }
        public double[] BlueChannel { get; }

        public ColorScale(double[] redChannel, double[] greenChannel, double[] blueChannel)
        {
            if (redChannel.Length != greenChannel.Length || greenChannel.Length != blueChannel.Length)
                throw new Exception("Color scale channels must have equal length.");

            this.RedChannel = redChannel;
            this.GreenChannel = greenChannel;
            this.BlueChannel = blueChannel;
        }

        public int ChannelLength => RedChannel.Length;
    }
}