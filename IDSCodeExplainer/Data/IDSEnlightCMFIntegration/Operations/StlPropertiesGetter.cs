using IDS.EnlightCMFIntegration.DataModel;
using IDS.EnlightCMFIntegration.Utilities;
using Materialise.MtlsMimicsRW.Core;
using Materialise.MtlsMimicsRW.Mimics;
using System.Collections.Generic;

namespace IDS.EnlightCMFIntegration.Operations
{
    public class StlPropertiesGetter
    {
        private readonly Context _context;
        private readonly MimicsFile _mimicsFile;

        public StlPropertiesGetter(Context context, MimicsFile mimicsFile)
        {
            _context = context;
            _mimicsFile = mimicsFile;
        }

        public bool GetAllStlProperties(out List<StlProperties> stls)
        {
            stls = new List<StlProperties>();

            try
            {
                var numberGetter = new GetNumbersOfObjects
                {
                    MimicsFile = _mimicsFile
                };

                var counts = numberGetter.Operate(_context);

                for (var i = 0; i < counts.StlsNumber; i++)
                {
                    var attrsGetter = new GetStlAttributes
                    {
                        MimicsFile = _mimicsFile,
                        Index = i
                    };

                    var attrs = attrsGetter.Operate(_context);

                    var name = attrs.Label;
                    var internalName = NameUtilities.GetName(_context, _mimicsFile, attrs.Guid);

                    stls.Add(new StlProperties
                    {
                        Name = name,
                        Guid = attrs.Guid,
                        Index = i,
                        TransformationMatrix = Converters.ToTransform(attrs.TransformationToWorld),
                        UiName = name,
                        InternalName = internalName
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
