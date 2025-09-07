using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.DTO
{
    public interface IFileIOComponentInfo
    {
        Guid Id { get; set; }

        string DisplayName { get; set; }

        string ClearanceMeshSTLFilePath { get; set; }

        List<string> SubtractorsSTLFilePaths { get; set; }

        List<string> ComponentMeshesSTLFilePaths { get; set; }

        bool IsActual { get; set; }

        bool NeedToFinalize { get; set; }

        IComponentInfo ToComponentInfo(IConsole console);
    }
}
