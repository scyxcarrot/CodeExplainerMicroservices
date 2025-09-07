using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace IDS.CMF.Utilities
{
    public class ImplantSupportGuidingOutlineHelper
    {
        private const string KeyTouchingOriginalPart = "touching_original_part";

        private readonly CMFImplantDirector _director;

        public ImplantSupportGuidingOutlineHelper(CMFImplantDirector director)
        {
            _director = director;
        }

        public Guid AddImplantSupportGuidingOutlineBuildingBlocks(Curve outline, RhinoObject touchingOriginalPart)
        {
            var objectManager = new CMFObjectManager(_director);
            var guid = objectManager.AddNewBuildingBlock(IBB.ImplantSupportGuidingOutline, outline);
            var outlineRhObject = _director.Document.Objects.Find(guid);
            outlineRhObject.Attributes.UserDictionary.Set(KeyTouchingOriginalPart, touchingOriginalPart.Id);
            return guid;
        }

        public static bool ExtractTouchingOriginalPartId(RhinoObject supportOutlineRhObject, out Guid guid)
        {
            guid = supportOutlineRhObject.Attributes.UserDictionary.GetGuid(KeyTouchingOriginalPart, Guid.Empty);
            return guid != Guid.Empty;
        }
    }
}
