using IDS.CMF.V2.DataModel;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Interface.Tools;
using System;

namespace IDS.CMFImplantCreation.Creators
{
    internal class LandmarkComponentFactory : IComponentFactory
    {
        public IComponentCreator CreateComponentCreator(IConsole console, IComponentInfo componentInfo, IConfiguration configuration)
        {
            if (!(componentInfo is LandmarkComponentInfo info))
            {
                throw new Exception($"Invalid type: {componentInfo.GetType().Name}");
            }

            switch (info.Type)
            {
                case LandmarkType.Circle:
                    return new CircleLandmarkCreator(console, componentInfo, configuration);
                case LandmarkType.Rectangle:
                    return new RectangleLandmarkCreator(console, componentInfo, configuration);
                case LandmarkType.Triangle:
                    return new TriangleLandmarkCreator(console, componentInfo, configuration);
                default:
                    throw new Exception($"Invalid LandmarkType: {info.Type}");
            }
        }

        public IComponentCreator CreateComponentCreatorFromFile(IConsole console, IFileIOComponentInfo fileIOComponentInfo, IConfiguration configuration)
        {
            if (!(fileIOComponentInfo is LandmarkFileIOComponentInfo info))
            {
                throw new Exception($"Invalid type: {fileIOComponentInfo.GetType().Name}");
            }

            switch (info.Type)
            {
                case LandmarkType.Circle:
                    return new CircleLandmarkCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case LandmarkType.Rectangle:
                    return new RectangleLandmarkCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                case LandmarkType.Triangle:
                    return new TriangleLandmarkCreator(console, fileIOComponentInfo.ToComponentInfo(console), configuration);
                default:
                    throw new Exception($"Invalid LandmarkType: {info.Type}");
            }
        }
    }
}
