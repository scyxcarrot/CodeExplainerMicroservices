using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class ScrewInfoConduitProxy
    {
        private static ScrewInfoConduitProxy _instance;

        public static ScrewInfoConduitProxy GetInstance()
        {
            return _instance ?? (_instance = new ScrewInfoConduitProxy());
        }

        private List<Screw> _screws;
        private List<KeyValuePair<Screw, ScrewInfoDisplayConduit>> _screwDisplayConduits;
        private CMFImplantDirector _director;
        private CMFScrewAnalysis _screwAnalysis;

        public ScrewInfoConduitProxy()
        {
            _screwDisplayConduits = new List<KeyValuePair<Screw, ScrewInfoDisplayConduit>>();
        }

        public void SetUp(List<Screw> screws, CMFImplantDirector director, bool isImplantScrew)
        {
            _screws = screws;
            _screwAnalysis = new CMFScrewAnalysis(director);
            _director = director;
            InvalidateConduits(isImplantScrew);
        }

        public void InvalidateConduits(bool isImplantScrew)
        {
            _screwDisplayConduits = new List<KeyValuePair<Screw, ScrewInfoDisplayConduit>>();
            var objectManager = new CMFObjectManager(_director);
            Vector3d referenceDirection = new Vector3d();

            _screws?.ForEach(x =>
            {
                if (isImplantScrew)
                {
                    var casePreference = objectManager.GetCasePreference(x);
                    var dotPastille = ScrewUtilities.FindDotTheScrewBelongsTo(x, casePreference.ImplantDataModel.DotList);
                    referenceDirection = -RhinoVector3dConverter.ToVector3d(dotPastille.Direction);
                }
                else
                {
                    var constraintMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry;
                    referenceDirection = -ScrewUtilities.GetNormalMeshAtScrewPoint(x, constraintMesh, ScrewAngulationConstants.AverageNormalRadiusGuideFixationScrew);
                }

                var screwAngle = _screwAnalysis.CalculateScrewAngle(x, referenceDirection);

                var cond = new ScrewInfoDisplayConduit();
                cond.OriginalScrewAngle = screwAngle;
                cond.ScrewLength = x.Length;
                cond.Location = x.HeadPoint;

                _screwDisplayConduits.Add(new KeyValuePair<Screw, ScrewInfoDisplayConduit>(x, cond));
            });

            ConduitUtilities.RefeshConduit();
        }

        public void Reset()
        {
            _screwDisplayConduits.ForEach(x => x.Value.Enabled = false);
            _screwDisplayConduits.Clear();
            _screwDisplayConduits = new List<KeyValuePair<Screw, ScrewInfoDisplayConduit>>();

            ConduitUtilities.RefeshConduit();
        }

        public void Show(bool isEnabled)
        {
            _screwDisplayConduits?.ForEach(x => x.Value.Enabled = isEnabled);

            ConduitUtilities.RefeshConduit();
        }

        public bool IsShowing()
        {
            return _screwDisplayConduits.Any() && _screwDisplayConduits[0].Value.Enabled;
        }
    }
}
