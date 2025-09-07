using IDS.CMFImplantCreation.Helpers;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public class FinalizationFileIOComponentInfo : IFileIOComponentInfo
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public bool IsActual { get; set; }

        public bool NeedToFinalize { get; set; }

        public string ClearanceMeshSTLFilePath { get; set; }

        public List<string> SubtractorsSTLFilePaths { get; set; }

        public List<string> ComponentMeshesSTLFilePaths { get; set; }

        public IComponentInfo ToComponentInfo(IConsole console)
        {
            return this.ToDefaultComponentInfo<FinalizationComponentInfo>(console);
        }
    }
}
