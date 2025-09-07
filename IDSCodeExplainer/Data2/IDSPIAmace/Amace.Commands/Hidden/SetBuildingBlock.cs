#if DEBUG

using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Common.Commands
{
    /**
     * Command to set the scaffold volume to an existing mesh
     * in the document.
     */

    [System.Runtime.InteropServices.Guid("056CBCCE-C855-4150-A22B-60C6E00D5C93")]
    [IDSCommandAttributes(false, DesignPhase.Any)]
    public class SetBuildingBlock : Rhino.Commands.Command
    {
        private static SetBuildingBlock m_thecommand;

        public SetBuildingBlock()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static SetBuildingBlock TheCommand => m_thecommand;

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "SetBuildingBlock";

        /**
         * Load the MBV volume from an existing mesh in the document
         * instead of creating it.
         */

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Check if all needed data is available
            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            bool ready = director.IsCommandRunnable(this, true);
            if (!ready)
            {
                return Result.Success;
            }

            // Ask which building block to set
            GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Which building block do you want to set?");
            go.AcceptNothing(true);
            IBB usr_blocktype = IBB.DesignPelvis; // default block
            //go.AddOptionEnumList<IBB>("BuildingBlock", usr_blocktype);

            List<IBB> all_blocks = BuildingBlocks.GetAllBuildingBlocks().ToList();
            List<IBB> sel_blocks = new List<IBB>();
            List<string> sel_desc = new List<string>();
            for (int i = 0; i < all_blocks.Count; i++)
            {
                // Skip non-modifiable blocks
                IBB block = all_blocks[i];
                if (block == IBB.Generic)
                {
                    continue;
                }

                // Strings inside command cannot contain spaces or dashes
                //string descr = BuildingBlockProperties.getDescription(block);
                string descr = block.ToString();
                descr = descr.Replace(" ", "").Replace("-", "");
                sel_blocks.Add(block);
                sel_desc.Add(descr);
            }
            go.AddOptionList("BuildingBlock", sel_desc, sel_blocks.IndexOf(usr_blocktype));
            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return Result.Failure;
                }
                if (res == GetResult.Option)
                {
                    //usr_blocktype = go.GetSelectedEnumValue<IBB>();
                    usr_blocktype = sel_blocks[go.Option().CurrentListOptionIndex];
                }
            }
            Result rc = go.CommandResult();
            if (rc != Result.Success)
            {
                return rc;
            }

            // Get block info based on selection
            string block_desc = BuildingBlocks.Blocks[usr_blocktype].Name;
            ObjectType block_filter = BuildingBlocks.Blocks[usr_blocktype].GeometryType;

            // Let user select object
            GetObject gm = new GetObject();
            gm.SetCommandPrompt(string.Format("Select building block <{0}>", block_desc));
            gm.DisablePreSelect();
            gm.AcceptNothing(false);
            gm.GeometryFilter = block_filter;
            Rhino.Input.GetResult gottype = gm.Get();
            rc = gm.CommandResult();
            if (rc != Result.Success || gottype != Rhino.Input.GetResult.Object)
            {
                return rc;
            }

            // Add to director
            ObjRef sel_obj = gm.Object(0);
            if (null == sel_obj)
            {
                return Result.Failure;
            }
            Guid blockId = sel_obj.ObjectId;
            if (Guid.Empty == blockId)
            {
                return Result.Failure;
            }
            RhinoObject restored = sel_obj.Object();

            // Check if it is a custom object
            if (usr_blocktype == IBB.Cup)
            {
                BrepObject oldCup = sel_obj.Object() as BrepObject;
                if (null == oldCup)
                {
                    return Result.Failure;
                }
                restored = Cup.CreateFromArchived(oldCup, true);
            }
            else if (usr_blocktype == IBB.Screw)
            {
                BrepObject oldObj = sel_obj.Object() as BrepObject;
                if (null == oldObj)
                {
                    return Result.Failure;
                }
                restored = ScrewHelper.CreateFromArchived(oldObj, true);
            }
            if (null == restored)
            {
                RhinoApp.WriteLine("[IDS] Error: the object could not be restored from its attached archive file");
                return Result.Failure;
            }

            AmaceObjectManager objectManager = new AmaceObjectManager(director);
            objectManager.AddNewBuildingBlock(usr_blocktype, restored.Geometry);
            RhinoApp.WriteLine("Successfully set the block.");
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}

#endif