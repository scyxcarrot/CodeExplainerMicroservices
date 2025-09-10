using IDS.CMFImplantCreation.DTO;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Helpers
{
    internal interface IComponentCreator
    {
        Task<IComponentResult> CreateComponentAsync();

        Task<IComponentResult> FinalizeComponentAsync(IComponentResult component);

        IComponentResult FinalizeComponent(IComponentResult component);
    }
}
