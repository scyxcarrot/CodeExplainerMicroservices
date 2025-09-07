using IDS.Core.V2.TreeDb.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class CircularDependencyDetectorTest
    {
        [TestMethod]
        public void Has_Circular_Dependency_Test()
        {
            // Arrange 
            var parentId = new List<Guid>();
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            bool firstCheck,
                secondCheck,
                thirdCheck;

            // Act
            // a -> b -> *a(circular dependencies)
            using (var detector1 = new CircularDependencyDetector<Guid>(parentId, a))
            {
                firstCheck = detector1.HasCircularDependency();
                using (var detector2 = new CircularDependencyDetector<Guid>(parentId, b))
                {
                    secondCheck = detector2.HasCircularDependency();
                    using (var detector3 = new CircularDependencyDetector<Guid>(parentId, a))
                    {
                        thirdCheck = detector3.HasCircularDependency();
                    }
                }
            }

            // Assert
            Assert.AreEqual(0, parentId.Count, "parentId should be empty");
            Assert.AreEqual(false, firstCheck, "First condition check shouldn't consider as circular dependency");
            Assert.AreEqual(false, secondCheck, "Second condition check shouldn't consider as circular dependency");
            Assert.AreEqual(true, thirdCheck, "Third condition check should consider as circular dependency");
        }
    }
}
