using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class QCImplantContactCreator
    {
        private GleniusObjectManager objectManager;

        public QCImplantContactCreator(GleniusImplantDirector director)
        {
            objectManager = new GleniusObjectManager(director);
        }

        public Mesh CreateImplantContact()
        {
            var scaffoldSupportContact = CreateScaffoldSupportContact();

            if (scaffoldSupportContact != null)
            {
                var screwContact = CreateScrewContactMesh();

                var result = new Mesh();
                result.Append(scaffoldSupportContact);

                if (screwContact != null)
                {
                    result.Append(screwContact);
                }

                return result;
            }

            return null;
        }

        public Mesh CreateScaffoldSupportContact()
        {
            var scaffoldSupport = objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;

            if (scaffoldSupport == null)
            {
                return null;
            }

            var scaffoldSupportContact = CreateWrap(scaffoldSupport.DuplicateMesh());
            return scaffoldSupportContact;
        }

        private Mesh CreateScrewContactMesh()
        {
            var scaffoldSupport = objectManager.GetBuildingBlock(IBB.ScaffoldSupport).Geometry as Mesh;
            var screws = GetScrewMeshes();
            var scapulaDesignReamed = objectManager.GetBuildingBlock(IBB.ScapulaDesignReamed).Geometry as Mesh;

            if (scapulaDesignReamed == null || scaffoldSupport == null || screws == null || !screws.Any())
            {
                return null;
            }

            var contactMeshes = new List<Mesh>();

            foreach (var screw in screws)
            {
                var intersectionCurves = Intersection.MeshMeshAccurate(screw, scapulaDesignReamed, 0.0001);

                if (intersectionCurves != null)
                {
                    var curves = intersectionCurves.Select(x => x.ToNurbsCurve()).ToList();

                    //If there're intersection exists, it should not fail onwards.
                    if (curves.Any())
                    {
                        var theCurve = CurveUtilities.GetClosestCurve(curves, scaffoldSupport);
                        if (theCurve != null)
                        {
                            var scapulaReamedCopy = scapulaDesignReamed.DuplicateMesh();
                            var splittedMesh = MeshOperations
                                .SplitMeshWithCurves(scapulaReamedCopy, new List<Curve>() { theCurve })
                                ?.OrderBy(x => x.CalculateTotalFaceArea());

                            if (splittedMesh != null)
                            {
                                contactMeshes.Add(splittedMesh.FirstOrDefault());
                                continue;
                            }
                        }

                        return null;
                    }
                }
            }

            if (contactMeshes.Any())
            {
                var result = new Mesh();
                foreach (var m in contactMeshes)
                {
                    var wrapped = CreateWrap(m);
                    if (wrapped != null)
                    {
                        result.Append(wrapped);
                    }
                }

                return result;
            }

            return null;
        }

        private Mesh CreateWrap(Mesh mesh)
        {
            Mesh wrapped;
            //var wrapParams = new MDCKShrinkWrapParameters(0.1, 0.0, 0.1, false, true, false, false);
            if (Wrap.PerformWrap(new[] { mesh }, 0.1, 0.0, 0.1, false, true, false, false, out wrapped))
            {
                return wrapped;
            }

            return null;
        }

        private List<Mesh> GetScrewMeshes()
        {
            var screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(x => x as Screw).ToList();

            var screwMeshes = new List<Mesh>();

            if (screws.Any())
            {

                foreach (var screw in screws)
                {
                    var screwMesh = Mesh.CreateFromBrep(screw.Geometry as Brep).ToList();
                    var screwMeshAppended = new Mesh();
                    screwMesh.ForEach(x => screwMeshAppended.Append(x));

                    screwMeshes.Add(screwMeshAppended);
                }

                return screwMeshes;
            }

            return null;
        }
    }
}
