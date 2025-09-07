#if (INTERNAL)

using IDS.Amace;
using IDS.Core.Enumerators;
using IDS.Core.NonProduction;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.Commands.NonProduction
{
    [System.Runtime.InteropServices.Guid("24a1a09b-6fce-4e68-9476-5f6c4c5ca93e")]
    public class AMace_TestCreateGuideHoleBooleanAndSafetyZone : Command
    {
        static AMace_TestCreateGuideHoleBooleanAndSafetyZone _instance;
        public AMace_TestCreateGuideHoleBooleanAndSafetyZone()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MyTest command.</summary>
        public static AMace_TestCreateGuideHoleBooleanAndSafetyZone Instance => _instance;

        public override string EnglishName => "AMace_TestCreateGuideHoleBooleanAndSafetyZone";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (director != null)
            {
                var screwManager = new ScrewManager(doc);
                var screws = screwManager.GetAllScrews().ToList();

                IDSPluginHelper.WriteLine(LogCategory.Default, $"DrillBitRadius: {director.DrillBitRadius}");

                foreach (var s in screws)
                {
                    var aideManager = new ScrewAideManager(s, director.ScrewDatabase);
                    var safetyZone = aideManager.GetGuideHoleSafetyZone(director.DrillBitRadius);

                    var guideCreator = new ScrewGuideCreator();
                    var guideHoleBoolean = guideCreator.GetGuideHoleBoolean(s, director.DrillBitRadius);

                    InternalUtilities.AddObject(guideHoleBoolean, "AMace_Test_GuideHoleBoolean");
                    InternalUtilities.AddObject(safetyZone, "AMace_Test_GuideHoleSafetyZone");
                }

                doc.Views.Redraw();

                return Result.Success;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Could not get director");
            return Result.Failure;
        }
    }
}

#endif