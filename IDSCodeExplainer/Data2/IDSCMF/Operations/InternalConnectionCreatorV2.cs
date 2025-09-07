using IDS.CMF.CasePreferences;
using IDS.CMF.Common;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Tracking;
using IDS.CMFImplantCreation.Configurations;
using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.Plugin;
using IDS.Core.V2.ExternalTools;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using IDS.Interface.Tools;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using IDSCMF.DataModel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDS.CMF.Operations
{
    public class InternalConnectionCreatorV2
    {
        public MsaiTrackingInfo TrackingInfo { get; set; }

        public int NumberOfTasks { get; set; } = 2;

        public bool GenerateImplantTubes(
            List<DotCurveDataModel> connectionDataModels, 
            CasePreferenceDataModel casePreferencesData,
            Mesh supportMesh, 
            Mesh supportMeshFull,
            IEnumerable<Screw> screws,
            bool isCreateActualConnection, 
            out Dictionary<Mesh, List<IDot>> implantSurfaces)
        {
            var console = new IDSRhinoConsole();

            var supportMeshIds = RemoveNoiseShellsAndFreePoints(
                console, RhinoMeshConverter.ToIDSMesh(supportMesh));
            var supportMeshFullIds = RemoveNoiseShellsAndFreePoints(
                console, RhinoMeshConverter.ToIDSMesh(supportMeshFull));
            
            var splitConnectionDataModels
                = ListUtilities.SplitListEvenly(connectionDataModels, NumberOfTasks);
            var tasks = splitConnectionDataModels
                .Select(splitConnectionDataModel =>
                {
                    return Task.Run(() => GenerateConnections(
                        splitConnectionDataModel,
                        casePreferencesData,
                        supportMeshIds,
                        supportMeshFullIds,
                        screws,
                        isCreateActualConnection));
                });

            var taskResults = Task.WhenAll(tasks);
            var allResults =
                taskResults.Result
                    .SelectMany(result => result)
                    .ToList();

            implantSurfaces = allResults
                .Where(result => result.Success)
                .ToDictionary(
                    result => result.ConnectionMesh, 
                    result => result.Dots);
            return allResults.All(result => result.Success);
        }

        private List<ConnectionResultDataModel> GenerateConnections(
            List<DotCurveDataModel> connectionDataModels,
            CasePreferenceDataModel casePreferenceDataModel,
            IMesh supportMesh,
            IMesh supportMeshFull,
            IEnumerable<Screw> screws,
            bool isCreateActualConnection)
        {
            return connectionDataModels.Select(
                connectionDataModel => GenerateConnections(
                    connectionDataModel, 
                    casePreferenceDataModel, 
                    supportMesh, 
                    supportMeshFull, 
                    screws, 
                    isCreateActualConnection))
                .ToList();
        }

        private ConnectionResultDataModel GenerateConnections(
            DotCurveDataModel connectionDataModel, 
            CasePreferenceDataModel casePreferenceDataModel,
            IMesh supportMesh, 
            IMesh supportMeshFull, 
            IEnumerable<Screw> screws, 
            bool isCreateActualConnection)
        {
            using (TimeTracking.NewInstance(
                           $"{TrackingConstants.DevMetrics}_V2GenerateConnection-Implant {casePreferenceDataModel.CaseName} (Id: {Guid.NewGuid()}) ({connectionDataModel.Curve.GetLength()}mm)",
                           TrackingInfo.AddTrackingParameterSafely))
            {
                try
                {
                    var dotIdList = connectionDataModel.Dots
                        .Select(dot => dot.Id.ToString());

                    var connectionCurve = connectionDataModel.Curve.ToIDSCurve(3);
                    if (connectionDataModel.Dots.Count == 2)
                    {
                        var points = connectionDataModel.Dots
                            .Select(dot => dot.Location)
                            .ToList();
                        connectionCurve = new IDSCurve(points);
                    }

                    var displayName = string.Join(", ", dotIdList);
                    var componentInfo = new ConnectionComponentInfo()
                    {
                        Id = Guid.NewGuid(),
                        DisplayName = displayName,
                        IsActual = isCreateActualConnection,
                        Thickness = connectionDataModel.ConnectionThickness,
                        Width = connectionDataModel.ConnectionWidth,
                        AverageConnectionDirection =
                            RhinoVector3dConverter.ToIVector3D(
                                connectionDataModel.AverageVector),
                        ConnectionCurve = connectionCurve,
                        SupportRoIMesh = supportMesh,
                        SupportMeshFull = supportMeshFull,
                    };

                    var console = new IDSRhinoConsole();
                    var factory = new ImplantFactory(console);
                    var result = factory.CreateImplant(componentInfo);

                    if (result.ErrorMessages.Any())
                    {
                        throw new Exception(result.ErrorMessages.Last());
                    }

                    if (result.IntermediateMeshes.ContainsKey(
                            ConnectionKeyNames.ConnectionMeshResult))
                    {
                        var connectionMesh =
                            RhinoMeshConverter.ToRhinoMesh(result.ComponentMesh);

                        return new ConnectionResultDataModel()
                        {
                            ConnectionMesh = connectionMesh,
                            Dots = connectionDataModel.Dots,
                            ErrorMessages = new List<string>(),
                            Success = true,
                        };
                    }

                    return new ConnectionResultDataModel()
                    {
                        ConnectionMesh = null,
                        Dots = connectionDataModel.Dots,
                        ErrorMessages = new List<string>() { $"Unable to find {ConnectionKeyNames.ConnectionMeshResult}" },
                        Success = false,
                    };
                }
                catch (Exception exception)
                {

                    ImplantCreationUtilities.GetDotInformation(
                        connectionDataModel.Curve,
                        connectionDataModel.Dots, screws,
                        out var dotA, out var dotB);

                    var dotStringA =
                        ImplantCreationUtilities.FormatDotDisplayString(
                            dotA, casePreferenceDataModel.NCase);
                    var dotStringB =
                        ImplantCreationUtilities.FormatDotDisplayString(
                            dotB, casePreferenceDataModel.NCase);

                    var errorMessage = 
                        $"Plate/Link between {dotStringA} and " +
                        $"{dotStringB} could not be created." +
                        $"\nThe following unknown exception was thrown. " +
                        $"Please report this to the development team." +
                        $"\n{exception}";

                    return new ConnectionResultDataModel()
                    {
                        ConnectionMesh = null,
                        Dots = connectionDataModel.Dots,
                        ErrorMessages = new List<string> { errorMessage },
                        Success = false,
                    };
                }
            }
        }

        private static IMesh RemoveNoiseShellsAndFreePoints(IConsole console, IMesh inputMesh)
        {
            var meshWithoutNoiseShells = AutoFixV2.RemoveNoiseShells(console, inputMesh);
            var meshWithoutFreePointsAndNoiseShells = AutoFixV2.RemoveFreePoints(console, meshWithoutNoiseShells);

            return meshWithoutFreePointsAndNoiseShells;
        }
    }
}
