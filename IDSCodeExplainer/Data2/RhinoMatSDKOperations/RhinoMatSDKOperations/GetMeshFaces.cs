using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RhinoMatSDKOperations.Utilities
{
    /// <summary>
    /// Let user indicate faces on a given mesh.
    /// </summary>
    /// <seealso cref="Rhino.Input.Custom.GetObject" />
    public class GetMeshFaces : Rhino.Input.Custom.GetObject
    {
        /// GUIDs of meshes from which faces can be selected
        private HashSet<Guid> m_allowable;

        /**
         * Allow face selection only on meshes that have
         * one of the provided GUIDs.
         */

        public GetMeshFaces(IEnumerable<Guid> allowed_meshes)
        {
            this.m_allowable = new HashSet<Guid>(allowed_meshes);
        }

        /**
         * Allow face selection only on meshes that have
         * one of the provided GUIDs.
         */

        public GetMeshFaces(Guid allowed)
        {
            this.m_allowable = new HashSet<Guid>() { allowed };
        }

        /**
         * Allow face selection only on the given
         * mesh objects.
         */

        public GetMeshFaces(IEnumerable<RhinoObject> allowed_rhobj)
        {
            m_allowable = new HashSet<Guid>();
            foreach (RhinoObject rhobj in allowed_rhobj)
            {
                if (rhobj.Id != Guid.Empty)
                    m_allowable.Add(rhobj.Id);
            }
        }

        /**
         * Allow face selection only on the given
         * mesh objects.
         */

        public GetMeshFaces(RhinoObject rhobj)
        {
            this.m_allowable = new HashSet<Guid>() { rhobj.Id };
        }

        /**
         * The filter function determines which geometry can
         * be selected.
         */

        public override bool CustomGeometryFilter(RhinoObject rhObject, GeometryBase geometry, ComponentIndex componentIndex)
        {
            return (componentIndex.ComponentIndexType == ComponentIndexType.MeshFace) && this.m_allowable.Contains(rhObject.Id);
        }
    }
}