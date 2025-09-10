using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.Operations
{
    //The command that calls Import method must be decorated with [CommandStyle(Style.ScriptRunner)] attribute
    public class ImporterViaRunScript
    {
        private readonly List<Guid> _importedGuids;
        private readonly List<Brep> _breps;

        public ImporterViaRunScript()
        {
            _importedGuids = new List<Guid>();
            _breps = new List<Brep>();
        }


        public List<Guid> Import(string filePath)
        {
            ClearImports();

            RhinoDoc.AddRhinoObject += RhinoObjectAdded;
            RhinoApp.RunScript($"-_Import \"{filePath}\" _Enter", false);
            RhinoDoc.AddRhinoObject -= RhinoObjectAdded;

            return _importedGuids;
        }

        private void ClearImports()
        {
            _importedGuids.Clear();
            _breps.Clear();
        }

        private void RhinoObjectAdded(object sender, RhinoObjectEventArgs e)
        {
            _importedGuids.Add(e.ObjectId);

            if (e.TheObject.Geometry.HasBrepForm)
            {
                _breps.Add(e.TheObject.Geometry as Brep);
            }
        }

        public List<Brep> ImportStepAsBrep(string filePath)
        {
            ClearImports();

            RhinoDoc.AddRhinoObject += RhinoObjectAdded;
            var imported = RhinoApp.RunScript($"-_Import \"{filePath}\" _Enter", false);
            RhinoDoc.AddRhinoObject -= RhinoObjectAdded;

            if (imported)
            {
                // Remove the objects that were added to the document by the Import command, since we have the breps now.
                RemoveImportedObjectsFromDocument();
            }

            return _breps;
        }

        private void RemoveImportedObjectsFromDocument()
        {
            foreach (var importedGuid in _importedGuids)
            {
                RhinoDoc.ActiveDoc.Objects.Delete(importedGuid, true);
            }
            _importedGuids.Clear();
        }
    }
}