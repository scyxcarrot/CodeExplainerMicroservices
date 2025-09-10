using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Utilities;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("D9163466-C5E0-48C1-AE29-345937C1B4B2")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFUpdatePlanning : CMFUpdateAnatomy
    {
        static CMFUpdatePlanning _instance;
        public CMFUpdatePlanning()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFUpdatePlanning command.</summary>
        public static CMFUpdatePlanning Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFUpdatePlanning;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var idsConsole = new IDSRhinoConsole();
            var logicHelper = new UpdatePlanningHelper(director, idsConsole, doc, mode);
            var logic = new UpdatePlanningLogic(idsConsole, logicHelper);
            var status = logic.Execute(out var result);

            if (status != LogicStatus.Success)
            {
                return status.ToResultStatus();
            }

            // TODO: Code below should be placed under ProcessLogicResult after ImportRecut and AddTrackingParameterSafely is completely decoupled
            AddTrackingParameterSafely("PreLoad Planning", StringUtilitiesV2.ElapsedTimeSpanToString(result.PreLoadPlanningTime));
            var success = UpdatePlanning(doc, mode, director, result.SelectedParts, result.PreLoadData,
                result.Loader, result.OsteotomyHandler, out var updateAnatomyTime);

            if (!success)
            {
                return Result.Failure;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, "Time spent UpdateProPlan " +
                 $"{ ((updateAnatomyTime.Milliseconds + result.PreLoadPlanningTime.Milliseconds) * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");

            var selectedReferenceObjects = new List<string>();
            foreach (var part in result.SelectedParts)
            {
                var data = result.PreLoadData.First(d => d.Name.ToLower() == part.ToLower());
                if (data.IsReferenceObject)
                {
                    selectedReferenceObjects.Add(data.Name);
                }
            }
            AddTrackingParameterSafely("Updated Reference Objects", string.Join(",", selectedReferenceObjects));

            return status.ToResultStatus();
        }

        private bool UpdatePlanning(RhinoDoc doc, RunMode mode, CMFImplantDirector director, List<string> partsToUpdate, 
            List<IPreopLoadResult> preLoadData, IPreopLoader loader, 
            List<IOsteotomyHandler> osteotomyHandler, out TimeSpan updateAnatomyTime)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            //filter preLoadData
            // TODO: Transform instead of IDSTransform is being used here because CMFUpdateAnatomy requires it
            var transformationMatrixMap = new List<Tuple<string, Transform>>();
            foreach (var part in partsToUpdate)
            {
                var data = preLoadData.First(d => d.Name.ToLower() == part.ToLower());
                transformationMatrixMap.Add(new Tuple<string, Transform>(data.Name, RhinoTransformConverter.ToRhinoTransformationMatrix(data.TransformationMatrix)));
            }

            loader.ExportPreopToStl(partsToUpdate, tempDirectory);

            var anatomyUpdater = new AnatomyUpdater(director, transformationMatrixMap, osteotomyHandler);
            var success = ExecuteOperation(doc, mode, director, tempDirectory, anatomyUpdater);

            stopwatch.Stop();
            AddTrackingParameterSafely("Update Anatomy", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
            updateAnatomyTime = stopwatch.Elapsed;

            loader.CleanUp();

            var directoryInfo = new DirectoryInfo(tempDirectory);
            directoryInfo.Delete(true);

            return success;
        }
    }
}