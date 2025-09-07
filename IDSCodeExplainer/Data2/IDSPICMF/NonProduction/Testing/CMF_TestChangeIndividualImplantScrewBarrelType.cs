using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Commands;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;
using System.Windows.Forms;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("40635782-0443-4658-9D9C-7736D6BFA576")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestChangeIndividualImplantScrewBarrelType : CMFImplantScrewBaseCommand
    {
        public CMF_TestChangeIndividualImplantScrewBarrelType()
        {
            TheCommand = this;
        }

        public static CMF_TestChangeIndividualImplantScrewBarrelType TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestChangeIndividualImplantScrewBarrelType";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwGuidStr = string.Empty;
            var barrelType = string.Empty;
            Screw screw;

            if (mode == RunMode.Scripted)
            {
                var result = RhinoGet.GetString("ScrewGuid", false, ref screwGuidStr);
                var screwGuid = new Guid(screwGuidStr);
                screw = doc.Objects.Find(screwGuid) as Screw;
                if (result != Result.Success || screw == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid screw guid {screwGuidStr}");
                    return Result.Failure;
                }
            }
            else
            {
                screw = SelectScrew(doc, "Select an implant screw to change it's barrel type.");
                if (screw == null)
                {
                    return Result.Failure;
                }
            }

            // store the old barrel type to show users later
            var oldBarrelType = screw.BarrelType;

            if (mode == RunMode.Scripted)
            {
                // get barrel type here
                var result = RhinoGet.GetString("BarrelType", false, ref barrelType);

                var barrelTypeList = Queries.GetBarrelTypes(screw.ScrewType);
                if (result != Result.Success || string.IsNullOrEmpty(barrelType) || !barrelTypeList.Contains(barrelType))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid barrel type {barrelType}");
                    return Result.Failure;
                }
            }
            else
            {
                var go = new GetOption();
                go.SetCommandPrompt("Choose Barrel Type.");
                go.AcceptNothing(true);

                var barrelTypeList = Queries.GetBarrelTypes(screw.ScrewType);
                var barrelTypeDisplayList = barrelTypeList.Select(b => b.Replace(" ", ""));

                go.AddOptionList("BarrelType", barrelTypeDisplayList, barrelTypeList.IndexOf(oldBarrelType));
                
                // initialize the default value with an oldBarrelType to avoid passing empty string to BarrelType in screws
                barrelType = oldBarrelType;
                while (true)
                {
                    var res = go.Get();
                    if (res == GetResult.Cancel)
                    {
                        return Result.Cancel;
                    }

                    if (res == GetResult.Option)
                    {
                        barrelType = barrelTypeList[go.Option().CurrentListOptionIndex];
                        continue;
                    }

                    if (res == GetResult.Nothing)
                    {
                        break;
                    }
                }
            }

            if (barrelType == screw.BarrelType)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Selected barrel type is same as current barrel type: {barrelType}");
                return Result.Cancel;
            }

            //update barrel type
            screw.BarrelType = barrelType;

            // trigger to generate barrel: invalidation of RegisteredBarrel is handled by CMFBarrelRegistrator;
            // if need to be invalidated explicitly, call screw.InvalidateGuideScrewAidesReferencesInDocument();
            var objectManager = new CMFObjectManager(director);
            Mesh guideSupportMesh = null;
            if (objectManager.HasBuildingBlock(IBB.GuideSupport))
            {
                guideSupportMesh = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            }

            var screwBarrelRegistration = new CMFBarrelRegistrator(director);
            bool isBarrelLevelingSkipped;
            var registeredBarrelId = screwBarrelRegistration.RegisterSingleScrewBarrel(screw, guideSupportMesh, out isBarrelLevelingSkipped);
            screwBarrelRegistration.Dispose();
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Registered barrel id: {registeredBarrelId}, BarrelLevelingSkipped: {isBarrelLevelingSkipped}");

            //invalidate dependant parts
            RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(director, screw.Id);

            if (mode == RunMode.Interactive)
            {
                MessageBox.Show($"Barrel type has been changed fron {oldBarrelType} to {barrelType}" +
                                "\n(Re)registration and (re)calibration of barrel was performed." +
                                "\nInvalidation of dependant building blocks has been triggered.",
                    "Change Barrel Type", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return Result.Success;
        }
    }
#endif
}