using IDS.CMF.DataModel;
using IDS.Core.Drawing;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.AttentionPointer
{
    //For future extension/scaling for customization to make it more for general use
    public class AttentionPointerConduit : PointConduit
    {
        private readonly DisplayMaterial _sphereMaterial;
        private readonly Mesh _sphereMesh;
        public AttentionPointerConduit(Point3d point, string name, System.Drawing.Color color) : base(point, name,
            color)
        {
            _sphereMaterial = new DisplayMaterial
            {
                Transparency = 0.7,
                Diffuse = color,
                Specular = color,
                Emission = color
            };

            var sphere = new Sphere(point, 3);
            _sphereMesh = Mesh.CreateFromSphere(sphere, 100, 100);
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            var silhouettes = Silhouette.Compute(
                _sphereMesh, SilhouetteType.Boundary, e.Viewport.CameraLocation, 0.1, 0.1).ToList();
            silhouettes.ForEach(x =>
            {
                e.Display.DrawCurve(x.Curve, Color.OrangeRed, 1);
            });
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            e.Display.DrawMeshShaded(_sphereMesh, _sphereMaterial);
            e.Display.DrawDot(Point, "May need\ninspection", Color.Transparent, Color.White);
        }
    }

    public class AttentionPointer
    {
        private readonly List<AttentionPointerConduit> _attentionPointerConduits = new List<AttentionPointerConduit>();

        private static AttentionPointer _instance = null;
        public static AttentionPointer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AttentionPointer();
                }

                return _instance;
            }
        }

        private bool _disableOnCommandStart = false;

        public AttentionPointer()
        {
            Command.BeginCommand += (sender, args) =>
            {
                if (_disableOnCommandStart)
                {
                    SetEnabled(false);
                    _disableOnCommandStart = false;
                }
            };
        }

        public void AddAttentionPoint(Point3d location, string content, System.Drawing.Color color)
        {
            _attentionPointerConduits.Add(new AttentionPointerConduit(location, content, color));
        }

        public void ClearAttentionPoint()
        {
            SetEnabled(false);
            _attentionPointerConduits.Clear();
        }

        public void SetEnabled(bool isEnabled)
        {
            _attentionPointerConduits.ForEach(x => x.Enabled = isEnabled);
        }

        public void SetEnabledOnce()
        {
            if (_attentionPointerConduits.Any())
            {
                _attentionPointerConduits.ForEach(x => x.Enabled = true);
                _disableOnCommandStart = true;
            }
        }

        public bool IsEnabled()
        {
            if (_attentionPointerConduits.Any())
            {
                return _attentionPointerConduits.First().Enabled;
            }

            return false;
        }
    }

    public class PastilleAttentionPointer
    {
        private static PastilleAttentionPointer _instance = null;
        public static PastilleAttentionPointer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PastilleAttentionPointer();
                }

                return _instance;
            }
        }

        private bool _userPreference = false;
        private bool _disableOnCommandStart = false;
        private readonly AttentionPointer _attentionPointer;

        public PastilleAttentionPointer()
        {
            _attentionPointer = new AttentionPointer();

            Command.BeginCommand += (sender, args) =>
            {
                if (_disableOnCommandStart)
                {
                    SetEnabled(false);
                    _disableOnCommandStart = false;
                }
            };
        }

        private void SetEnabled(bool isEnabled)
        {
            _attentionPointer.SetEnabled(isEnabled);
        }

        private bool IsEnabled()
        {
            return _attentionPointer.IsEnabled();
        }

        private void SetEnabledOnlyOnce()
        {
            _attentionPointer.SetEnabled(true);
            _disableOnCommandStart = true;
        }

        private void Reset()
        {
            _attentionPointer.ClearAttentionPoint();
        }

        private void InitializePastilleCreationAttention(CMFImplantDirector director)
        {
            director.CasePrefManager.CasePreferences.ForEach(x =>
            {
                x.ImplantDataModel.DotList.ForEach(y =>
                {
                    if (y is DotPastille pastille)
                    {
                        if (pastille.CreationAlgoMethod == DotPastille.CreationAlgoMethods[1])
                        {
                            _attentionPointer.AddAttentionPoint(RhinoPoint3dConverter.ToPoint3d(pastille.Location), "Diff - Pastille Creation Algo", Color.Yellow);
                        }
                    }
                });
            });
        }

        private void InitializePastilleCreationAttention(List<IDot> dotList)
        {
            dotList.ForEach(dot =>
            {
                if (dot is DotPastille pastille)
                {
                    if (pastille.CreationAlgoMethod == DotPastille.CreationAlgoMethods[1])
                    {
                        _attentionPointer.AddAttentionPoint(RhinoPoint3dConverter.ToPoint3d(pastille.Location), "Diff - Pastille Creation Algo", Color.Yellow);
                    }
                }
            });
        }

        public void RefreshHighlightedPastillePosition(CMFImplantDirector director)
        {
            if (IsEnabled())
            {
                Reset();
                InitializePastilleCreationAttention(director);
                SetEnabled(true);
            }
        }

        public void RefreshHighlightedPastillePosition(List<IDot> dotList)
        {
            Reset();
            InitializePastilleCreationAttention(dotList);
            SetEnabled(_userPreference);
        }

        public void HighlightAndRefreshDeformedPastille(CMFImplantDirector director)
        {
            _userPreference = true;
            Reset();
            InitializePastilleCreationAttention(director);
            SetEnabled(true);
        }

        public void HideAndClearDeformedPastille(CMFImplantDirector director)
        {
            _userPreference = false;
            Reset();
        }

        public void ToggleHighlightPastille(CMFImplantDirector director)
        {
            if (_userPreference)
            {
                HideAndClearDeformedPastille(director);
            }
            else
            {
                HighlightAndRefreshDeformedPastille(director);
            }
        }
    }
}