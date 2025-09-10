using IDS.Amace.Constants;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Xml;


namespace IDS.Amace.Operations
{
    // CupExporter provides functionality for exporting a parameter file for the cup
    public class CupExporter
    {
        // Export a csv file for booklet
        public static bool ExportBookletCsv(string saveFolder, string prefix, Cup cup)
        {
            try
            {
                // make the filepath
                string filePath = $"{saveFolder}\\{prefix}_Reporting_Cup.csv";

                // Write to file
                File.WriteAllText(filePath, "sep=,\n");
                File.AppendAllText(filePath, "ReamerDia,CupOpeningDia,LinerDia,AV1,INCL1\n");
                File.AppendAllText(filePath, string.Format(CultureInfo.InvariantCulture, "{0:F0},{1:F0},{2:F0},{3:F0},{4:F0}", cup.outerReamingDiameter, cup.innerCupDiameter, cup.linerDiameterMax, cup.anteversion, cup.inclination));
            }
            catch
            {
                return false;
            }

            // Success
            return true;
        }

        // Export an xml file with the acetabular plane
        public static void ExportAcetabularPlane(string saveFolder, ImplantDirector director)
        {
            ExportAcetabularPlane(director.cup.cupRimPlane, saveFolder, director.Inspector.CaseId,
                director.draft, director.version);
        }

        public static void ExportAcetabularPlane(Plane acetabularPlane, string saveFolder, string caseId, int version, int draft)
        {
            ExportPlane(acetabularPlane, "AcetabularPlane", saveFolder, caseId, version, draft, AcetabularPlane.Size);

        }

        // Export an xml file with the acetabular plane
        public static void ExportPlane(Plane plane, string planeName, string saveFolder, string caseId, int version, int draft, double planeSize)
        {
            var fullPlaneName = $"{caseId}_{planeName}_v{version:D}_draft{draft:D}";
            var xmlPath = Path.Combine(saveFolder, $"{fullPlaneName}.xml");

            // Write header
            var xmlDoc = new XmlDocument();
            var nodeDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty);
            xmlDoc.AppendChild(nodeDeclaration);

            // Open entities tag
            var nodeEntities = xmlDoc.CreateElement("Entities");
            var attrMaterialise = xmlDoc.CreateAttribute("xmlns", "mat", "http://www.w3.org/2000/xmlns/");
            attrMaterialise.Value = "urn:materialise";
            nodeEntities.Attributes.Append(attrMaterialise);
            xmlDoc.AppendChild(nodeEntities);

            // Create plane node
            var nodePlane = xmlDoc.CreateElement("Plane");

            // Plane Name
            var nodeName = xmlDoc.CreateElement("Name");
            nodeName.InnerText = fullPlaneName;
            nodePlane.AppendChild(nodeName);

            // Plane origin
            var nodeOrigin = xmlDoc.CreateElement("Origin");
            nodeOrigin.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", plane.Origin.X, plane.Origin.Y, plane.Origin.Z);
            nodePlane.AppendChild(nodeOrigin);

            // Plane Normal
            var nodeNormal = xmlDoc.CreateElement("Normal");
            nodeNormal.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
            nodePlane.AppendChild(nodeNormal);

            // Plane X-axis
            var nodeXaxis = xmlDoc.CreateElement("X-axis");
            nodeXaxis.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", plane.XAxis.X * planeSize, plane.XAxis.Y * planeSize, plane.XAxis.Z * planeSize);
            nodePlane.AppendChild(nodeXaxis);

            // Plane Y-axis
            var nodeYaxis = xmlDoc.CreateElement("Y-axis");
            nodeYaxis.InnerText = string.Format(CultureInfo.InvariantCulture, "{0:F8} {1:F8} {2:F8}", plane.YAxis.X * planeSize, plane.YAxis.Y * planeSize, plane.YAxis.Z * planeSize);
            nodePlane.AppendChild(nodeYaxis);

            // Append plane
            nodeEntities.AppendChild(nodePlane);

            // Save doc
            xmlDoc.Save(xmlPath);
        }

        // Export the figure of the cup position as shown in the QC doc
        public static void ExportCupPositionImage(ImplantDirector director, string filename, bool showOverlay)
        {
            var objectManager = new AmaceObjectManager(director);
            var imgPos = ScreenshotsCup.GenerateCupImage(director.Document, 1000, 1000, CupImageType.Position, showOverlay, objectManager.HasBuildingBlock(IBB.PlateSmoothHoles));
            imgPos.Save(filename);
            imgPos.Dispose();
        }

        private static void ExportCoordinateSphere(Point3d point, string filename)
        {
            var sphere = new Sphere(point, 5);
            var mesh = Mesh.CreateFromBrep(sphere.ToBrep())[0];
            StlUtilities.RhinoMesh2StlBinary(mesh, filename);
        }

        private static void ExportLineCylinder(Line line, double radius, string filename)
        {
            var plane = new Plane(line.From, new Vector3d(line.To - line.From));
            var circle = new Circle(plane, radius);
            var cylinder = new Cylinder(circle, line.Length);

            var mesh = Mesh.CreateFromBrep(cylinder.ToBrep(true, true))[0];
            StlUtilities.RhinoMesh2StlBinary(mesh, filename);
        }

        public static void ExportCupPositionParts(string exportFolder, ImplantDirector director)
        {
            var pcs = director.Inspector.AxialPlane;
            var offsetSphere = pcs.XAxis * 200;
            var offsetCylinder = pcs.XAxis * 100;
            var offsetXform = new Transform(1)
            {
                M03 = offsetCylinder.X,
                M13 = offsetCylinder.Y,
                M23 = offsetCylinder.Z
            };
            const double radiusSphere = 5;
            const double radiusCylinder = 0.5;
            var objectManager = new AmaceObjectManager(director);

            // Get relevant meshes
            var def = (Mesh)objectManager.GetBuildingBlock(IBB.DefectPelvis).Geometry;
            Mesh clat = null;
            if (objectManager.HasBuildingBlock(IBB.ContralateralPelvis))
            {
                clat = (Mesh)objectManager.GetBuildingBlock(IBB.ContralateralPelvis).Geometry;
            }

            Mesh sacrum = null;
            if (objectManager.HasBuildingBlock(IBB.Sacrum))
            {
                sacrum = (Mesh)objectManager.GetBuildingBlock(IBB.Sacrum).Geometry;
            }

            // Get bounds, based on Cup Conduit
            var conduit = new CupPositionConduit(director.cup, director.CenterOfRotationContralateralFemur,
                director.CenterOfRotationContralateralFemurMirrored, Color.Black, def, clat, sacrum, true, true, true);
            var bbox = conduit.Bounds;

            // Cup sphere
            ExportCoordinateSphere(director.cup.centerOfRotation + offsetSphere,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORcup_v{director.version:D}_draft{director.draft:D}.stl");
            // Cup COR lines
            var coRcupVer = Drawing2D.CreateVerticalLine(bbox, director.cup.centerOfRotation, pcs);
            var pcSoriVer = Drawing2D.CreateVerticalLine(bbox, pcs.Origin, pcs);
            var coRcupHor = Drawing2D.CreateHorizontalLine(bbox, director.cup.centerOfRotation, pcs);
            coRcupVer.Transform(offsetXform);
            pcSoriVer.Transform(offsetXform);
            coRcupHor.Transform(offsetXform);
            // Export lines
            ExportLineCylinder(coRcupVer, radiusCylinder,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORcupVer_v{director.version:D}_draft{director.draft:D}.stl");
            ExportLineCylinder(pcSoriVer, radiusCylinder,
                $"{exportFolder}\\{director.Inspector.CaseId}_PCSoriVer_v{director.version:D}_draft{director.draft:D}.stl");
            ExportLineCylinder(coRcupHor, radiusCylinder,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORcupHor_v{director.version:D}_draft{director.draft:D}.stl");

            if (director.CenterOfRotationContralateralFemur.IsValid)
            {
                // Clat sphere
                ExportCoordinateSphere(director.CenterOfRotationContralateralFemur + offsetSphere,
                    $"{exportFolder}\\{director.Inspector.CaseId}_CORclat_v{director.version:D}_draft{director.draft:D}.stl");
                // Clat lines
                var coRclatVer = Drawing2D.CreateVerticalLine(bbox, director.CenterOfRotationContralateralFemur, pcs);
                var coRclatHor = Drawing2D.CreateHorizontalLine(bbox, director.CenterOfRotationContralateralFemur, pcs, director.Inspector.DefectSide == "right" ? LineType.Right : LineType.Left);
                coRclatVer.Transform(offsetXform);
                coRclatHor.Transform(offsetXform);
                // Export lines
                ExportLineCylinder(coRclatVer, radiusCylinder,
                    $"{exportFolder}\\{director.Inspector.CaseId}_CORclatVer_v{director.version:D}_draft{director.draft:D}.stl");
                ExportLineCylinder(coRclatHor, radiusCylinder,
                    $"{exportFolder}\\{director.Inspector.CaseId}_CORclatHor_v{director.version:D}_draft{director.draft:D}.stl");
            }

            if (!director.Inspector.DefectFemurCenterOfRotation.IsValid)
            {
                return;
            }

            // Defect sphere
            ExportCoordinateSphere(director.Inspector.DefectFemurCenterOfRotation + offsetSphere,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORdefect_v{director.version:D}_draft{director.draft:D}.stl");
            // Defect lines
            var coRdefVer = Drawing2D.CreateVerticalLine(bbox, director.Inspector.DefectFemurCenterOfRotation, pcs, LineType.Top);
            var coRdefHor = Drawing2D.CreateHorizontalLine(bbox, director.Inspector.DefectFemurCenterOfRotation, pcs, director.Inspector.DefectSide == "left" ? LineType.Right : LineType.Left);
            coRdefVer.Transform(offsetXform);
            coRdefHor.Transform(offsetXform);
            // Export lines
            ExportLineCylinder(coRdefVer, radiusCylinder,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORdefVer_v{director.version:D}_draft{director.draft:D}.stl");
            ExportLineCylinder(coRdefHor, radiusCylinder,
                $"{exportFolder}\\{director.Inspector.CaseId}_CORdefHor_v{director.version:D}_draft{director.draft:D}.stl");
        }
    }
}