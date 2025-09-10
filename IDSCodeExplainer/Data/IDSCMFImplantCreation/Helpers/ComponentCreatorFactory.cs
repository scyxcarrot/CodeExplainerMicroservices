using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.Creators;
using IDS.CMFImplantCreation.DTO;
using IDS.Interface.Tools;
using System;

namespace IDS.CMFImplantCreation.Helpers
{
    internal class ComponentFactory : IComponentFactory
    {
        public IComponentCreator CreateComponentCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
        {
            switch (componentInfo.GetType().Name)
            {
                // pastille creation
                case nameof(PastilleComponentInfo):
                    return new PastilleCreator(console, componentInfo, configuration);
                case nameof(PastilleIntersectionCurveComponentInfo):
                    return new PastilleIntersectionCurveCreator(console, componentInfo, configuration);
                case nameof(ExtrusionComponentInfo):
                    return new ExtrusionCreator(console, componentInfo, configuration);
                case nameof(PatchComponentInfo):
                    return new PatchCreator(console, componentInfo, configuration);
                case nameof(StitchMeshComponentInfo):
                    return new StitchMeshCreator(console, componentInfo, configuration);
                case nameof(ScrewStampImprintComponentInfo):
                    return new ScrewStampImprintCreator(console, componentInfo, configuration);
                case nameof(FinalizationComponentInfo):
                    return new FinalizationCreator(console, componentInfo, configuration);
                case nameof(SolidMeshComponentInfo):
                    return new SolidMeshCreator(console, componentInfo, configuration);

                // connection creation
                case nameof(ConnectionComponentInfo):
                    return new ConnectionCreator(console, componentInfo, configuration);
                case nameof(ConnectionIntersectionCurveComponentInfo):
                    return new ConnectionIntersectionCurveCreator(console, componentInfo, configuration);
                case nameof(GenerateConnectionComponentInfo):
                    return new GenerateConnectionCreator(console, componentInfo, configuration);

                // landmark creation
                case nameof(LandmarkComponentInfo):
                    return new LandmarkCreator(console, componentInfo, configuration);

                default:
                    throw new Exception($"Invalid type: {componentInfo.GetType().Name}");
            }
        }

        public IComponentCreator CreateComponentCreatorFromFile(IConsole console, IFileIOComponentInfo fileIOComponentInfo, IConfiguration configuration)
        {
            switch (fileIOComponentInfo.GetType().Name)
            {
                // Pastille creation
                case nameof(PastilleFileIOComponentInfo):
                    return new PastilleCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(PastilleIntersectionCurveFileIOComponentInfo):
                    return new PastilleIntersectionCurveCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(ExtrusionFileIOComponentInfo):
                    return new ExtrusionCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(PatchFileIOComponentInfo):
                    return new PatchCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(StitchMeshFileIOComponentInfo):
                    return new StitchMeshCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(ScrewStampImprintFileIOComponentInfo):
                    return new ScrewStampImprintCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(FinalizationFileIOComponentInfo):
                    return new FinalizationCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(SolidMeshFileIOComponentInfo):
                    return new SolidMeshCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);

                // connection creation
                case nameof(ConnectionFileIOComponentInfo):
                    return new ConnectionCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(ConnectionIntersectionCurveFileIOComponentInfo):
                    return new ConnectionIntersectionCurveCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case nameof(GenerateConnectionFileIOComponentInfo):
                    return new GenerateConnectionCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);

                // landmark creation
                case nameof(LandmarkFileIOComponentInfo):
                    return new LandmarkCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);

                default:
                    throw new Exception($"Invalid type: {fileIOComponentInfo.GetType().Name}");
            }
        }
    }
}
