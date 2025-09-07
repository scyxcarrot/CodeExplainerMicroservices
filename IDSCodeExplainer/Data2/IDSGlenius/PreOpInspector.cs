using Rhino;
using Rhino.Collections;
using Rhino.FileIO;

namespace IDS.Glenius
{
    public class PreOpInspector : ArchivableDictionary
    {
        private const string keyCaseId = "CASE_ID";

        private const string keyDefectSide = "defect_side";

        private RhinoDoc Document = null;
        
        public PreOpInspector(RhinoDoc doc)
        {
            this.Document = doc;
            this.Name = "PreOpInspector";
        }
        
        public PreOpInspector(RhinoDoc doc, BinaryArchiveReader archive) : this(doc)
        {
            ArchivableDictionary preop_dict = archive.ReadDictionary();
            AddContentsFrom(preop_dict);
        }

        public string caseId
        {
            get
            {
                return GetString(keyCaseId, "Unset");
            }
            set
            {
                Set(keyCaseId, value);
            }
        }

        public string defectSide
        {
            get
            {
                return GetString(keyDefectSide, null);
            }
            set
            {
                Set(keyDefectSide, value);
            }
        }

        public void WriteToArchive(BinaryArchiveWriter archive)
        {
            archive.WriteDictionary(this);
        }
    }
}