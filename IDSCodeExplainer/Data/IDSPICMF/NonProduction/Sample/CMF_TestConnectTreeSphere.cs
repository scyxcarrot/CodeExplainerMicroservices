#if (INTERNAL)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.Core.Enumerators;
using IDS.Core.NonProduction;
using IDS.Core.PluginHelper;
using IDS.Core.V2.DataModels;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("924626FA-BE35-43BB-8A56-566B34241F5D")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestConnectTreeSphere : CmfCommandBase
    {
        public CMF_TestConnectTreeSphere()
        {
            Instance = this;
        }

        public static CMF_TestConnectTreeSphere Instance { get; private set; }

        public override string EnglishName => "CMF_TestConnectTreeSphere";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Ask the user to select an object to connect establish the connection
            foreach (var rhinoObj in doc.Objects)
            {
                doc.Objects.Unlock(rhinoObj, true);
            }

            var rhinoObjGuidStr = string.Empty;

            RhinoObject rhinoObject = null;
            if (mode == RunMode.Scripted)
            {
                var guidResult = RhinoGet.GetString("Object GUID", false, ref rhinoObjGuidStr);

                rhinoObject = doc.Objects.FindId(new Guid(rhinoObjGuidStr));
                if (guidResult != Result.Success || string.IsNullOrEmpty(rhinoObjGuidStr) || rhinoObject is null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid GUID given: {rhinoObjGuidStr}");
                    return Result.Failure;
                }
            }
            else
            {
                var selectObject = new GetObject();
                selectObject.SetCommandPrompt("Select object to connect cascading delete");
                selectObject.EnablePreSelect(false, false);
                selectObject.EnablePostSelect(true);
                selectObject.AcceptNothing(false);
                selectObject.EnableTransparentCommands(false);

                var result = selectObject.Get();

                if (result == GetResult.Object)
                {
                    rhinoObject = selectObject.Object(0).Object();
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "No object chosen");
                    return Result.Failure;
                }
            }

            // if exists, then create a sphere
            RhinoGet.GetPoint("Select point to generate sphere", false, 
                out var inputPoint);
            var sphere = new Sphere(inputPoint, 2);
            var sphereGuid = Guid.NewGuid();
            var sphereMesh = Mesh.CreateFromBrep(
                sphere.ToBrep(), MeshingParameters.Default)[0];
            var sphereMeshData = new ObjectValueData(sphereGuid, new List<Guid>() { rhinoObject.Id }, new ObjectValue());
            var success = director.IdsDocument.Create(sphereMeshData);
            if (success)
            {
                InternalUtilities.AddObject(sphereGuid, sphereMesh, "sphereMesh", "TreeSphere");
            }

            var selectedMesh = rhinoObject.Geometry.HasBrepForm ? 
                    Mesh.CreateFromBrep((Brep) rhinoObject.Geometry)[0] : 
                    (Mesh) rhinoObject.Geometry;
            var meshDimensionsResult = MeshDimensions.GetMeshDimensions(selectedMesh);

            var planeOrigin = new Point3d(
                meshDimensionsResult.CenterOfGravity[0], 
                meshDimensionsResult.CenterOfGravity[1],
                meshDimensionsResult.CenterOfGravity[2]);
            var planeNormal = planeOrigin - inputPoint;
            var plane = new Plane(inputPoint, planeNormal);

            var circle = new Circle(plane, 0.5);
            var cylinder = new Cylinder(circle, planeNormal.Length);
            var cylinderGuid = Guid.NewGuid();
            var cylinderMesh = Mesh.CreateFromBrep(
                cylinder.ToBrep(false, false), MeshingParameters.Default)[0];
            var cylinderMeshData = new ObjectValueData(cylinderGuid, new List<Guid>() { rhinoObject.Id, sphereGuid }, new ObjectValue());
            success = director.IdsDocument.Create(cylinderMeshData);
            if (success)
            {
                InternalUtilities.AddObject(cylinderGuid, cylinderMesh, "cylinderMesh", "TreeSphere");
            }

            var cone = new Cone(plane, 4, 2);
            var coneGuid = Guid.NewGuid();
            var coneMesh = Mesh.CreateFromBrep(
                cone.ToBrep(true), MeshingParameters.Default)[0];
            var coneMeshData = new ObjectValueData(coneGuid, new List<Guid>() { rhinoObject.Id, sphereGuid }, new ObjectValue());
            success = director.IdsDocument.Create(coneMeshData);
            if (success)
            {
                InternalUtilities.AddObject(coneGuid, coneMesh, "coneMesh", "TreeSphere");
            }

            return Result.Success;
        }
    }
}

#endif