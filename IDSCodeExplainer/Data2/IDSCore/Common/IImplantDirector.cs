using IDS.Core.Enumerators;
using IDS.Core.SplashScreen;
using IDS.Core.V2.TreeDb.Model;
using Rhino;
using Rhino.FileIO;
using System;
using System.Collections.Generic;

namespace IDS.Core.ImplantDirector
{
    public interface IImplantDirector : ICaseInfoProvider
    {
        IPluginInfoModel PluginInfoModel { get; set; }

        DocumentType documentType { get; set; }

        Dictionary<string, Dictionary<string, DateTime>> ComponentDateTimes { get; }

        Dictionary<string, Dictionary<string, string>> ComponentVersions { get; }

        RhinoDoc Document { get; }

        IDSDocument IdsDocument { get; }

        string FileName { get; set; }

        List<string> InputFiles { get; set; }
        
        bool IsCommandRunnable(Rhino.Commands.Command command, bool printMessage = false);
        
        void PrepareObjectsForArchive();
        
        void RestoreCustomRhinoObjects(RhinoDoc doc);
        
        void UnsubscribeCallbacks();
        
        void WriteToArchive(BinaryArchiveWriter archive);
        
        void SetVisibilityByPhase();

        string CurrentDesignPhaseName { get; }

        void OnInitialView(RhinoDoc openedDoc);

        void OnObjectDeleted();

        DesignPhaseProperty CurrentDesignPhaseProperty { get; }

        void EnterDesignPhase(DesignPhaseProperty toPhase);

        bool IsTestingMode { get; set; }
        bool IsForUserTesting { get; set; }
    }
}