using IDS.Core.ImplantDirector;
using Rhino;
using Rhino.Commands;
using System;

namespace IDS.Core.CommandBase
{
    public class CommandCallbackEventArgs<TImplantDirector> : EventArgs where TImplantDirector : class, IImplantDirector
    {
        public TImplantDirector Director { get; }

        public RhinoDoc Document => Director.Document;

        public string CommandName { get; }

        public RunMode Mode { get; }

        public CommandCallbackEventArgs(TImplantDirector director,
            string commandName, RunMode mode)
        {
            Director = director;
            CommandName = commandName;
            Mode = mode;
        }
    }
}
