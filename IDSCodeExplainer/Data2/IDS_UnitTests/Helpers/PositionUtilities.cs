using System;

namespace IDS.Testing
{
    public static class PositionUtilities
    {
        public static void RandomXY(out double x, out double y, double max, double min)
        {
            var random = new Random();
            var realMax = max - min;

            x = random.NextDouble() * realMax - min;
            y = random.NextDouble() * realMax - min;
        }
    }
}
