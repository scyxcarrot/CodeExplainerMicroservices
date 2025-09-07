#if (INTERNAL)
using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.Core.NonProduction;
using IDS.Core.V2.DataModels;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("924626FA-BE35-43BC-8A56-566A34241F8D")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestCreateTreeSphereWithGuid : CmfCommandBase
    {
        public CMF_TestCreateTreeSphereWithGuid()
        {
            Instance = this;
        }

        public static CMF_TestCreateTreeSphereWithGuid Instance { get; private set; }

        public override string EnglishName => "CMF_TestCreateTreeSphereWithGuid";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // create a dummy root node so that we can delete this sphere node
            var dummyGuid = Guid.NewGuid();
            var dummyValueData = new GuidValueData(dummyGuid, new List<Guid>(), dummyGuid);
            director.IdsDocument.Create(dummyValueData);

            var customGuid = new Guid("111111fa-be11-11bc-1a11-111a11111f1d");
            RhinoGet.GetPoint("Select point to generate sphere", false,
                out var inputPoint);
            var sphere = new Sphere(inputPoint, 2);
            var sphereMesh = Mesh.CreateFromBrep(sphere.ToBrep(), MeshingParameters.Default)[0];
            var sphereMeshData = new ObjectValueData(customGuid, new List<Guid>(){dummyValueData.Id}, new ObjectValue()); 

            var success = director.IdsDocument.Create(sphereMeshData);
            if (success)
            {
                InternalUtilities.AddObject(customGuid, sphereMesh, "sphereMesh", "TreeSphere");
            }

            return Result.Success;
        }
    }
}

#endif