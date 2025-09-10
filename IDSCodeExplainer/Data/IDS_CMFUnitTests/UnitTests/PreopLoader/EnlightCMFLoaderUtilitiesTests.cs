using IDS.CMF.V2.Loader;
using IDS.EnlightCMFIntegration.DataModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class EnlightCMFLoaderUtilitiesTests
    {
        [TestMethod]
        public void Selected_WorkflowObject_Will_Have_InternalName_As_Name()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "Mandible - CT",
                    InternalName = "00MAN"
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 1);
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00MAN" && o.InternalName == "00MAN"));
        }

        [TestMethod]
        public void Last_Object_Will_Be_Selected_When_There_Are_Duplicate_InternalNames()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "Mandible - CT",
                    InternalName = "00MAN",
                    Index = 1
                },
                new StlProperties
                {
                    Name = "Mandible - CT",
                    InternalName = "00MAN",
                    Index = 2
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 1);
            Assert.IsTrue(filteredObjects.Any(o => o.Index == 2));
        }

        [TestMethod]
        public void Last_Object_Will_Be_Selected_When_There_Are_Duplicate_UiNames_For_ReferenceObjects()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "00MAN",
                    InternalName = "Ref object",
                    Index = 1
                },
                new StlProperties
                {
                    Name = "00MAN",
                    InternalName = "Ref object",
                    Index = 2
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 1);
            Assert.IsTrue(filteredObjects.Any(o => o.Index == 2));
        }

        [TestMethod]
        public void Object_Without_InternalName_Will_Be_Filtered_Out()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "Mandible - CT",
                    InternalName = "00MAN"
                },
                new StlProperties
                {
                    Name = "Mandible1",
                    InternalName = null
                },
                new StlProperties
                {
                    Name = "Mandible2",
                    InternalName = string.Empty
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 1);
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00MAN" && o.InternalName == "00MAN"));
        }

        [TestMethod]
        public void Object_With_CorrectName_But_Without_InternalName_Will_Be_Filtered_Out()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "00MAN",
                    InternalName = null
                },
                new StlProperties
                {
                    Name = "01MAN",
                    InternalName = string.Empty
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 0);
        }

        [TestMethod]
        public void WorkflowObject_Is_Selected_When_There_Is_No_ReferenceObject()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "Mandible - CT",
                    InternalName = "00MAN"
                },
                new StlProperties
                {
                    Name = "Nerve L - Planned",
                    InternalName = "05MAN_nerve_L"
                },
                new StlProperties
                {
                    Name = "Teeth - Maxilla R",
                    InternalName = "01MAX_teeth_R"
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 3);
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00MAN"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "05MAN_nerve_L"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "01MAX_teeth_R"));
        }

        [TestMethod]
        public void Part_With_UiName_Is_Selected_When_There_Is_ReferenceObject()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "00MAN_comp",
                    InternalName = "Ref object"
                },
                new StlProperties
                {
                    Name = "00SKU",
                    InternalName = "Ref object"
                },
                new StlProperties
                {
                    Name = "05MAX",
                    InternalName = "Ref object"
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 3);
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00MAN_comp"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00SKU"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "05MAX"));
        }

        [TestMethod]
        public void WorkflowObject_Is_Overwritten_By_ReferenceObject_When_There_Is_SameName()
        {
            //Arrange
            var objectProperties = new List<IObjectProperties>
            {
                new StlProperties
                {
                    Name = "Maxilla - Planned",
                    InternalName = "05MAX"
                },
                new StlProperties
                {
                    Name = "05MAX",
                    InternalName = "Ref object"
                },
                new StlProperties
                {
                    Name = "Nerve L - Planned",
                    InternalName = "05MAN_nerve_L"
                },
                new StlProperties
                {
                    Name = "00SKU",
                    InternalName = "Ref object"
                }
            };

            //Act
            var filteredObjects = EnlightCMFLoaderUtilities.FilterParts(objectProperties);

            //Assert
            Assert.AreEqual(filteredObjects.Count, 3);
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "05MAX" && o.InternalName == "Ref object"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "05MAN_nerve_L"));
            Assert.IsTrue(filteredObjects.Any(o => o.Name == "00SKU"));
        }

        [TestMethod]
        public void ReferenceObject_Is_Detected()
        {
            //Arrange
            var objectProperties = new StlProperties
            {
                Name = "00MAN_comp",
                InternalName = "Ref object"
            };

            //Act
            var isRefObject = EnlightCMFLoaderUtilities.IsReferenceObject(objectProperties);

            //Assert
            Assert.IsTrue(isRefObject);
        }

        [TestMethod]
        public void ReferenceObject_Is_Not_Detected_When_InternalName_Is_Null()
        {
            //Arrange
            var objectProperties = new StlProperties
            {
                Name = "00MAN_comp",
                InternalName = null
            };

            //Act
            var isRefObject = EnlightCMFLoaderUtilities.IsReferenceObject(objectProperties);

            //Assert
            Assert.IsFalse(isRefObject);
        }

        [TestMethod]
        public void ReferenceObject_Is_Not_Detected_When_InternalName_Is_Empty()
        {
            //Arrange
            var objectProperties = new StlProperties
            {
                Name = "00MAN_comp",
                InternalName = string.Empty
            };

            //Act
            var isRefObject = EnlightCMFLoaderUtilities.IsReferenceObject(objectProperties);

            //Assert
            Assert.IsFalse(isRefObject);
        }

        [TestMethod]
        public void ReferenceObject_Is_Not_Detected_When_InternalName_Contains_Incorrect_Value()
        {
            //Arrange
            var objectProperties = new StlProperties
            {
                Name = "Mandible - CT",
                InternalName = "00MAN"
            };

            //Act
            var isRefObject = EnlightCMFLoaderUtilities.IsReferenceObject(objectProperties);

            //Assert
            Assert.IsFalse(isRefObject);
        }
    }
}
