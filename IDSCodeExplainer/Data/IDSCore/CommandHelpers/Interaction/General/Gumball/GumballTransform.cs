using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI.Gumball;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace IDS.Core.Operations
{
    public abstract class GumballTransform : DisplayConduit, IDisposable
    {
        protected readonly string CommandPrompt;
        protected readonly bool AllowKeyboardEvents;

        protected readonly RhinoDoc ActiveDoc;
        protected readonly EventWaitHandle EventWaitHandle;

        // ObjRef for passing back to child for events
        protected ObjRef SelectedObjectRef;

        private bool _isDisposed = true;
        private GumballDisplayConduit _gumballConduit;

        protected readonly string TransformObjectOption = "TransformObject";
        protected readonly string RelocatedGumballOption = "RelocateGumball";
        protected int GumballModeIndex;

        protected GumballTransform(RhinoDoc doc, bool allowKeyboardEvents, string commandPrompt)
        {
            ActiveDoc = doc;
            AllowKeyboardEvents = allowKeyboardEvents;
            CommandPrompt = commandPrompt;

            // Keyboard event
            if (AllowKeyboardEvents)
            {
                EventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                EnableKeyboardEventsIfAllowed();
            }
        }

        public Transform GumballTransformObject(ObjRef objectReference, GumballObject gumball, GumballAppearanceSettings appearance, ObjRef[] moveAlongObjectReferences = null)
        {
            // Turn Ortho on to make gumball work
            var currentOrthoStatus = Rhino.ApplicationSettings.ModelAidSettings.Ortho;
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = true; // turn it on

            // Define object 'list'
            SelectedObjectRef = objectReference;
            var list = new Rhino.Collections.TransformObjectList();
            list.Add(objectReference);
            if (moveAlongObjectReferences != null)
            {
                foreach (var moveAlong in moveAlongObjectReferences)
                {
                    list.Add(moveAlong);
                }
            }

            // Initialize
            _gumballConduit = new GumballDisplayConduit();
            var finalTransform = Transform.Identity;

            // Transform
            while (true)
            {
                // Update gumball position to current position
                _gumballConduit.SetBaseGumball(gumball, appearance);
                _gumballConduit.Enabled = true;
                ActiveDoc.Views.Redraw();

                // Configure gumball transform
                var gp = new GetGumballTransform(ActiveDoc, _gumballConduit);
                gp.AcceptNothing(true);
                gp.SetCommandPrompt(CommandPrompt);
                gp.AddTransformObjects(list);
                gp.EnableTransparentCommands(false);
                gp.MoveGumball();

                // Transform
                var result = OnTransform(gp);
                if (result == GetResult.Point) // Dragged gumball
                {
                    UpdateGumballFrame(gumball, TransformObjectOption);
                }

                else if (result == GetResult.Cancel) // Pressed 'Esc'
                {
                    // Disable dispaly conduit
                    _gumballConduit.Enabled = false;

                    // Initial (identity) transform is returned, nothing changes
                    Rhino.ApplicationSettings.ModelAidSettings.Ortho = false;

                    // Dispose events
                    Dispose();

                    return Transform.Identity;
                }

                else if (result == GetResult.Nothing) // Pressed 'Enter'
                {
                    // Disable dispaly conduit
                    _gumballConduit.Enabled = false;

                    // If a transformation was applied, move the relevant object
                    finalTransform = ApplyTransformation(finalTransform);

                    break;
                }
            }
            // Dispose events
            Dispose();

            // Refresh
            ActiveDoc.Views.Redraw();
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = currentOrthoStatus; // set original ortho state
            return finalTransform;
        }

        public Transform MoveableGumballTransformObject(ObjRef objectReference, GumballObject gumball, GumballAppearanceSettings appearance)
        {
            // Turn Ortho on to make gumball work
            var currentOrthoStatus = Rhino.ApplicationSettings.ModelAidSettings.Ortho;
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = true; // turn it on

            // Define object 'list'
            SelectedObjectRef = objectReference;

            // Initialize
            _gumballConduit = new GumballDisplayConduit();
            var finalTransform = Transform.Identity;
            var moveOption = TransformObjectOption;

            // Transform
            while (true)
            {
                var list = new Rhino.Collections.TransformObjectList();
                list.Add(SelectedObjectRef);

                // Update gumball position to current position
                _gumballConduit.SetBaseGumball(gumball, appearance);
                _gumballConduit.Enabled = true;

                // Configure gumball transform
                var gp = new GetGumballTransform(ActiveDoc, _gumballConduit);
                gp.ClearCommandOptions();
                gp.AcceptNothing(true);
                gp.AcceptNumber(true, false);
                gp.SetCommandPrompt(CommandPrompt);
                gp.AddTransformObjects(list);
                gp.EnableTransparentCommands(false);

                // Move gumball option
                var optionToggle = new OptionToggle(
                    moveOption == TransformObjectOption,
                    RelocatedGumballOption, TransformObjectOption);
                GumballModeIndex = gp.AddOptionToggle("GumballMode", ref optionToggle);

                if (moveOption == TransformObjectOption)
                {
                    EnableKeyboardEventsIfAllowed();

                    gp.MoveGumball();
                }
                else
                {
                    Dispose();

                    gp.RelocateGumball();
                }

                var result = OnTransform(gp);
                if (result == GetResult.Point) // dragged gumball
                {
                    UpdateGumballFrame(gumball, moveOption);
                }
                
                else if (result == GetResult.Cancel) // pressed 'ESC'
                {
                    // Disable dispaly conduit
                    _gumballConduit.Enabled = false;

                    // Initial (identity) transform is returned, nothing changes
                    Rhino.ApplicationSettings.ModelAidSettings.Ortho = false;

                    // Dispose events
                    Dispose();

                    // Restore selected object to original
                    RestoreObject();
                    return Transform.Identity;
                }
                
                else if (result == GetResult.Nothing) // pressed 'Enter'
                {
                    // Disable display conduit
                    _gumballConduit.Enabled = false;

                    // If a transformation was applied, move the relevant object
                    finalTransform = ApplyTransformation(finalTransform);

                    break;
                }
                
                else if (result == GetResult.Option && gp.OptionIndex() == GumballModeIndex) // toggle the gumball mode
                {
                    moveOption = optionToggle.CurrentValue
                       ? TransformObjectOption 
                       : RelocatedGumballOption;
                }
            }

            // Dispose events
            Dispose();

            // Refresh
            ResetObjectVisibility();

            ActiveDoc.Views.Redraw();
            Rhino.ApplicationSettings.ModelAidSettings.Ortho = currentOrthoStatus; // set original ortho state
            return finalTransform;
        }

        // Get the key state
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        // Detect if a keyboard key is down
        protected static bool IsKeyDown(int key)
        {
            short retVal = GetKeyState(key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                return true;
            }

            //If the low-order bit is 1, the key is toggled.
            return false;
        }

        protected virtual void OnKeyboard(int key)
        {
            
        }

        protected virtual GetResult OnTransform(GetGumballTransform gp)
        {
            // Override it for special treatment
            var getPointResult = gp.Result();
            return getPointResult;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Dispose(true);
            }
        }

        protected virtual void Dispose(bool isDispose)
        {
            if (!isDispose)
            {
                return;
            }

            if (AllowKeyboardEvents)
            {
                RhinoApp.KeyboardEvent -= OnKeyboard;
                EventWaitHandle.Set();
            }

            _isDisposed = true;
        }

        private Transform ApplyTransformation(Transform finalTransform)
        {
            if (_gumballConduit.PreTransform != Transform.Identity)
            {
                finalTransform = _gumballConduit.PreTransform;
                ActiveDoc.Objects.Transform(SelectedObjectRef, finalTransform, true);
            }

            return finalTransform;
        }

        private void UpdateGumballFrame(GumballObject gumball, string moveOption)
        {
            // Update transform
            if (!_gumballConduit.InRelocate && moveOption == TransformObjectOption)
            {
                _gumballConduit.PreTransform = _gumballConduit.TotalTransform;
            }
            // Update location of gumball
            GumballFrame gbframe = _gumballConduit.Gumball.Frame;
            GumballFrame baseFrame = gumball.Frame;
            baseFrame.Plane = gbframe.Plane;
            baseFrame.ScaleGripDistance = gbframe.ScaleGripDistance;
            gumball.Frame = baseFrame;
        }

        private void EnableKeyboardEventsIfAllowed()
        {
            if (_isDisposed && AllowKeyboardEvents)
            {
                RhinoApp.KeyboardEvent += OnKeyboard;
                EventWaitHandle.Set();
                _isDisposed = false;
            }
        }

        protected virtual void RestoreObject()
        {
            ResetObjectVisibility();
        }

        private void ResetObjectVisibility()
        {
            RhinoObjectUtilities.SetRhObjVisibility(ActiveDoc, SelectedObjectRef.Object(), true);

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }
    }
}