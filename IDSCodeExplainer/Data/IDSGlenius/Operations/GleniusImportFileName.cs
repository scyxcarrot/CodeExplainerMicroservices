using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class GleniusImportFileName
    {
        public string FullPath { get; private set; }
        public string FullName { get; private set; }
        public string CaseID { get; private set; }
        public string CaseType { get; private set; }
        public string ScapulaOrHumerus { get; private set; }
        public string Side { get; private set; }
        public string Part { get; private set; }
        public string Keyword { get; private set; }
        public bool IsValid { get; private set; }
        public Nullable<IBB> BuildingBlock { get; private set; }
        public Mesh ImportedMesh { get; private set; }

        public GleniusImportFileName(string fileFullPath)
        {
            IsValid = ExtractFileName(fileFullPath);
        }

        public void SetBuildingBlock(IBB buildingBlock)
        {
            BuildingBlock = buildingBlock;
        }

        public void SetImportedMesh(Mesh mesh)
        {
            ImportedMesh = mesh;
        }

        private bool ExtractFileName(string fileFullPath)
        {
            //Naming convention: [CaseID]_GR_<SR/SL/HR/HL>_{Part name}      
            FullPath = fileFullPath;
            FullName = Path.GetFileNameWithoutExtension(fileFullPath);
            var splits = FullName.Split('_');
            var count = splits.Count();
            if (count >= 4)
            {
                CaseID = splits[0];
                CaseType = splits[1];
                ScapulaOrHumerus = splits[2].Substring(0, 1);
                Side = splits[2].Substring(splits[2].Count() - 1, 1);
                Part = string.Join("_", splits.Skip(3).Take(count - 3));
                Keyword = $"{ScapulaOrHumerus}{Side}_{Part}";
                CapitalizeStrings();
                return true;
            }


            return false;
        }

        private void CapitalizeStrings()
        {
            FullName = FullName.ToUpperInvariant();
            CaseID = CaseID.ToUpperInvariant();
            CaseType = CaseType.ToUpperInvariant();
            ScapulaOrHumerus = ScapulaOrHumerus.ToUpperInvariant();
            Side = Side.ToUpperInvariant();
            Part = Part.ToUpperInvariant();
        }
    }
}