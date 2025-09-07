using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.Interface.Tools;
using System.Threading.Tasks;

namespace IDS.CMFImplantCreation.Helpers
{
    public class ImplantFactory
    {
        private readonly IConsole _console;
        private readonly IComponentFactory _componentFactory;
        private readonly IConfiguration _configuration;

        public ImplantFactory(IConsole console) : this(console, new ComponentFactory(), new Configuration())
        {

        }

        internal ImplantFactory(IConsole console, IComponentFactory componentFactory, IConfiguration configuration)
        {
            _console = console;
            _componentFactory = componentFactory;
            _configuration = configuration;
        }

        public IComponentResult CreateImplant(IComponentInfo componentInfo)
        {
            var creator = _componentFactory.CreateComponentCreator(_console, componentInfo, _configuration);
            var component = creator.CreateComponentAsync();
            return creator.FinalizeComponent(component.Result);
        }

        public IComponentResult CreateImplant(IFileIOComponentInfo componentInfo)
        {
            var creator = _componentFactory.CreateComponentCreatorFromFile(_console, componentInfo, _configuration);
            var component = creator.CreateComponentAsync();
            return creator.FinalizeComponent(component.Result);
        }

        public Task<IComponentResult> CreateImplantAsync(IComponentInfo componentInfo)
        {
            var creator = _componentFactory.CreateComponentCreator(_console, componentInfo, _configuration);
            var component = creator.CreateComponentAsync();
            return creator.FinalizeComponentAsync(component.Result);
        }

        public Task<IComponentResult> CreateImplantAsync(IFileIOComponentInfo componentInfo)
        {
            var creator = _componentFactory.CreateComponentCreatorFromFile(_console, componentInfo, _configuration);
            var component = creator.CreateComponentAsync();
            return creator.FinalizeComponentAsync(component.Result);
        }
    }
}
