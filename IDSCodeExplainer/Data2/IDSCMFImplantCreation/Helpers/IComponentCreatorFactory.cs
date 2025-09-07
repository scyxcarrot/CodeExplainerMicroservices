using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.Helpers
{
    internal interface IComponentFactory
    {
        IComponentCreator CreateComponentCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration);

        IComponentCreator CreateComponentCreatorFromFile(IConsole console, IFileIOComponentInfo componentInfo, IConfiguration configuration);
    }
}
