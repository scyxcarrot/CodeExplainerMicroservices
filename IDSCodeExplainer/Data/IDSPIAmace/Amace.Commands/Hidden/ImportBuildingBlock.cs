#if DEBUG

using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Common.Commands
{
    /**
     * Rhino command to import an STL file and set it as an Implant Building
     * Block in the document.
     */

    [System.Runtime.InteropServices.Guid("ba2647fc-9b74-4cd5-b9a3-70cf36e78b49")]
    [IDSCommandAttributes(true, DesignPhase.Any)]
    public class ImportBuildingBlock : Rhino.Commands.Command
    {
        private static ImportBuildingBlock m_thecommand;

        public ImportBuildingBlock()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportBuildingBlock TheCommand => m_thecommand;

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportBuildingBlock";

        /**
         * Let user select building block he wants to set, then show dialog to open
         * stl file.
         */

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Check input data
            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Ask which building block to set
            GetOption go = new GetOption();
            go.SetCommandPrompt("Which building block do you want to set?");
            go.AcceptNothing(true);
            IBB usr_blocktype = IBB.DesignPelvis;
            List<IBB> sel_blocks = BuildingBlocks.GetAllBuildingBlocks()
                                             .Where(b => BuildingBlocks.Blocks[b].GeometryType == ObjectType.Mesh)
                                             .ToList();
            List<string> sel_desc = sel_blocks.Select(b => b.ToString().Replace(" ", "").Replace("-", ""))
                                              .ToList();

            go.AddOptionList("BuildingBlock", sel_desc, sel_desc.IndexOf("DesignPelvis"));
            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Option)
                {
                    usr_blocktype = sel_blocks[go.Option().CurrentListOptionIndex];
                }
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }
            }
            Result rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return rc;
            }

            // Show file dialog
            Rhino.UI.OpenFileDialog fd = new Rhino.UI.OpenFileDialog();
            fd.Title = "Please select STL file containing mesh";
            fd.Filter = "STL files (*.stl)|*.stl||";
            fd.InitialDirectory = Environment.SpecialFolder.Desktop.ToString();
            System.Windows.Forms.DialogResult drc = fd.ShowDialog();
            if (drc != System.Windows.Forms.DialogResult.OK)
            {
                RhinoApp.WriteLine("Invalid file. Aborting.");
                return Result.Failure;
            }
            string stl_file = fd.FileName;

            // Import the mesh
            Mesh block_mesh;
            bool read = StlUtilities.StlBinary2RhinoMesh(stl_file, out block_mesh);
            if (!read)
            {
                RhinoApp.WriteLine("Something went wrong while reading the STL file. Aborted.");
                return Result.Failure;
            }

            // Set it as building block
            //Guid block_id = doc.Objects.AddMesh(block_mesh);
            AmaceObjectManager objectManager = new AmaceObjectManager(director);
            objectManager.AddNewBuildingBlock(usr_blocktype, block_mesh);

            // Success!
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}

#endif