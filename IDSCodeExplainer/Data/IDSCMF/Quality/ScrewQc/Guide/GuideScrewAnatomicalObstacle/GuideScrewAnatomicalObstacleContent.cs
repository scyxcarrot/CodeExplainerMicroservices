using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDS.CMF.ScrewQc
{
    public class GuideScrewAnatomicalObstacleContent
    {
        public double DistanceToAnatomicalObstacles { get; set; }

        public GuideScrewAnatomicalObstacleContent()
        {
            DistanceToAnatomicalObstacles = double.NaN;
        }
    }
}
