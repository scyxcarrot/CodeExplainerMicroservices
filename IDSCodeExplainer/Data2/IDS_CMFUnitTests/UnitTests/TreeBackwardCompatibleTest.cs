using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.TreeDb.Interface;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    [TestClass]
    public class TreeBackwardCompatibleTest
    {
        private readonly int _numberOfScrews = 2;

        [TestMethod]
        public void BackwardCompatible_Test()
        {
            // Arrange
            var director = CreateTestData();

            // Act
            TreeBackwardCompatibilityUtilities.CreateTree(director);

            // Assert
            Assert.IsTrue(CasePreferences_Backward_Compatible(director));
            Assert.IsTrue(IDot_Backward_Compatible(director));
            Assert.IsTrue(IConnection_Backward_Compatible(director));
            Assert.IsTrue(Connection_Curves_Backward_Compatible(director));
            Assert.IsTrue(Screws_Backward_Compatible(director));
            Assert.IsTrue(Barrels_Backward_Compatible(director));

            // TODO add others for backward compatible
        }


        private bool CasePreferences_Backward_Compatible(CMFImplantDirector director)
        {
            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var node = director.IdsDocument.GetNode(casePreference.CaseGuid);
                if (node == null || !node.Parents.Contains(IdsDocumentUtilities.RootGuid))
                {
                    return false;
                }
            }

            return true;
        }
        
        private bool IDot_Backward_Compatible(CMFImplantDirector director)
        {
            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                foreach (var dot in casePreference.ImplantDataModel.DotList)
                {
                    var node = director.IdsDocument.GetNode(dot.Id);
                    if (node == null || !node.Parents.Contains(casePreference.CaseGuid))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IConnection_Backward_Compatible(CMFImplantDirector director)
        {
            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                foreach (var connection in casePreference.ImplantDataModel.ConnectionList)
                {
                    var node = director.IdsDocument.GetNode(connection.Id);
                    if (node == null || 
                        !node.Parents.Contains(connection.A.Id) ||
                        !node.Parents.Contains(connection.B.Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool Connection_Curves_Backward_Compatible(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var connectionId = objectManager
                    .GetAllImplantExtendedImplantBuildingBlocks(
                        IBB.Connection,
                        casePreference).Select(c => c.Id);

                foreach (var id in connectionId)
                {
                    var connectionCurve = director.IdsDocument.GetNode(id);

                    if (connectionCurve == null || connectionCurve.Parents.Count > 1)
                    {
                        return false;
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// All screws are connected to Root
        /// </summary>
        private bool Screws_Backward_Compatible(CMFImplantDirector director)
        {
            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                foreach (var dot in casePreference.ImplantDataModel.DotList)
                {
                    if (dot is DotPastille dotPastille)
                    {
                        var node = director.IdsDocument.GetNode(dotPastille.Screw.Id);
                        if (node == null ||
                            !node.Parents.Contains(dotPastille.Id))
                        {
                            return false;
                        }
                    }
                    
                }
            }
            return true;
        }

        /// <summary>
        /// Barrels are connected to screw respectively
        ///      S S S
        ///      | | |
        ///      | | |
        ///      | | |
        ///      B B B
        /// </summary>
        private bool Barrels_Backward_Compatible(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);

            foreach (var casePref in director.CasePrefManager.CasePreferences)
            {
                var barrels = objectManager
                    .GetAllImplantExtendedImplantBuildingBlocks(
                        IBB.Screw,
                        casePref)
                    .Select(s => s.Attributes.UserDictionary?[AttributeKeys.KeyRegisteredBarrel])
                    .ToList();

                if (barrels.Count < _numberOfScrews)
                {
                    return false;
                }
                foreach (var barrel in barrels)
                {
                    var barrelId = new Guid(barrel.ToString());

                    var barrelNode = director.IdsDocument.GetNode(barrelId);

                    if (barrelNode == null || barrelNode.Parents.Count > 1)
                    {
                        return false;
                    }

                }

            }

            return true;
        }


        private CMFImplantDirector CreateTestData()
        {
            ImplantScrewTestUtilities
                .CreateDirectorAndImplantPreferenceDataModel(
                    Transform.Identity,
                    out var director,
                    out var implantPreferenceDataModels,
                    true,
                    "05GEN",
                    "01GEN",
                    false
                );

            var objectManager = new CMFObjectManager(director);
            var sphereBrep = BuildingBlockHelper.CreateSphereBrep(3);
            var implantCaseComponent = new ImplantCaseComponent();

            foreach (var casePref in director.CasePrefManager.CasePreferences)
            {
                var screwBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, casePref);
                // Add "Screw"
                var screwId1 = objectManager.AddNewBuildingBlock(screwBuildingBlock, sphereBrep);
                var screwId2 = objectManager.AddNewBuildingBlock(screwBuildingBlock, sphereBrep);

                var p1 = new DotPastille
                {
                    Id = Guid.NewGuid(),
                    Location = new IDSPoint3D(0, 0, 0),
                    Direction = IDSVector3D.ZAxis,
                    Screw = new ScrewData(){ Id = screwId1 },
                };

                var p2 = new DotPastille
                {
                    Id = Guid.NewGuid(),
                    Location = new IDSPoint3D(1, 0, 0),
                    Direction = IDSVector3D.ZAxis,
                    Screw = new ScrewData() { Id = screwId2 },
                };
                var connections = new List<IConnection>()
                {
                    new ConnectionPlate{ Id = Guid.NewGuid(), A = p1, B = p2},
                };

                // Add "Barrel"
                var screws = objectManager
                    .GetAllImplantExtendedImplantBuildingBlocks(
                        IBB.Screw,
                        casePref);
                screws.ForEach(s => s.Attributes.UserDictionary.Set(
                    AttributeKeys.KeyRegisteredBarrel,
                    Guid.NewGuid()));

                casePref.ImplantDataModel = new ImplantDataModel(connections);

                var buildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Connection, casePref);
                var curves = ImplantCreationUtilities.CreateImplantConnectionCurves(casePref.ImplantDataModel.ConnectionList);
                curves.ForEach(c =>
                {
                    objectManager.AddNewBuildingBlock(buildingBlock, c);
                });
            }
            return director;
        }
    }

#endif
}
