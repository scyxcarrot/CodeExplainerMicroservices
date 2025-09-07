using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.Plugin;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ImplantPastilleCreationUtilitiesV2
    {
        private readonly DotPastille _pastille;
        private readonly Screw _screw;
        private readonly CasePreferenceDataModel _casePreferencesData;
        private readonly IMesh _supportMeshRoI;
        private readonly IMesh _supportMeshFull;
        private readonly bool _isCreateActualPastille;
        private readonly IMesh _pastilleLandmarkMesh;
        private readonly IMesh _screwStamp;
        private readonly bool _withFinalization;

        public ImplantPastilleCreationUtilitiesV2(DotPastille pastille, Screw screw, CasePreferenceDataModel casePreferencesData,
            IMesh supportMeshRoI, IMesh supportMeshFull, bool isCreateActualPastille,
            IMesh pastilleLandmarkMesh, IMesh screwStamp, bool withFinalization)
        {
            _pastille = pastille;
            _screw = screw;
            _casePreferencesData = casePreferencesData;
            _supportMeshRoI = supportMeshRoI;
            _supportMeshFull = supportMeshFull;
            _isCreateActualPastille = isCreateActualPastille;
            _pastilleLandmarkMesh = pastilleLandmarkMesh;
            _screwStamp = screwStamp;
            _withFinalization = withFinalization;
        }

        public bool GenerateImplantPastille(ref List<string> errorMessages, out Mesh finalFilteredMesh,
            out Mesh implantPastilleMesh, out Mesh cylinderMesh)
        {
            try
            {
                var console = new IDSRhinoConsole();
                var factory = new ImplantFactory(console);

                var componentMeshes = new List<IMesh>();
                if (_pastilleLandmarkMesh != null)
                {
                    componentMeshes.Add(_pastilleLandmarkMesh);
                }

                var subtractors = new List<IMesh>();
                if (_screwStamp != null)
                {
                    subtractors.Add(_screwStamp);
                }

                var componentInfo = new PastilleComponentInfo
                {
                    DisplayName = $"{_screw.Id}",
                    IsActual = _isCreateActualPastille,
                    ScrewType = _casePreferencesData.CasePrefData.ScrewTypeValue,
                    Direction = _pastille.Direction,
                    Thickness = _pastille.Thickness,
                    Location = _pastille.Location,
                    Diameter = _pastille.Diameter,
                    ScrewHeadPoint = RhinoPoint3dConverter.ToIPoint3D(_screw.HeadPoint),
                    ScrewDirection = RhinoVector3dConverter.ToIVector3D(_screw.Direction),
                    ComponentMeshes = componentMeshes,
                    Subtractors = subtractors,
                    ClearanceMesh = _supportMeshFull,
                    SupportRoIMesh = _supportMeshRoI,
                    NeedToFinalize = _withFinalization
                };

                var result = factory.CreateImplant(componentInfo);

                if (result.ErrorMessages.Any())
                {
                    if (result.ErrorMessages.Count > 1)
                    {
                        errorMessages.AddRange(result.ErrorMessages.Take(result.ErrorMessages.Count - 1));
                    }

                    throw new Exception(result.ErrorMessages.Last());
                }

                _pastille.CreationAlgoMethod = ((PastilleComponentResult)result).CreationAlgoMethod;

                if (result.IntermediateMeshes.ContainsKey(PastilleKeyNames.CylinderResult))
                {
                    cylinderMesh = RhinoMeshConverter.ToRhinoMesh(result.IntermediateMeshes[PastilleKeyNames.CylinderResult]);
                }
                else
                {
                    cylinderMesh = RhinoMeshConverter.ToRhinoMesh(result.IntermediateMeshes[PastilleKeyNames.SphereResult]);
                }

                implantPastilleMesh = RhinoMeshConverter.ToRhinoMesh(result.ComponentMesh);
                finalFilteredMesh = RhinoMeshConverter.ToRhinoMesh(result.FinalComponentMesh);

                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
