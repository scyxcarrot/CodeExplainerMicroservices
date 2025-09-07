using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.Core.V2.Logic
{
    public abstract class LogicV2<TContext>: ILogicV2<TContext>
    {
        protected readonly IConsole console;

        protected LogicV2(IConsole console)
        {
            this.console = console;
        }

        /// <summary>
        /// A logic for execute
        /// </summary>
        /// <param name="context">context for get and set properties for </param>
        /// <returns></returns>
        public abstract LogicStatus Execute(TContext context);
    }
}
