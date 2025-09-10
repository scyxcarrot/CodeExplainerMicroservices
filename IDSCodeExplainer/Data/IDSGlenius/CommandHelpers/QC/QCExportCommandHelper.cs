using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Quality;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.CommandHelpers
{
    public class QCExportCommandHelper
    {
        public bool DoQCExport(GleniusImplantDirector director, DocumentType documentType, bool useProductionRodWithChamfer)
        {
            var outputDirectory = "";
            if (documentType == DocumentType.ScrewQC || documentType == DocumentType.ScaffoldQC)
            {
                if (director.documentType != DocumentType.Work)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Current 3dm-File must be a Work-File");
                    return false;
                }

                if (!HandleDraftFolderCreation(director, ref outputDirectory))
                {
                    return false;
                }
            }
            else if (documentType == DocumentType.ApprovedQC)
            {
                if (!HandleOutputFolderCreation(director, ref outputDirectory))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            var prevDocType = director.documentType;
            director.documentType = documentType;

            var projectFileName = Path.Combine(outputDirectory, Create3dmFileName(director, documentType));
            var options = new Rhino.FileIO.FileWriteOptions();
            if (director.Document.WriteFile(projectFileName, options))
            {
                director.documentType = prevDocType;
                File.SetAttributes(projectFileName, FileAttributes.ReadOnly);

                if (ExportData(outputDirectory, director, documentType, useProductionRodWithChamfer))
                {
                    SystemTools.OpenExplorerInFolder(outputDirectory);
                    SystemTools.DiscardChanges();
                    Msai.Terminate(director.PluginInfoModel, director.FileName, director.version, director.draft);
                    RhinoApp.Exit();
                    return true;
                }
                Dialogs.ShowMessageBox("Could not export all files.", "Export fails", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SystemTools.DiscardChanges();
                Msai.Terminate(director.PluginInfoModel, director.FileName, director.version, director.draft);
                RhinoApp.Exit();
                return false;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Could not create project draft file.");
            return false;
        }

        private bool HandleDraftFolderCreation(GleniusImplantDirector director, ref string outputDirectory)
        {
            outputDirectory = DirectoryStructure.GetDraftFolderPath(director);

            //Check if parent folder is good
            var outputFolderName = outputDirectory.Split('\\').LastOrDefault();

            var parentOfWorkingDir = DirectoryStructure.GetWorkingDir(director.Document);
            parentOfWorkingDir += "\\..\\";

            if (!DirectoryStructure.CheckDirectoryIntegrity(parentOfWorkingDir,
                new List<string>() { "work", outputFolderName },
                director.InputFiles.Select(file => Path.GetFileName(file)).ToList(), new List<string>() { "stl" }))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, string.Format("Subfolders not allowed in parent folder of current Work-file except: Work, {0} Folder", outputFolderName));
                return false;
            }

            if (!HandleOutputDirectoryExistanceCheck(outputDirectory))
            {
                return false;
            }

            if (outputDirectory != DirectoryStructure.MakeDraftFolder(director))
            {
                return false;
            }

            RhinoApp.RunScript("-_Save Version=6 _Enter", false);

            return true;
        }

        private bool HandleOutputFolderCreation(GleniusImplantDirector director, ref string outputDirectory)
        {
            outputDirectory = DirectoryStructure.GetOutputFolderPath(director.Document);

            if (!HandleOutputDirectoryExistanceCheck(outputDirectory))
            {
                return false;
            }

            if (outputDirectory != DirectoryStructure.MakeOutputFolder(director.Document))
            {
                return false;
            }

            return true;
        }

        private bool HandleOutputDirectoryExistanceCheck(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                var deleteExistingDialogResult =
                    Dialogs.ShowMessageBox(
                        "A Draft folder already exists and will be deleted. Is this OK?", "Draft folder exists",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (deleteExistingDialogResult == DialogResult.Yes)
                {
                    SystemTools.DeleteRecursively(outputDirectory);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool ExportData(string outputDirectory, GleniusImplantDirector director, DocumentType docType, bool useProductionRodWithChamfer)
        {
            var failedEntities = new List<string>();
            var failedSTLEntities = new List<string>();
            var failedXMLEntities = new List<string>();
            var stlCount = 0;
            var xmlCount = 0;
            var exceptionThrown = false;

            try
            {
                var qcReportFile = Path.Combine(outputDirectory, CreateReportFileName(director, docType));
                var exporter = new QualityReportExporter(docType);
                var resources = new Resources();
                exporter.ExportReport(director, qcReportFile, resources);

                var sTLExporter = new QCSTLExporter(director);
                sTLExporter.UseProductionRodWithChamfer = useProductionRodWithChamfer;
                stlCount = sTLExporter.DoExport(docType, outputDirectory, out failedSTLEntities);

                var xMLExporter = new QCXMLExporter(director);
                xmlCount = xMLExporter.DoExport(docType, outputDirectory, out failedXMLEntities);

                var folderArchiver = new QCFolderArchiver(director);
                folderArchiver.ZipAllFolders(docType, outputDirectory);
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                IDSPluginHelper.WriteLine(LogCategory.Error, "Exception occurred in ExportData:\n{0}", ex.ToString());
            }
            finally
            {
                if (!exceptionThrown)
                {
                    var totalExportedItems = stlCount + xmlCount;
                    failedEntities.AddRange(failedSTLEntities);
                    failedEntities.AddRange(failedXMLEntities);
                    var failedExportedItems = failedEntities.Count > 0 ? string.Join(",", failedEntities) : "NIL";
                    IDSPluginHelper.WriteLine(LogCategory.Default,
                        "Operation name: {0}\nNo.Of Exported items: {1}/{2}\nFailed Exported items: {3}",
                        docType, totalExportedItems - failedEntities.Count, totalExportedItems,
                        failedExportedItems);
                }
            }

            return !exceptionThrown && failedEntities.Count == 0;
        }

        private string Create3dmFileName(GleniusImplantDirector director, DocumentType docType)
        {
            if (docType == DocumentType.ApprovedQC)
            {
                return $"{director.caseId}_IDS_Export.3dm";
            }
            return $"{director.caseId}_IDS_Draft.3dm";
        }

        private string CreateReportFileName(GleniusImplantDirector director, DocumentType docType)
        {
            if (docType == DocumentType.ApprovedQC)
            {
                return $"{director.caseId}_IDS_Export_Report.html";
            }
            return $"{director.caseId}_IDS_QC_Report.html";
        }
    }
}
