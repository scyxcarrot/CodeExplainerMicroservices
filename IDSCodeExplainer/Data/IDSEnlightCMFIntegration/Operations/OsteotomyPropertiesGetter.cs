using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Utilities;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class OsteotomyPropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public OsteotomyPropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public bool GetAllOsteotomyProperties(out List<OsteotomyProperties> osteotomies)
        {
            osteotomies = new List<OsteotomyProperties>();

            try
            {
                var numberGetter = new GetNumberOfOsteotomy
                {
                    MimicsFile = _mimicsFile
                };

                var counts = numberGetter.Operate(_context);

                for (var i = 0; i < counts.OsteotomyNumber; i++)
                {
                    var attrsGetter = new GetOsteotomyAttributes
                    {
                        MimicsFile = _mimicsFile,
                        Index = i
                    };

                    var attrs = attrsGetter.Operate(_context);

                    var name = attrs.Label;
                    var internalName = NameUtilities.GetName(_context, _mimicsFile, attrs.Guid);

                    osteotomies.Add(new OsteotomyProperties
                    {
                        Name = name,
                        Guid = attrs.Guid,
                        Index = i,
                        Type = attrs.Type,
                        HandlerIdentifier = (string[])attrs.HandlerIdentifiers.Data,
                        HandlerCoordinates = (double[,])attrs.HandlerCoordinates.Data,
                        Thickness = attrs.Thickness,
                        UiName = name,
                        InternalName = internalName
                        //no TransformationMatrix info
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
