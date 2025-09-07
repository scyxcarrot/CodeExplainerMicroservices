using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public class BarrelTypeChecker : ImplantScrewQcProxyChecker
    {
        public BarrelTypeChecker() : 
            base(ImplantScrewQcCheck.BarrelType)
        {
        }

        public override string ScrewQcCheckTrackerName => "Registered Barrel Type Check";

        public override IScrewQcResult Check(Screw screw)
        {
            var barrelType = screw.BarrelType;

            var registeredBarrelId = screw.RegisteredBarrelId;
            var objectManager = new CMFObjectManager(screw.Director);
            var registeredBarrel = objectManager
                .GetAllBuildingBlocks(IBB.RegisteredBarrel)
                .FirstOrDefault(rhinoObject => rhinoObject.Id == registeredBarrelId);

            var barrelErrorInGuideCreation = false;
            if (registeredBarrel != null)
            {
                var keyIsGuideCreationErrorPresent =
                    registeredBarrel.Attributes.UserDictionary
                        .TryGetBool(BarrelAttributeKeys.KeyIsGuideCreationError,
                            out var isGuideCreationError);
                if (keyIsGuideCreationErrorPresent)
                {
                    barrelErrorInGuideCreation = isGuideCreationError;
                }
            }

            var content = new BarrelTypeContent()
            {
                BarrelType = barrelType,
                BarrelErrorInGuideCreation = barrelErrorInGuideCreation
            };

            return new BarrelTypeResult(ScrewQcCheckName, content);
        }
    }
}
