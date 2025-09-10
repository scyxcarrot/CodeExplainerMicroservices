using Materialise.SDK.MatSAX;
using Materialise.SDK.MDCK.Model.Objects;
using System;
using System.Collections.Generic;

namespace RhinoMatSDKOperations.CMFProPlan
{
    public class ProPlanObjectReader : ISAXReadHandler
    {
        public List<Model> Models;
        public List<IOsteotomy> OsteotomyList;
        public IOsteotomy CurrentOsteotomy;
        public Model MeshModel;

        public ProPlanObjectReader()
        {
            Models = new List<Model>();
            OsteotomyList = new List<IOsteotomy>();
        }

        public bool HandleTag(string tag, MSAXReaderWrapper reader)
        {
            if (tag == "Stl")
            {
                MeshModel = new Model();
                var stlParser = new StlParser(MeshModel);
                reader.PushTagHandler(stlParser);
                return true;
            }

            if (tag == "ProtectedStl")
            {
                var protectedStl = new StlParser(MeshModel);
                reader.PushTagHandler(protectedStl);
                return true;
            }

            if (tag == "CuttingPathPlanar")
            {
                CurrentOsteotomy = new OsteotomyPlanar();
                var cuttingPathPlanar = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathPlanar);
                return true;
            }

            if (tag == "CuttingPathLeFort1")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathLefort1 = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathLefort1);
                return true;
            }

            if (tag == "CuttingPathLeFort2")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathLefort2 = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathLefort2);
                return true;
            }

            if (tag == "CuttingPathLeFort3")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathLefort3 = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathLefort3);
                return true;
            }

            if (tag == "CuttingPathBSSO")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathBSSO = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathBSSO);
                return true;
            }

            if (tag == "CuttingPathGenioplasty")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathGenioplasty = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathGenioplasty);
                return true;
            }

            if (tag == "CuttingPathCurve")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathCurve = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathCurve);
                return true;
            }

            if (tag == "CuttingPathSurface")
            {
                CurrentOsteotomy = new OsteotomyFreeFormCut();
                var cuttingPathSurface = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathSurface);
                return true;
            }

            if (tag == "CuttingPathVShaped")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathVShaped = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathVShaped);
                return true;
            }

            if (tag == "CuttingPathZShaped")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPathZShaped = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPathZShaped);
                return true;
            }

            if (tag == "CuttingPath")
            {
                CurrentOsteotomy = new OsteotomyCuttingPath();
                var cuttingPath = new CuttingPathSegmentsParser(CurrentOsteotomy);
                reader.PushTagHandler(cuttingPath);
                return true;
            }
            return true;
        }

        public void HandleEndTag(string tag)
        {
            if (tag == "Stl" || tag == "ProtectedStl")
            {
                Models.Add(MeshModel);
            }

            if (tag == "CuttingPathPlanar")
            {
                OsteotomyList.Add(CurrentOsteotomy);
            }

            if (tag == "CuttingPathVShaped" || tag == "CuttingPathZShaped")
            {
                OsteotomyList.Add(CurrentOsteotomy);
            }

            if (tag == "CuttingPathLeFort1" || tag == "CuttingPathLeFort2" || tag == "CuttingPathLeFort3")
            {
                OsteotomyList.Add(CurrentOsteotomy);
            }

            if (tag == "CuttingPathBSSO" || tag == "CuttingPathGenioplasty")
            {
                OsteotomyList.Add(CurrentOsteotomy);
            }

            if (tag == "CuttingPathCurve" || tag == "CuttingPath" || tag == "CuttingPathSurface")
            {
                OsteotomyList.Add(CurrentOsteotomy);
            }

        }

        public void InitAfterLoading()
        {
        }
    }
}
