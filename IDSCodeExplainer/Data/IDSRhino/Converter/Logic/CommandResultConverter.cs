using IDS.Interface.Logic;
using Rhino.Commands;
using System;

namespace IDS.RhinoInterfaces.Converter
{
    public static class CommandResultConverter
    {
        public static Result ToResultStatus(this LogicStatus status)
        {
            switch (status)
            {
                case LogicStatus.Success:
                    return Result.Success;
                case LogicStatus.Cancel:
                    return Result.Cancel;
                case LogicStatus.Failure:
                    return Result.Failure;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
