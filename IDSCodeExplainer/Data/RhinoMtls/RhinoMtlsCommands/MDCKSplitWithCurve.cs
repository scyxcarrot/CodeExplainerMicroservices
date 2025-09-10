using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using MDCK = Materialise.SDK.MDCK;
using MDCKVertex = Materialise.SDK.MDCK.Model.Objects.Vertex;
using WPoint3d = System.Windows.Media.Media3D.Point3D;

namespace RhinoMatSDKOperations.Curves
{
    /**
     * Operator that splits a mesh with a curve. Splitting is done
     * by attracting the mesh to the curve, creating new triangles,
     * and subsequently separating the mesh along the created border
     * (sequence of triangle edges).
     */

    public class MDCKSplitWithCurve
    {
        /**
         * @see     MDCKSplitWithCurve
         * @param sortByArea    Determines whether the returned list
         *                      of meshes is sorted by area from large
         *                      to small.
         */

        public static bool OperatorSplitWithCurve(Mesh inmesh, Curve splittingCurve, out List<Mesh> parts)
        {
            return OperatorSplitWithCurve(inmesh, new List<Curve> { splittingCurve }, out parts);
        }

        public static bool OperatorSplitWithCurve(Mesh inmesh, IEnumerable<Curve> splittingCurves, out List<Mesh> parts)
        {
            // MatSDK does not support quads
            if (inmesh.Faces.QuadCount > 0)
                inmesh.Faces.ConvertQuadsToTriangles();
            parts = null;

            // create new MDCK model and add our mesh to it
            MDCK.Model.Objects.Model mdckInmodel;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(inmesh, out mdckInmodel);
            if (!res)
            {
                mdckInmodel.Dispose();
                mdckInmodel = null;
                return false;
            }

            // Add a curveset to contain the curves
            MDCK.Model.Objects.CurveSet curveSet = null;
            using (var setadd = new MDCK.Operators.ModelAddCurveSet())
            {
                // Create new CurveSet in model
                setadd.Model = mdckInmodel;
                try
                {
                    setadd.Operate();
                    curveSet = setadd.NewCurveSet;
                }
                catch (MDCK.Operators.ModelAddCurveSet.Exception)
                {
                    mdckInmodel.Dispose();
                    mdckInmodel = null;
                    return false;
                }
            }

            var attachedCurves = new List<MDCK.Model.Objects.Curve>();
            // Attrach the Rhino Curve to Rhino Mesh
            // TODO: just sample curve without attracting, and let MDCK do the attract
            foreach (var curve in splittingCurves)
            {
                var pulledCurve = curve.PullToMesh(inmesh, 0.01);
                if (pulledCurve == null)
                {
                    mdckInmodel.Dispose();
                    mdckInmodel = null;
                    return false;
                }

                // Add the curve as MDCK Curve to the model
                using (var curveadd = new MDCK.Operators.CurveSetAddCurve())
                using (var attacher = new MDCK.Operators.CurveAttach())
                {
                    // Add Curve to CurveSet
                    curveadd.CurveSet = curveSet;
                    bool isclosed = pulledCurve.IsClosed;
                    curveadd.IsClosed = isclosed;
                    try
                    {
                        curveadd.Operate();
                    }
                    catch (MDCK.Operators.CurveSetAddCurve.Exception)
                    {
                        mdckInmodel.Dispose();
                        mdckInmodel = null;
                        return false;
                    }

                    // Sample original polyline curve and add it as an MDCK Curve
                    for (int i = 0; i < pulledCurve.SpanCount; i++)
                    {
                        Interval ival = pulledCurve.SpanDomain(i);
                        double spanA = ival.T0;
                        Point3d cpt = pulledCurve.PointAt(spanA);

                        // Add vertex to model
                        MDCKVertex vert;
                        //using (var vop = new MDCK.Operators.ModelAddVertex())
                        //{
                        var vop = new MDCK.Operators.ModelAddVertex();
                        vop.Model = mdckInmodel;
                        vop.InputPoint = new WPoint3d(cpt.X, cpt.Y, cpt.Z);
                        try
                        {
                            vop.Operate();
                        }
                        catch (MDCK.Operators.ModelAddVertex.Exception)
                        {
                            mdckInmodel.Dispose();
                            mdckInmodel = null;
                            return false;
                        }
                        vert = vop.NewModelVertex;
                        //}

                        // Add MDCK Vertex to curve
                        //using (var bop = new MDCK.Operators.CurveAddVertexBack())
                        //{
                        var bop = new MDCK.Operators.CurveAddVertexBack();
                        bop.Curve = curveadd.NewCurve;
                        bop.ModelVertex = vert;
                        try
                        {
                            bop.Operate();
                        }
                        catch (MDCK.Operators.CurveAddVertexBack.Exception)
                        {
                            mdckInmodel.Dispose();
                            mdckInmodel = null;
                            return false;
                        }
                        //}

                        // Aid the garbage collector
                        //vert = null;
                    }

                    // Pull MDCK curve to model, creating new triangles
                    attacher.Curve = curveadd.NewCurve;
                    attacher.DistanceThreshold = 6.5; // Default of 6.5 mm
                    attacher.RemoveOriginal = true;
                    try
                    {
                        attacher.Operate();
                        attachedCurves.AddRange(attacher.OutputCurves);
                    }
                    catch (MDCK.Operators.CurveAttach.Exception)
                    {
                        mdckInmodel.Dispose();
                        mdckInmodel = null;
                        return false;
                    }
                }
            }

            using (var splitter = new MDCK.Operators.SurfacesSplit())
            {
                // Split the single surface in the MDCK model using the attached curve
                splitter.AddModel(mdckInmodel);
                foreach (var curve in attachedCurves)
                {
                    splitter.AddCurve(curve);
                }
                try
                {
                    splitter.Operate();
                }
                catch (MDCK.Operators.SurfacesSplit.Exception)
                {
                    mdckInmodel.Dispose();
                    mdckInmodel = null;
                    return false;
                }

                // Convert every patch/surface to a Rhino mesh
                parts = new List<Mesh>();
                bool success = true;
                foreach (var patch in splitter.NewSurfaces)
                {
                    // Create new model to feature
                    using (var patchmodel = new MDCK.Model.Objects.Model())
                    using (var mover = new MDCK.Operators.SurfaceMoveToFeature())
                    {
                        mover.SourceSurface = patch;
                        mover.DestinationFeature = patchmodel.MainFeature;
                        try
                        {
                            mover.Operate();
                        }
                        catch (MDCK.Operators.SurfaceMoveToFeature.Exception)
                        {
                            mdckInmodel.Dispose();
                            mdckInmodel = null;
                            return false;
                        }

                        // Convert Model to Rhino mesh
                        Mesh rhmesh;
                        bool rc = MDCKConversion.MDCK2RhinoMeshStl(patchmodel, out rhmesh);
                        if (rc)
                            parts.Add(rhmesh);
                        success &= rc;
                    }
                }
                return success;
            } // End using operators
        }
    }
}