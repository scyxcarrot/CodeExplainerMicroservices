using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Commands;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("3C5D0235-BB0E-4782-8B20-65E3D935818D")]
    [CommandStyle(Style.ScriptRunner)]
    public class CmfTestShowOverlapTriangle : CMFCreateGuideSupport
    {
        public CmfTestShowOverlapTriangle()
        {
            TheCommand = this;
            VisualizationComponent = null;
        }
        
        public static CmfTestShowOverlapTriangle TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestShowOverlapTriangle";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var selectedMesh = SelectedMesh(doc);

            if (selectedMesh == null)
            {
                return Result.Cancel;
            }
            
            IDSPluginHelper.WriteLine(LogCategory.Default, "Original Result:");
            var results = DisplayFinalResultDiagnostics(selectedMesh);
            results = SetDummyResult(results);
            IDSPluginHelper.WriteLine(LogCategory.Default, "Edited Result:");
            DisplayFinalResultDiagnostics(results);

            if (!ValidateQualityOfSupport(director, results, selectedMesh, selectedMesh))
            {
                return Result.Failure;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, "Replaced the support (demo, not really replace)");
            return Result.Success;
        }

        private Mesh SelectedMesh(RhinoDoc doc)
        {
            LockUnlockMeshObjects(doc, false);

            var selectedPart = new GetObject();
            selectedPart.SetCommandPrompt("Select a mesh for to show test");
            selectedPart.EnablePreSelect(false, false);
            selectedPart.EnablePostSelect(true);
            selectedPart.AcceptNothing(true);
            selectedPart.EnableTransparentCommands(false);

            var res = selectedPart.Get();
            if (res != GetResult.Object)
            {
                return null;
            }

            var selectedMesh = selectedPart.Object(0).Object().Geometry.Duplicate() as Mesh;
            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            LockUnlockMeshObjects(doc, true);

            return selectedMesh;
        }

        private void LockUnlockMeshObjects(RhinoDoc doc, bool locked)
        {
            foreach (RhinoObject obj in doc.Objects)
            {
                if (!(obj.Geometry is Mesh))
                {
                    continue;
                }

                if (locked)
                {
                    doc.Objects.Lock(obj.Id, true);
                }
                else
                {
                    doc.Objects.Unlock(obj.Id, true);
                }
            }
        }

        private MeshDiagnostics.MeshDiagnosticsResult SetDummyResult(MeshDiagnostics.MeshDiagnosticsResult originalResult)
        {
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Change the parameter values and press <Enter> to confirmed and <Esc> for abort.");
            getOption.AcceptNothing(true);
            getOption.EnableTransparentCommands(false);

            var numberOfInvertedNormal = (int)originalResult.NumberOfInvertedNormal;
            var numberOfBadEdges = (int)originalResult.NumberOfBadEdges;
            var numberOfBadContours = (int)originalResult.NumberOfBadContours;
            var numberOfNearBadEdges = (int)originalResult.NumberOfNearBadEdges;
            var numberOfHoles = (int)originalResult.NumberOfHoles;
            var numberOfShells = (int)originalResult.NumberOfShells;
            var numberOfOverlappingTriangles = (int)originalResult.NumberOfOverlappingTriangles;
            var numberOfIntersectingTriangles = (int)originalResult.NumberOfIntersectingTriangles;

            while (true)
            {
                getOption.ClearCommandOptions();

                var optionNumberOfInvertedNormal = new OptionInteger(numberOfInvertedNormal, 0, 10);
                var numberOfInvertedNormalIndex = getOption.AddOptionInteger("NumberOfInvertedNormal", ref optionNumberOfInvertedNormal, $"Minimum: 0, Maximum: 10");

                var optionNumberOfBadEdges = new OptionInteger(numberOfBadEdges, 0, 10);
                var numberOfBadEdgesIndex = getOption.AddOptionInteger("NumberOfBadEdges", ref optionNumberOfBadEdges, $"Minimum: 0, Maximum: 10");
                
                var optionNumberOfBadContours = new OptionInteger(numberOfBadContours, 0, 10);
                var numberOfBadContoursIndex = getOption.AddOptionInteger("NumberOfBadContours", ref optionNumberOfBadContours, $"Minimum: 0, Maximum: 10");

                var optionNumberOfNearBadEdges = new OptionInteger(numberOfNearBadEdges, 0, 10);
                var numberOfNearBadEdgesIndex = getOption.AddOptionInteger("NumberOfNearBadEdges", ref optionNumberOfNearBadEdges, $"Minimum: 0, Maximum: 10");

                var optionNumberOfHoles = new OptionInteger(numberOfHoles, 0, 10);
                var numberOfHolesIndex = getOption.AddOptionInteger("NumberOfHoles", ref optionNumberOfHoles, $"Minimum: 0, Maximum: 10");

                var optionNumberOfShells = new OptionInteger(numberOfShells, 0, 10);
                var numberOfShellsIndex = getOption.AddOptionInteger("NumberOfShells", ref optionNumberOfShells, $"Minimum: 0, Maximum: 10");

                var optionNumberOfOverlappingTriangles = new OptionInteger(numberOfOverlappingTriangles, 0, 10);
                var numberOfOverlappingTrianglesIndex = getOption.AddOptionInteger("NumberOfOverlappingTriangles", ref optionNumberOfOverlappingTriangles, $"Minimum: 0, Maximum: 10");

                var optionNumberOfIntersectingTriangles = new OptionInteger(numberOfIntersectingTriangles, 0, 10);
                var numberOfIntersectingTrianglesIndex = getOption.AddOptionInteger("NumberOfIntersectingTriangles", ref optionNumberOfIntersectingTriangles, $"Minimum: 0, Maximum: 10");

                var getResult = getOption.Get();
                if (getResult == GetResult.Option)
                {
                    if (getOption.OptionIndex() == numberOfInvertedNormalIndex)
                    {
                        numberOfInvertedNormal = optionNumberOfInvertedNormal.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfBadEdgesIndex)
                    {
                        numberOfBadEdges = optionNumberOfBadEdges.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfBadContoursIndex)
                    {
                        numberOfBadContours = optionNumberOfBadContours.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfNearBadEdgesIndex)
                    {
                        numberOfNearBadEdges = optionNumberOfNearBadEdges.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfHolesIndex)
                    {
                        numberOfHoles = optionNumberOfHoles.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfShellsIndex)
                    {
                        numberOfShells = optionNumberOfShells.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfOverlappingTrianglesIndex)
                    {
                        numberOfOverlappingTriangles = optionNumberOfOverlappingTriangles.CurrentValue;
                    }

                    else if (getOption.OptionIndex() == numberOfIntersectingTrianglesIndex)
                    {
                        numberOfIntersectingTriangles = optionNumberOfIntersectingTriangles.CurrentValue;
                    }

                    continue;
                }

                if (getResult == GetResult.Nothing)
                {
                    break;
                }

                if (getResult == GetResult.Cancel || getResult == GetResult.NoResult)
                {
                    return originalResult;
                }
            }

            var newResults = new MeshDiagnostics.MeshDiagnosticsResult();
            newResults.NumberOfInvertedNormal = numberOfInvertedNormal;
            newResults.NumberOfBadEdges = numberOfBadEdges;
            newResults.NumberOfBadContours = numberOfBadContours;
            newResults.NumberOfNearBadEdges = numberOfNearBadEdges;
            newResults.NumberOfHoles = numberOfHoles;
            newResults.NumberOfShells = numberOfShells;
            newResults.NumberOfOverlappingTriangles = numberOfOverlappingTriangles;
            newResults.NumberOfIntersectingTriangles = numberOfIntersectingTriangles;
            return newResults;
        }
    }
#endif
}