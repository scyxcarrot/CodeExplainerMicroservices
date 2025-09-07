using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Amace.Quality;
using IDS.Amace.Visualization;
using IDS.Common.Visualisation;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Amace.Proxies
{
    public static class TogglePlateAnglesVisualisation
    {
        public static PlateConduit Conduit
        {
            get;
            private set;
        }

        private static PlateAnalyzer plateAnalyzer = null;       

        public static bool Enabled => Conduit != null && Conduit.Enabled;

        public static bool OnTop
        {
            get
            {
                return Conduit.DrawOnTop;
            }
            set
            {
                Conduit.DrawOnTop = value;
            }
        }

        public static void Enable(ImplantDirector director)
        {
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Booleans to check presence lack of specific building blocks
            bool allFlangeSurfacesAvailable = objectManager.HasBuildingBlock(IBB.SolidPlateBottom)
                                                && objectManager.HasBuildingBlock(IBB.SolidPlateTop)
                                                && objectManager.HasBuildingBlock(IBB.SolidPlateSide);
            bool allPlateContoursAvailable = objectManager.HasBuildingBlock(IBB.PlateContourTop)
                                                && objectManager.HasBuildingBlock(IBB.PlateContourBottom);

            // Create flanges if they do not exist
            if (allPlateContoursAvailable && !allFlangeSurfacesAvailable)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Creating flanges.");
                double wrapBumpsInTopWrapResolution = 1.0;
                PlateMaker.CreateFlanges(director, wrapBumpsInTopWrapResolution);
            }

            // Calculate values / lines
            Mesh sideSurface = objectManager.GetBuildingBlock(IBB.SolidPlateSide).Geometry as Mesh;
            Mesh topSurface = objectManager.GetBuildingBlock(IBB.SolidPlateTop).Geometry as Mesh;
            Mesh bottomSurface = objectManager.GetBuildingBlock(IBB.SolidPlateBottom).Geometry as Mesh;
            List<Mesh> exclusioEntities = new List<Mesh>() { director.cup.innerReamingVolumeMesh, director.cup.filledCupMesh };
            double plateThickness = director.PlateThickness;
            if (plateAnalyzer == null || !plateAnalyzer.IsUpToDate(topSurface, bottomSurface, sideSurface))
            {
                // Renew conduit
                Conduit = new PlateConduit(director);
                // Update Plate Analyzer
                plateAnalyzer = new PlateAnalyzer(topSurface, bottomSurface, sideSurface, exclusioEntities, plateThickness);

                List<Tuple<Line, double>> lineAngles = plateAnalyzer.GetSideSurfaceLinesAndAngles();
                // Settings
                Conduit.AngleLines = lineAngles;
            }
            // Show
            Conduit.Enabled = true;

            // Set visibility
            if (objectManager.HasBuildingBlock(IBB.PlateHoles))
            {
                Visibility.PlateDefault(director.Document);
            }
            else // (director.HasBuildingBlock(IBB.SolidPlate))
            {
                Visibility.PlateSurfaces(director.Document);
            }
        }

        public static void Disable(ImplantDirector director)
        {
            // Hide if it exists
            if (Conduit != null)
            {
                Conduit.Enabled = false;
            }

            director.Document.Views.Redraw();
        }
    }
}
