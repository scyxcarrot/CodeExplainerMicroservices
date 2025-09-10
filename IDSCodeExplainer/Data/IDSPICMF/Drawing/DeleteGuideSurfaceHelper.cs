using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace IDS.PICMF.Drawing
{
    public class DeleteGuideSurfaceHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly RhinoDoc _doc;

        private readonly List<PatchData> _deletedPositiveSurfaces = new List<PatchData>();
        private readonly List<PatchData> _deletedNegativeSurfaces = new List<PatchData>();
        private readonly List<PatchData> _deletedLinkSurfaces = new List<PatchData>();
        private readonly List<PatchData> _deletedSolidSurfaces = new List<PatchData>();

        public DeleteGuideResult Result { get; private set; }

        public DeleteGuideSurfaceHelper(RhinoDoc doc, CMFImplantDirector director)
        {
            _director = director;
            _doc = doc;
        }

        public bool Execute(GuidePreferenceDataModel prefData)
        {
            var positiveSurfaces = prefData.PositiveSurfaces;
            var negativeSurfaces = prefData.NegativeSurfaces;
            var linkSurfaces = prefData.LinkSurfaces;
            var solidSurfaces = prefData.SolidSurfaces;

            if (!positiveSurfaces.Any() && !negativeSurfaces.Any() && !linkSurfaces.Any() && !solidSurfaces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "There must be at least a positive/negative/link surface/solid surface!");
                return true;
            }

            var patchSurfaces = positiveSurfaces.Where(s => s.GuideSurfaceData is PatchSurface).ToList();
            var skeletonSurfaces = positiveSurfaces.Where(s => s.GuideSurfaceData is SkeletonSurface).ToList();
            patchSurfaces.AddRange(linkSurfaces.Where(s => s.GuideSurfaceData is PatchSurface));
            skeletonSurfaces.AddRange(linkSurfaces.Where(s => s.GuideSurfaceData is SkeletonSurface));
            var conduit = new GuideSurfacesConduit(patchSurfaces, negativeSurfaces, skeletonSurfaces, solidSurfaces);
            conduit.IsHighlighted = true;
            conduit.Enabled = true;
            _doc.Views.Redraw();

            var surfaces = new List<PatchData>();

            while (true)
            {
                PatchData surface;
                var selected = SelectSurface(positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces, out surface);

                if (!selected) //Escape pressed
                {
                    conduit.Enabled = false;
                    return false;
                }

                if (surface == null) //Enter pressed
                {
                    if (BlockUserFromDeletingSelectedSurfaces(surfaces, positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces))
                    {
                        MessageBox.Show("You have selected all the available positive surface(s) while keeping negative/link surface(s)/solid surface(s). This is not allowed.", "Remove Surface(s)",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        continue;
                    }

                    var res = MessageBox.Show("Are you sure you want to remove selected surface(s)?", "Remove Surface(s)",
                        MessageBoxButton.YesNo);

                    if (res != MessageBoxResult.Yes)
                    {
                        continue;
                    }

                    break;
                }

                if (surfaces.Contains(surface))
                {
                    surfaces.Remove(surface);
                    conduit.PatchSurfaceExclusion.Remove(surface);
                }
                else
                {
                    surfaces.Add(surface);
                    conduit.PatchSurfaceExclusion.Add(surface);
                }
                _doc.Views.Redraw();
            }

            if (!surfaces.Any())
            {
                conduit.Enabled = false;
                IDSPluginHelper.WriteLine(LogCategory.Default, "No surfaces selected, no change is done.");
                return false;
            }

            surfaces.ForEach(x =>
            {
                DeleteSelectedSurfacePatchData(x, ref positiveSurfaces, ref negativeSurfaces, ref linkSurfaces, ref solidSurfaces);
            });

            Result = new DeleteGuideResult(positiveSurfaces, negativeSurfaces, linkSurfaces, solidSurfaces);

            conduit.Enabled = false;

            return true;
        }

        public void RestoreSurfaces(ref GuidePreferenceDataModel prefData)
        {
            prefData.PositiveSurfaces.AddRange(_deletedPositiveSurfaces);
            prefData.NegativeSurfaces.AddRange(_deletedNegativeSurfaces);
            prefData.LinkSurfaces.AddRange(_deletedLinkSurfaces);
            prefData.SolidSurfaces.AddRange(_deletedSolidSurfaces);
        }

        private void DeleteSelectedSurfacePatchData(PatchData surface, ref List<PatchData> PositiveSurfaces,
            ref List<PatchData> NegativeSurfaces, ref List<PatchData> LinkSurfaces, ref List<PatchData> SolidSurfaces)
        {
            if (PositiveSurfaces.Exists(s => s == surface))
            {
                _deletedPositiveSurfaces.Add(surface);
                PositiveSurfaces.Remove(surface);
                return;
            }

            if (NegativeSurfaces.Exists(s => s == surface))
            {
                _deletedNegativeSurfaces.Add(surface);
                NegativeSurfaces.Remove(surface);
                return;
            }

            if (LinkSurfaces.Exists(s => s == surface))
            {
                _deletedLinkSurfaces.Add(surface);
                LinkSurfaces.Remove(surface);
            }

            if (SolidSurfaces.Exists(s => s == surface))
            {
                _deletedSolidSurfaces.Add(surface);
                SolidSurfaces.Remove(surface);
            }
        }

        private bool SelectSurface(List<PatchData> positiveSurfaces, List<PatchData> negativeSurfaces, List<PatchData> linkSurfaces, List<PatchData> solidSurfaces, out PatchData patch)
        {
            patch = null;

            var selectSurface = new GetObject();
            selectSurface.SetCommandPrompt("Select a surface to delete.");
            selectSurface.DisablePreSelect();
            selectSurface.AcceptNothing(true);
            selectSurface.EnableHighlight(false);

            var res = selectSurface.Get();
            if (res == GetResult.Object)
            {
                var surface = (Mesh)selectSurface.Object(0).Object().Geometry;

                var patchData = positiveSurfaces.FirstOrDefault(s => MeshUtilities.IsEqual(s.Patch, surface));
                if (patchData != null)
                {
                    patch = patchData;
                    return true;
                }

                patchData = negativeSurfaces.FirstOrDefault(s => MeshUtilities.IsEqual(s.Patch, surface));
                if (patchData != null)
                {
                    patch = patchData;
                    return true;
                }

                patchData = linkSurfaces.FirstOrDefault(s => MeshUtilities.IsEqual(s.Patch, surface));
                if (patchData != null)
                {
                    patch = patchData;
                    return true;
                }
                
                patchData = solidSurfaces.FirstOrDefault(s => MeshUtilities.IsEqual(s.Patch, surface));
                if (patchData != null)
                {
                    patch = patchData;
                    return true;
                }
            }
            else if (res == GetResult.Nothing)
            {
                return true;
            }

            return false;
        }

        private bool BlockUserFromDeletingSelectedSurfaces(List<PatchData> selectedSurfaces, List<PatchData> positiveSurfaces, List<PatchData> negativeSurfaces, List<PatchData> linkSurfaces, List<PatchData> solidSurfaces)
        {
            var allPositiveSurfacesSelected = positiveSurfaces.All(s => selectedSurfaces.Contains(s));
            var keepingNegativeSurfaces = negativeSurfaces.Any(s => !selectedSurfaces.Contains(s));
            var keepingLinkSurfaces = linkSurfaces.Any(s => !selectedSurfaces.Contains(s));
            var keepingSolidSurfaces = solidSurfaces.Any(s => !selectedSurfaces.Contains(s));
            return allPositiveSurfacesSelected && (keepingNegativeSurfaces || keepingLinkSurfaces || keepingSolidSurfaces);
        }
    }
}
