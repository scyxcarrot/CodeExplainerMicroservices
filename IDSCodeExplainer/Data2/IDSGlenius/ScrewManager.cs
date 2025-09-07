using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace IDS.Glenius
{
    public class ScrewManager
    {
        private readonly RhinoDoc _document;
        public bool IsScrewInvalidationSubscribed { get; private set; }

        public ScrewManager(RhinoDoc document)
        {
            this._document = document;
            IsScrewInvalidationSubscribed = false;
        }
        
        public IEnumerable<Screw> GetAllScrews()
        {
            var settings = new ObjectEnumeratorSettings();
            settings.NameFilter = BuildingBlocks.Blocks[IBB.Screw].Name;
            settings.HiddenObjects = true;
            var rhobjs = _document.Objects.FindByFilter(settings);
            var screws = new List<Screw>();
            foreach (var rhobj in rhobjs)
            {
                var newscrew = rhobj as Screw;
                if (newscrew != null)
                {
                    screws.Add(newscrew);
                }
            }
            screws.Sort();
            return screws;
        }

        public void DeleteAllScrews()
        {
            var allScrews = GetAllScrews();
            foreach (var thisScrew in allScrews)
            {
                thisScrew.Delete();
            }
        }

        public void HandleIndexAssignment(ref Screw screw)
        {
            var screwIbBs = GetAllScrews().ToList();

            if (screwIbBs.Any())
            {
                var maxScrewIndex = screwIbBs.Select(x => x).Select(x => x.Index).Max();
                screw.Index = maxScrewIndex + 1;
            }
            else
            {
                screw.Index = 1;
            }
        }

        //This should better be a static, since we are dealing with static instance of document.
        public void SubscribeScrewInvalidation()
        {
            if (IsScrewInvalidationSubscribed)
            {
                return;
            }
            RhinoDoc.DeleteRhinoObject += OnInvalidateScrewIndexes;
            RhinoDoc.UndeleteRhinoObject += OnInvalidateScrewIndexes;
            IsScrewInvalidationSubscribed = true;
        }

        //This should better be a static, since we are dealing with static instance of document.
        public void UnSubscribeScrewInvalidation()
        {
            if (!IsScrewInvalidationSubscribed)
            {
                return;
            }

            RhinoDoc.DeleteRhinoObject -= OnInvalidateScrewIndexes;
            RhinoDoc.UndeleteRhinoObject -= OnInvalidateScrewIndexes;
            IsScrewInvalidationSubscribed = false;
        }

        private void OnInvalidateScrewIndexes(object sender, RhinoObjectEventArgs e)
        {
            if (!(e.TheObject is Screw))
            {
                return;
            }

            var screws = GetAllScrews().ToList();

            for (var i = 0; i < screws.Count; i++)
            {
                screws[i].Index = i + 1;
            }
        }

        public void TransformAllScrewsAndAides(GleniusImplantDirector director, Transform transform)
        {
            var allScrews = GetAllScrews().ToList();
            allScrews.ForEach(x => TransformScrewAndAides(x, director, transform));
        }

        public void TransformScrewAndAides(Screw screw, GleniusImplantDirector director, Transform transform)
        {
            //Save original screw information first
            var xformHeadPoint = new Point3d(screw.HeadPoint);
            var xformTipPoint = new Point3d(screw.TipPoint);
            var screwType = screw.ScrewType;
            var index = screw.Index;

            //Transform the screw construction base, head and tip point
            xformHeadPoint.Transform(transform);
            xformTipPoint.Transform(transform);

            //Save the mantle length, because when new screw is created to replace the old screw,
            //Mantle will be recreated and in default length.
            var screwMantleExtensionLength = screw.GetScrewMantle().ExtensionLength;

            //Create new screw base on transformed head and tip points and replace old screw via Set(...)
            var newScrew = new Screw(director, xformHeadPoint, xformTipPoint, screwType, index);
            newScrew.Set(screw.Id, false, false);

            //Set new screw mantle length using previous screw length
            var mantle = newScrew.GetScrewMantle();
            var objManager = new GleniusObjectManager(director);
            mantle.SetLength(screwMantleExtensionLength, objManager);
        }
    }
}
