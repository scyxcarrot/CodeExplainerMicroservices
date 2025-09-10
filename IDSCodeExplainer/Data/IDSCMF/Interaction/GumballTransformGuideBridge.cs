using IDS.CMF.Constants;
using IDS.CMF.Factory;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.UI.Gumball;
using System;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.CMF.Interaction
{
    public class GumballTransformGuideBridge : GumballTransform
    {
        private readonly CMFImplantDirector _director;

        // Diameter for octagonal bridge
        private double _originalBridgeDiameter;

        public Brep GuideBridgeAfterOperation
        {
            get
            {
                return SelectedObjectRef?.Brep();
            }
        }

        public GumballTransformGuideBridge(CMFImplantDirector director, bool allowKeyboardEvents,
            string commandPrompt = "Drag gumball. Press Enter when done.") :
            base(director.Document, allowKeyboardEvents, commandPrompt)
        {
            _director = director;
        }

        public Transform TransformGuideBridge(Guid brepId, GumballAppearanceSettings appearance, Plane gumballPlane)
        {
            // Create mesh and calculate bounding box
            var brep = (Brep) ActiveDoc.Objects.Find(brepId).Geometry;
            var bbox = BrepUtilities.GetBoundingBoxFromMesh(brep);

            if (AllowKeyboardEvents)
            {
                // Get the original diameter to restore when cancelling
                if (!brep.UserDictionary.TryGetDouble(AttributeKeys.KeyGuideBridgeDiameter, out _originalBridgeDiameter))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Bridge does not have a diameter value!");
                    EventWaitHandle.Set();
                    return Transform.Identity;
                }
            }

            // Gumball
            var gumball = new GumballObject();
            var gumballFrame = gumball.Frame; //copy whatever it is previously there
            gumballFrame.Plane = gumballPlane;

            gumball.SetFromBoundingBox(bbox);
            gumball.Frame = gumballFrame;

            ActiveDoc.Views.Redraw();
            // Get reference
            SelectedObjectRef = new ObjRef(brepId);

            // Transform
            return MoveableGumballTransformObject(SelectedObjectRef, gumball, appearance);
        }

        protected override void OnKeyboard(int key)
        {
            // Only execute if key is down (avoid triggering on key release)
            if (!IsKeyDown(key))
            {
                return;
            }

            switch (key)
            {
                case (187): //+
                case (107): //+ numpad
                    // EventWaitHandle to avoid method being called more than once in a row
                    EventWaitHandle.WaitOne();
                    Rhino.RhinoApp.RunScript("1", false);
                    break;

                case (189): //-
                case (109): //- numpad
                    // EventWaitHandle to avoid method being called more than once in a row
                    EventWaitHandle.WaitOne();
                    Rhino.RhinoApp.RunScript("-1", false);
                    break;
            }

            ConduitUtilities.RefeshConduit();
        }

        protected override GetResult OnTransform(GetGumballTransform gp)
        {
            // Transform
            var getPointResult = gp.Result();
             
            if (getPointResult == GetResult.Number)
            {
                ResizeBridge((int)gp.Number() == 1);
            }

            return getPointResult;
        }

        private bool ResizeBridge(bool positive)
        {
            try
            {
                EventWaitHandle.Reset();
                var bridge = SelectedObjectRef.Brep();
                if (!bridge.UserDictionary.TryGetDouble(AttributeKeys.KeyGuideBridgeDiameter, out var diameter))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Bridge does not have a diameter value!");
                    EventWaitHandle.Set();
                    throw new Exception();
                }

                RhinoObjectUtilities.SetRhObjVisibility(ActiveDoc, SelectedObjectRef.Object(), true);

                // Cast to decimal to avoid floating points
                var newDiameter = decimal.ToDouble(positive ? (decimal)diameter + 0.2M : (decimal)diameter - 0.2M);
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{(positive ? "Increasing" : "Decreasing")} diameter to {newDiameter}");
                var newBridge = ChangeBridgeDiameter(bridge, newDiameter);

                if (newBridge == null)
                {
                    EventWaitHandle.Set();
                    return true;
                }

                ActiveDoc.Objects.Replace(SelectedObjectRef, newBridge);

                EventWaitHandle.Set();

                return true;
            }
            catch (Exception e)
            {
                EventWaitHandle.Set();
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Exception was caught: {e}");
                return false;
            }
        }

        protected override void RestoreObject()
        {
            base.RestoreObject();

            if (!AllowKeyboardEvents)
            {
                return;
            }

            var bridge = SelectedObjectRef.Brep();
            var newBridge = ChangeBridgeDiameter(bridge, _originalBridgeDiameter);
            ActiveDoc.Objects.Replace(SelectedObjectRef, newBridge);
        }

        private Brep ChangeBridgeDiameter(Brep bridge, double diameter)
        {
            // Cast to decimal to avoid floating points
            var minimumDiameter = CMFPreferences.GetGuideBridgeParameters().MinimumDiameter;
            if (diameter < minimumDiameter)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"New diameter ({diameter}) is lower than the allowed minimum diameter ({minimumDiameter})!");
                return null;
            }

            var objectManager = new CMFObjectManager(_director);
            objectManager.GetBuildingBlockCoordinateSystem(SelectedObjectRef.ObjectId, out var coordinate);
            var newCoordinate = coordinate;
            // get the BridgeGenio
            bridge.UserDictionary.TryGetBool(AttributeKeys.KeyGuideBridgeGenio, out var bridgeGenio);

            var startEndPoints = GuideBridgeUtilities.GetStartEndPoints(bridge);
            var guideBridgeBrepFactory = new GuideBridgeBrepFactory(GuideBridgeType.OctagonalBridge, bridgeGenio);

            var newBridge = guideBridgeBrepFactory.CreateGuideBridgeWithRatio(startEndPoints.First(), startEndPoints.Last(), newCoordinate.ZAxis,
                diameter: diameter);

            newBridge.UserDictionary.AddContentsFrom(bridge.UserDictionary);
            newBridge.UserDictionary.Set(AttributeKeys.KeyGuideBridgeDiameter, diameter);

            return newBridge;
        }
    }
}
