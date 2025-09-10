using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.Operations
{
    public static class PlaneFromReamingEntity
    {
        //Document needs to be unlocked, else object cant be selected
        public static bool GetPlane(RhinoDoc doc, out Guid chosenEntityId, out Plane thePlane)
        {
            // init
            Brep selEntity;
            thePlane = new Plane();
            chosenEntityId = Guid.Empty;
            int bottomFaceIndex = 1; // index of the face of a reaming entity

            // Get entity
            GetObject selectReamingBlocks = new GetObject();
            selectReamingBlocks.SetCommandPrompt("Select reaming entity to transform.");
            selectReamingBlocks.EnablePreSelect(false, false);
            selectReamingBlocks.EnablePostSelect(true);
            selectReamingBlocks.AcceptNothing(true);
            selectReamingBlocks.EnableTransparentCommands(false);

            // Get user input
            while (true)
            {
                GetResult res = selectReamingBlocks.Get();

                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return false;
                }
                if (res == GetResult.Object)
                {
                    // Get selected objects (if multiple selected, select first)
                    List<RhinoObject> selectedReamingBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();
                    selEntity = selectedReamingBlocks[0].Geometry as Brep;

                    // Retrieve the original plane
                    if (selEntity.Faces.Count > 0)
                    {
                        // Get the entity id
                        chosenEntityId = selectedReamingBlocks[0].Id;

                        // Create the plane
                        Point3d p1 = selEntity.Faces[bottomFaceIndex].PointAt(0.5, 0.5);
                        Point3d p2 = selEntity.Faces[bottomFaceIndex].PointAt(0.5, 1);
                        Point3d p3 = selEntity.Faces[bottomFaceIndex].PointAt(1, 0.5);
                        thePlane = new Plane(p1, p2, p3);

                        // Get projected centroid of reaming block to figure out the plane origin
                        VolumeMassProperties vmp = VolumeMassProperties.Compute(selEntity);
                        thePlane.Origin = thePlane.ClosestPoint(vmp.Centroid);
                    }
                    else
                    {
                        return false;
                    }
                    break;
                }
            }
            return true;
        }
    }
}