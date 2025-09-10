using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.Quality
{
    public class MeshObject : MeshProperties
    {
        public string Name { get; private set; }
        public Transform Transform { get; private set; }

        public MeshObject(Mesh mesh, string layerPath, string name, Transform transform) : base(mesh, layerPath)
        {
            Name = name;
            Transform = transform;
        }
    }

    public class CMFOriginalPositionedScrewAnalysis
    {
        private readonly List<Mesh> _originalOsteotomies;
        private ScrewRegistration _screwRegistration;

        public CMFOriginalPositionedScrewAnalysis(List<Mesh> originalOsteotomies)
        {            
            this._originalOsteotomies = originalOsteotomies;
        }

        public List<KeyValuePair<Screw, Transform>> GetAllScrewsAtOriginalPosition(IEnumerable<Screw> screws, out Dictionary<Screw, Screw> screwsMap)
        {
            var screwsAtOriginalAndTransform = new List<KeyValuePair<Screw, Transform>>();
            screwsMap = new Dictionary<Screw, Screw>();

            foreach (var screw in screws)
            {
                Transform transformationToOriginal;

                if (_screwRegistration == null)
                {
                    _screwRegistration = new ScrewRegistration(screw.Director, true);
                }

                var screwAtOriginalHelper = new CMFScrewAtOriginalPositionHelper(_screwRegistration);
                var screwAtOriginal = screwAtOriginalHelper.GetScrewAtOriginalPosition(screw, out transformationToOriginal);

                if (screwAtOriginal == null)
                {
                    continue;
                }

                screwsAtOriginalAndTransform.Add(new KeyValuePair<Screw, Transform>(screwAtOriginal, transformationToOriginal));
                screwsMap.Add(screwAtOriginal, screw);
            }

            return screwsAtOriginalAndTransform;
        }

        public void CleanUp()
        {
            if (_screwRegistration != null)
            {
                _screwRegistration.Dispose();
            }
        }
    }
}
