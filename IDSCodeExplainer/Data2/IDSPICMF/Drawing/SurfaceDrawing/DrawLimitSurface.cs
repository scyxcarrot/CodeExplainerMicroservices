using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Drawing
{
    public class DrawLimitSurface : DrawSurface
    {
        public DrawLimitSurfaceResult DrawSurfaceResult { get; set; }

        private readonly DrawSurfaceDataContext _dataContext;

        public DrawLimitSurface(DrawSurfaceDataContext dataContext, Mesh constraintMesh) :
            base(constraintMesh, dataContext, new DrawLimitSurfaceMode(ref dataContext))
        {
            _dataContext = dataContext;
        }

        protected override void PrepareResult()
        {
            var positiveSurfaces = new List<PatchData>();
            var controlPointsList = new List<Point3d>();
            foreach (var positivePatchData in _dataContext.PositivePatchTubes)
            {
                var controlPoints = positivePatchData.Value.ControlPoints;
                var innerSurface = positivePatchData.Key;
                if (innerSurface != null && innerSurface.Faces.Any())
                {
                    positiveSurfaces.Add(new PatchData(innerSurface)
                    {
                        GuideSurfaceData = positivePatchData.Value
                    });
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Patch failed to be created, please adjust the failed patch design.");
                }
                controlPointsList.AddRange(controlPoints);
            }
            //Add Results
            DrawSurfaceResult = new DrawLimitSurfaceResult();
            DrawSurfaceResult.InnerSurfaces.AddRange(positiveSurfaces);
            DrawSurfaceResult.ControlPoints.AddRange(controlPointsList);
            DrawSurfaceResult.ExtensionLength = _dataContext.ExtensionLength;
        }
    }
}