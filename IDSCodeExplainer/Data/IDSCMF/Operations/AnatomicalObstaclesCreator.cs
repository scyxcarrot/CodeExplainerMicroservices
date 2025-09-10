using IDS.CMF.Query;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class AnatomicalObstaclesCreator
    {
        private readonly CMFImplantDirector _director;

        public AnatomicalObstaclesCreator(CMFImplantDirector director)
        {
            this._director = director;
        }

        public IEnumerable<Mesh> CreateDefaultAnatomicalObstacles()
        {
            var query = new DefaultAnatomicalObstacleQuery(new CMFObjectManager(_director));
            var anatomicalObstacles = query.GetDefaultAnatomicalObstacles().Select(mesh => mesh.DuplicateMesh());
            return anatomicalObstacles;
        }
    }
}
