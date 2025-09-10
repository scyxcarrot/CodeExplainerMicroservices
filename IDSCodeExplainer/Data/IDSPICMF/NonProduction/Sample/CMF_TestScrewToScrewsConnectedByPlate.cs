#if (INTERNAL)
using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.RhinoFree.Utilities;
using IDS.CMF.Utilities;
using IDS.Core.NonProduction;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("03844db4-2731-4ce0-b63b-a2102884b05f")]
    public class CMF_TestScrewToScrewsConnectedByPlate : CmfCommandBase
    {
        static CMF_TestScrewToScrewsConnectedByPlate _instance;
        public CMF_TestScrewToScrewsConnectedByPlate()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateImplantScrewVisualization();
            IsUseBaseCustomUndoRedo = false;
        }

        ///<summary>The only instance of the CMF_TestScrewToScrewsConnectedByPlate command.</summary>
        public static CMF_TestScrewToScrewsConnectedByPlate Instance => _instance;

        public override string EnglishName => "CMF_TestScrewToScrewsConnectedByPlate";

        private static int n = 0;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockScrews(director.Document);

            // Select screw
            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to rotate it's tip.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var objManager = new CMFObjectManager(director);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                // Get selected screw
                var screw = selectScrew.Object(0).Object() as Screw;
                var cp = objManager.GetCasePreference(screw);
                var connectionList = cp.ImplantDataModel.ConnectionList;

                var dot = ImplantCreationUtilities.FindClosestDotPastille(cp.ImplantDataModel.DotList,
                    screw.HeadPoint);

                //Algo Now
                var dotPastilles = new List<DotPastille>();
                foreach (var connection in connectionList)
                {
                    if (connection.A is DotPastille && !dotPastilles.Contains(connection.A))
                    {
                        dotPastilles.Add((DotPastille)connection.A);
                    }

                    if (connection.B is DotPastille && !dotPastilles.Contains(connection.B))
                    {
                        dotPastilles.Add((DotPastille)connection.B);
                    }
                }

                var result = 
                    ImplantCreationUtilitiesRhinoFree.FindNeigbouringDotPastilles(
                        dot, connectionList, ConnectionType.EPlate);

                var sp1 = new Sphere(RhinoPoint3dConverter.ToPoint3d(dot.Location), 1);
                InternalUtilities.AddObject(Brep.CreateFromSphere(sp1), $"Source #{n}");

                result.ForEach(x =>
                {
                    var sp = new Sphere(RhinoPoint3dConverter.ToPoint3d(x.Location), 1);
                    InternalUtilities.AddObject(Brep.CreateFromSphere(sp), $"Target #{n}");
                });

                n++;
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}

#endif