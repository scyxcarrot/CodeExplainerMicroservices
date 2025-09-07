using IDS.CMF.Factory;
using Rhino.Collections;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class PatchData : ISerializable<ArchivableDictionary>
    {
        public PatchData()
        {

        }

        public PatchData(Mesh patch)
        {
            Patch = patch;
        }

        private Mesh _patch;

        public Mesh Patch
        {
            get { return _patch; }
            set
            {
                _patch = value;

                _patch.Compact();
                _patch.FaceNormals.ComputeFaceNormals();
                _patch.UnifyNormals(false);

                if (_patch.GetNakedEdges() == null)
                {
                    //MeshRepair - Partially...
                    _patch.ExtractNonManifoldEdges(true);
                    _patch.Faces.ExtractDuplicateFaces();
                }

                Edges = new List<Polyline>(_patch.GetNakedEdges());
            }
        }
        public List<Polyline> Edges { get; private set; }

        public IGuideSurface GuideSurfaceData { get; set; }

        public string SerializationLabel => "PatchData";
        private readonly string KeyPatch = "Patch";
        private readonly string KeyGuideSurfaceData = "GuideSurfaceData";

        public bool Serialize(ArchivableDictionary serializer)
        {
            serializer.Set(KeyPatch, Patch);
            var surfDataDict = SerializationFactory.CreateSerializedArchive(GuideSurfaceData);
            serializer.Set(KeyGuideSurfaceData, surfDataDict);

            return true;
        }

        public bool DeSerialize(ArchivableDictionary serializer)
        {
            Patch = (Mesh)((GeometryBase)serializer[KeyPatch]);
            GuideSurfaceData = SerializationFactory.DeserializeGuideSurface(serializer.GetDictionary(KeyGuideSurfaceData));

            return true;
        }
    }
}
