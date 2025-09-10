using IDS.CMF.Constants;
using IDS.CMF.Factory;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Drawing;

namespace IDS.PICMF.Helper
{
    public class GuideBridgeCreatorHelper
    {
        private double minimumDiameter;
        private Mesh constrainMesh;

        public Point3d StartPoint { get; private set; }
        public Point3d EndPoint { get; private set; }
        public Vector3d UpDirection { get; private set; }
        public string BridgeType { get; private set; }
        public double  BridgeDiameter { get; private set; }
        public bool  BridgeGenio { get; private set; }

        public bool DrawBridge(Mesh pickConstrainMesh, double defaultDiameter, double minDiameter, double maxDiameter)
        {
            StartPoint = Point3d.Unset;
            EndPoint = Point3d.Unset;
            UpDirection = Vector3d.Unset;
            BridgeType = GuideBridgeType.OctagonalBridge;
            BridgeDiameter = defaultDiameter;
            BridgeGenio = false;
            minimumDiameter = minDiameter;
            constrainMesh = pickConstrainMesh;

            var getPoints = new GetPoint();
            getPoints.SetCommandPrompt("Select a start point.");
            getPoints.Constrain(constrainMesh, false);
            getPoints.DynamicDraw += OnDynamicDraw;
            getPoints.AcceptNothing(true); // accept ENTER to confirm

            while (true)
            {
                getPoints.ClearCommandOptions();
                var diameterOption = new OptionDouble(BridgeDiameter, minDiameter, maxDiameter);

                var optionToggle = new OptionToggle(BridgeType == GuideBridgeType.OctagonalBridge, 
                    GuideBridgeType.HexagonalBridge, GuideBridgeType.OctagonalBridge);

                var optionGenioToggle = new OptionToggle(BridgeGenio, "No", "Yes");

                var bridgeTypeIndex = getPoints.AddOptionToggle("BridgeType", ref optionToggle);
                
                int bridgeGenioIndex = -1; // initialize an invalid index

                if (BridgeType == GuideBridgeType.OctagonalBridge)
                {
                    getPoints.AddOptionDouble("BridgeDiameter", ref diameterOption, "The total diameter of the bridge.");
                    bridgeGenioIndex = getPoints.AddOptionToggle("BridgeGenio", ref optionGenioToggle);
                }

                var getResult = getPoints.Get();
                if (getResult == GetResult.Cancel)
                {
                    getPoints.DynamicDraw -= OnDynamicDraw;
                    return false;
                }

                if (getResult == GetResult.Point)
                {
                    if (StartPoint == Point3d.Unset)
                    {
                        StartPoint = getPoints.Point();
                        getPoints.SetCommandPrompt("Select an end point.");
                    }
                    else if (EndPoint == Point3d.Unset)
                    {
                        var endPoint = getPoints.Point();
                        if ((endPoint - StartPoint).Length >= minimumDiameter)
                        {
                            EndPoint = getPoints.Point();
                            var guideBridgeBrepFactory = new GuideBridgeBrepFactory();
                            UpDirection = guideBridgeBrepFactory.SetUpBridgeDirection(StartPoint, EndPoint, constrainMesh);
                            break;
                        }
                        getPoints.SetCommandPrompt($"Minimum diameter is {minimumDiameter}. Select another end point.");
                    }
                }

                if (getResult == GetResult.Option)
                {
                    if (getPoints.OptionIndex() == bridgeTypeIndex)
                    {
                        BridgeType = optionToggle.CurrentValue
                            ? GuideBridgeType.OctagonalBridge
                            : GuideBridgeType.HexagonalBridge;
                    }
                    else if (getPoints.OptionIndex() == bridgeGenioIndex)
                    {
                        BridgeGenio = optionGenioToggle.CurrentValue;
                    }
                    else
                    {
                        BridgeDiameter = diameterOption.CurrentValue;
                    }
                }
            }

            getPoints.DynamicDraw -= OnDynamicDraw;

            return true;
        }

   
        private void OnDynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            if (StartPoint == Point3d.Unset)
            {
                e.Display.DrawPoint(e.CurrentPoint, Color.Blue);
            }

            if (StartPoint != Point3d.Unset && EndPoint == Point3d.Unset)
            {
                e.Display.DrawPoint(StartPoint, Color.Blue);
                e.Display.DrawPoint(e.CurrentPoint, Color.Blue);
                if ((e.CurrentPoint - StartPoint).Length >= minimumDiameter)
                {
                    e.Display.DrawLine(StartPoint, e.CurrentPoint, Color.Red);
                }
            }
        }
    }
}
