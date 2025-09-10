using IDS.CMF.DataModel;
using IDS.CMF.TestLib.Components;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantDataModelConvertTests
    {
        private const double SimpleDiameter = 2.2;
        private const double SimpleThickness = 1.2;
        private const double SimplePlateWidth = 1.75;
        private const double SimpleLinkWidth = 1.35;

        private DotPastille CreateSimpleDotPastille(IDSPoint3D location, IDSVector3D direction)
        {
            var algo = DotPastille.CreationAlgoMethods[0];

            return new DotPastille()
            {
                Location = location,
                Direction = direction,
                CreationAlgoMethod = algo,
                Diameter = SimpleDiameter,
                Id = Guid.NewGuid(),
                Landmark = null,
                Screw = null,
                Thickness = SimpleThickness
            };
        }

        private DotControlPoint CreateSimpleDotControlPoint(IDSPoint3D location, IDSVector3D direction)
        {
            return new DotControlPoint()
            {
                Location = location,
                Direction = direction
            };
        }

        private ConnectionPlate CreateSimplePlate(IDot dotA, IDot dotB)
        {
            return new ConnectionPlate()
            {
                A = dotA, 
                B = dotB,
                Thickness = SimpleThickness, 
                Width = SimplePlateWidth
            };
        }

        private ConnectionLink CreateSimpleLink(IDot dotA, IDot dotB)
        {
            return new ConnectionLink()
            {
                A = dotA,
                B = dotB,
                Thickness = SimpleThickness,
                Width = SimpleLinkWidth
            };
        }

        [TestMethod]
        public void Basic_Serialize_Deserialize_Test()
        {
            // Arrange
            var caseGuid = Guid.NewGuid();
            var dot1 = CreateSimpleDotPastille(
                new IDSPoint3D(0, 0, 0), new IDSVector3D(1, 0, 0));
            var dot2 = CreateSimpleDotControlPoint(
                new IDSPoint3D(1, 1, 1), new IDSVector3D(0, 1, 0));
            var dot3 = CreateSimpleDotPastille(
                new IDSPoint3D(0, 1, 1), new IDSVector3D(0, 0, 1));
            var dot4 = CreateSimpleDotControlPoint(
                new IDSPoint3D(0, 0, 1), new IDSVector3D(0, 1, 0));

            var connection = new List<IConnection>()
            {
                CreateSimplePlate(dot1, dot2),
                CreateSimpleLink(dot2, dot3),
                CreateSimplePlate(dot3, dot4),
                CreateSimpleLink(dot4, dot1)
            };

            var expectedImplantDataModel = new ImplantDataModel(connection);
            
            // Act
            var implantDataModelComponent = new ImplantDataModelComponent();
            implantDataModelComponent.SetImplantDataModel(caseGuid, expectedImplantDataModel);
            var actualImplantDataModel = implantDataModelComponent.GetImplantDataModel();

            // Assert
            Assert.AreEqual(caseGuid, implantDataModelComponent.CaseGuid, "Case Guid not assign properly");
            AssertImplantModelAreEqual(expectedImplantDataModel, actualImplantDataModel);
        }

        private void AssertImplantModelAreEqual(ImplantDataModel expected, ImplantDataModel actual)
        {
            var expectedDotList = expected.DotList.ToList();
            var actualDotList = actual.DotList.ToList();

            var expectedConnectionList = expected.ConnectionList.ToList();
            var actualConnectionList = actual.ConnectionList.ToList();

            Assert.AreEqual(expectedDotList.Count, actualDotList.Count, 
                "DotList count is not match");
            Assert.AreEqual(expectedConnectionList.Count, actualConnectionList.Count, 
                "ConnectionList count is not match");

            for (var i = 0; i < expectedDotList.Count; i++)
            {
                DotTestUtilities.AssertDotAreEqual(expectedDotList[i], actualDotList[i]);
            }

            for (var i = 0; i < expectedDotList.Count; i++)
            {
                ConnectionTestUtilities.AssertConnectionAreEqual(expectedConnectionList[i], expectedDotList, 
                    actualConnectionList[i], actualDotList);
            }
        }

        
    }
}
