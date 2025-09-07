using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    public class BatchAssert
    {
        private readonly List<string> _errorMessages = new List<string>();

        public void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                _errorMessages.Add(message);
            }
        }

        public void DoAssert()
        {
            Assert.IsFalse(_errorMessages.Any(),
                $"Error:\n{string.Join("\n", _errorMessages)}");
        }
    }
}
