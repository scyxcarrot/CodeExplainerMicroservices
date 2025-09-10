using System;
using System.Collections.Generic;
using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class IdsDocumentUtilitiesTests
    {
        [TestMethod]
        public void Ids_Document_Tree_Recursive_Search_Test()
        {
            var caseNum = 1;
            var director = MockConnectionsInIdsDocument(caseNum);
            var data = director.CasePrefManager.GetCaseWithCaseIndex(caseNum);
            var dotPastilleIdList = IdsDocumentUtilities.RecursiveSearchClassInTree(director.IdsDocument, data.CaseGuid, typeof(DotPastille).FullName);
            var dotControlPointIdList = IdsDocumentUtilities.RecursiveSearchClassInTree(director.IdsDocument, data.CaseGuid, typeof(DotControlPoint).FullName);
            var connectionPlateIdList = IdsDocumentUtilities.RecursiveSearchClassInTree(director.IdsDocument, data.CaseGuid, typeof(ConnectionPlate).FullName);
            var connectionLinkIdList = IdsDocumentUtilities.RecursiveSearchClassInTree(director.IdsDocument, data.CaseGuid, typeof(ConnectionLink).FullName);

            Assert.AreEqual(3, dotPastilleIdList.Count, "Recursive search in IdsDocument for DotPastille is wrong!");
            Assert.AreEqual(2, dotControlPointIdList.Count, "Recursive search in IdsDocument for DotControlPoint is wrong!");
            Assert.AreEqual(3, connectionPlateIdList.Count, "Recursive search in IdsDocument for ConnectionPlate is wrong!");
            Assert.AreEqual(1, connectionLinkIdList.Count, "Recursive search in IdsDocument for ConnectionLink is wrong!");
        }

        private CMFImplantDirector MockConnectionsInIdsDocument(int caseNum)
        {
            const string implantType = "Genioplasty";
            const string screwType = "Matrix Orthognathic Ø1.85";
            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(EScrewBrand.Synthes, ESurgeryType.Orthognathic, implantType,
                screwType, caseNum, out var director, out _);

            var a = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
                Diameter = 5.0,
                Thickness = 2.0,
                CreationAlgoMethod = "MockCreationMethod",
                Id = Guid.NewGuid()
            };
            var b = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 0, Y = 0, Z = 1 },
                Direction = IDSVector3D.ZAxis,
                Diameter = 5.0,
                Thickness = 2.0,
                CreationAlgoMethod = "MockCreationMethod",
                Id = Guid.NewGuid()
            };
            var c = new DotControlPoint()
            {
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 0 },
                Direction = IDSVector3D.ZAxis,
                Id = Guid.NewGuid()

            };
            var d = new DotControlPoint()
            {
                Location = new IDSPoint3D() { X = 0, Y = 1, Z = 1 },
                Direction = IDSVector3D.ZAxis,
                Id = Guid.NewGuid()
            };
            var e = new DotPastille()
            {
                Location = new IDSPoint3D() { X = 1, Y = 0, Z = 0 },
                Direction = IDSVector3D.ZAxis,
                Diameter = 5.0,
                Thickness = 2.0,
                CreationAlgoMethod = "MockCreationMethod",
                Id = Guid.NewGuid()
            };

            var con1 = new ConnectionPlate()
            {
                A = a, 
                B = b,
                Id = Guid.NewGuid(),
                Thickness = 2.0,
                Width = 5.0
            };
            var con2 = new ConnectionPlate()
            {
                A = b, 
                B = c,
                Id = Guid.NewGuid(),
                Thickness = 2.0,
                Width = 5.0
            };
            var con3 = new ConnectionLink()
            {
                A = c, 
                B = d,
                Id = Guid.NewGuid(),
                Thickness = 2.0,
                Width = 5.0
            };
            var con4 = new ConnectionPlate()
            {
                A = d, 
                B = e,
                Id = Guid.NewGuid(),
                Thickness = 2.0,
                Width = 5.0
            };

            var connections = new List<IConnection>()
            {
                con1,
                con2,
                con3,
                con4
            };

            var data = director.CasePrefManager.GetCaseWithCaseIndex(caseNum);
            data.ImplantDataModel.ConnectionList = connections;

            director.ImplantManager.AddAllIDotToDocument(data);
            director.ImplantManager.AddAllIConnectionToDocument(data);

            return director;
        }
    }
}
