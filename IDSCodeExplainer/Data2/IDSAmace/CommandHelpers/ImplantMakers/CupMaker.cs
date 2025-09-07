using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;

namespace IDS.Amace.Operations
{
    /// <summary>
    /// Functionality to create cups in the document
    /// </summary>
    internal class CupMaker
    {
        /// <summary>
        /// Creates the cup.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        public static bool CreateCup(ImplantDirector director)
        {
            // Init
            var inspector = director.Inspector;
            var doc = director.Document;

            // Determine initialization based on information available in inspector
            var hjc = Point3d.Unset;
            if (director.CenterOfRotationContralateralFemurMirrored.IsValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Cup set at mirrored contralateral ball center.");
                hjc = director.CenterOfRotationContralateralFemurMirrored;
            }
            else if (inspector.DefectFemurCenterOfRotation.IsValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No Contralateral ball available.");
                IDSPluginHelper.WriteLine(LogCategory.Default, "Cup set at defect ball center.");
                hjc = director.CenterOfRotationDefectFemur;
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No Contralateral ball available.");
                IDSPluginHelper.WriteLine(LogCategory.Default, "No Defect ball available.");
                IDSPluginHelper.WriteLine(LogCategory.Default, "Cup set at PCS origin.");
                hjc = director.Pcs.Origin;
            }

            // Compute orientation based on anteversion & inclination
            // use default orientation and parameters
            var cup = new Cup(director, hjc, new CupType(Cup.thicknessDefault, Cup.porousThicknessDefault, CupDesign.v1));
            var objectManager = new AmaceObjectManager(director);
            objectManager.AddNewBuildingBlock(IBB.Cup, cup);

            // Update the cup panel
            var cupPanel = CupPanel.GetPanel();
            if (null != cupPanel)
            {
                cupPanel.UpdatePanelWithCup(director.cup);
            }

            // Set insertion direction (can be changed by user afterwards
            director.InsertionDirection = -cup.orientation;

            // Success
            doc.Views.Redraw();
            return true;
        }
    }
}