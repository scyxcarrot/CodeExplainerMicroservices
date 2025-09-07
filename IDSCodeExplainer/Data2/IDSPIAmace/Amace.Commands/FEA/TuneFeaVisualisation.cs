using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Fea;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("E534A31F-0714-4E23-AF6F-E5D1A807B57C")]
    [IDSCommandAttributes(true, DesignPhase.ImplantQC | DesignPhase.Export, IBB.PlateHoles, IBB.SolidPlateBottom, IBB.OriginalReamedPelvis)]
    public class TuneFeaVisualisation : CommandBase<ImplantDirector>
    {
        public static double SafetyFactorLow
        {
            get
            {
                return Proxies.TuneFeaVisualisation.SafetyFactorLow;
            }
            private set
            {
                Proxies.TuneFeaVisualisation.SafetyFactorLow = value;
            }
        }

        public static double SafetyFactorMiddle
        {
            get
            {
                return Proxies.TuneFeaVisualisation.SafetyFactorMiddle;
            }
            private set
            {
                Proxies.TuneFeaVisualisation.SafetyFactorMiddle = value;
            }
        }

        public static double SafetyFactorHigh
        {
            get
            {
                return Proxies.TuneFeaVisualisation.SafetyFactorHigh;
            }
            private set
            {
                Proxies.TuneFeaVisualisation.SafetyFactorHigh = value;
            }
        }

        public static double UltimateTensileStrength
        {
            get
            {
                return Proxies.TuneFeaVisualisation.UltimateTensileStrength;
            }
            private set
            {
                Proxies.TuneFeaVisualisation.UltimateTensileStrength = value;
            }
        }

        public static double FatigueLimit
        {
            get
            {
                return Proxies.TuneFeaVisualisation.FatigueLimit;
            }
            private set
            {
                Proxies.TuneFeaVisualisation.FatigueLimit = value;
            }
        }

        public TuneFeaVisualisation()
        {
            Instance = this;

            SafetyFactorLow = FeaConduit.safetyFactorLowDefault;
            SafetyFactorMiddle = FeaConduit.safetyFactorMiddleDefault;
            SafetyFactorHigh = FeaConduit.safetyFactorHighDefault;
            UltimateTensileStrength = Materials.Titanium.UltimateTensileStrength;
            FatigueLimit = Materials.Titanium.FatigueLimit;
        }

        ///<summary>The only instance of the PerformFea command.</summary>
        public static TuneFeaVisualisation Instance { get; private set; }

        public override string EnglishName => "TuneFeaVisualisation";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            if (!PerformFea.ConduitEnabled)
            {
                IDSPIAmacePlugIn.WriteLine(Core.Enumerators.LogCategory.Error, "TuneFeaVisualisation command can only be used if the FEA visualisation is visible.");

                return Result.Failure;
            }

            // Ask the user if the FEA should be deleted.
            var go = new GetOption();
            go.SetCommandPrompt("VBT Options");
            go.AcceptNothing(true);
            // Color Scale Settings
            var optSafetyFactorLow = new OptionDouble(Proxies.PerformFea.FeaConduit.safetyFactorLow);
            go.AddOptionDouble("SafetyLow", ref optSafetyFactorLow);
            var optSafetyFactorMiddle = new OptionDouble(Proxies.PerformFea.FeaConduit.safetyFactorMiddle);
            go.AddOptionDouble("SafetyMiddle", ref optSafetyFactorMiddle);
            var optSafetyFactorHigh = new OptionDouble(Proxies.PerformFea.FeaConduit.safetyFactorHigh);
            go.AddOptionDouble("SafetyHigh", ref optSafetyFactorHigh);
            // Material properties
            var optMaterialUTS = new OptionDouble(Proxies.PerformFea.FeaConduit.ultimateTensileStrength);
            go.AddOptionDouble("MaterialUTS", ref optMaterialUTS);
            var optMaterialFatigueLimit = new OptionDouble(Proxies.PerformFea.FeaConduit.fatigueLimit);
            go.AddOptionDouble("MaterialFatigueLimit", ref optMaterialFatigueLimit);

            // Get user input
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (res == GetResult.Nothing)
                {
                    if (optSafetyFactorLow.CurrentValue < optSafetyFactorMiddle.CurrentValue &&
                        optSafetyFactorMiddle.CurrentValue < optSafetyFactorHigh.CurrentValue)
                    {
                        break;
                    }

                    return Result.Failure;
                }

            }

            // Set static variables for retrieval later on
            SafetyFactorLow = optSafetyFactorLow.CurrentValue;
            SafetyFactorMiddle = optSafetyFactorMiddle.CurrentValue;
            SafetyFactorHigh = optSafetyFactorHigh.CurrentValue;
            FatigueLimit = optMaterialFatigueLimit.CurrentValue;
            UltimateTensileStrength = optMaterialUTS.CurrentValue;

            // Set visualisation parameters
            Proxies.PerformFea.FeaConduit.SetVisualisationParameters(SafetyFactorLow, SafetyFactorMiddle, SafetyFactorHigh, FatigueLimit, UltimateTensileStrength);

            return Result.Success;
        }
    }
}
