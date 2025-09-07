using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.TestLib;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ImplantScrewQcLiveUpdateHandlerTests
    {
        private IndividualImplantScrewQcResultDatabase GetIndividualImplantScrewQcResultDatabase(
            ImplantScrewSerializableDataModel implantScrewA,
            ImplantScrewSerializableDataModel implantScrewB)
        {
            return new IndividualImplantScrewQcResultDatabase
            {
                ScrewId = implantScrewA.Id,
                SkipOstDistAndIntersect = new SkipOstDistAndIntersectContent(true),
                MinMaxDistance = new MinMaxDistanceSerializableContent()
                {
                    TooCloseScrews = { implantScrewB },
                    TooFarScrews = { implantScrewB },
                },
                AnatomicalObstacle = new ImplantScrewAnatomicalObstacleContent()
                {
                    DistanceToAnatomicalObstacles = 458238794.5345345
                },
                OsteotomyDistance = new OsteotomyDistanceSerializableContent()
                {
                    Distance = 5432.534,
                    IsFloatingScrew = true,
                    IsOk = true,
                    PtFrom = new IDSPoint3D(4.5435,543534.6,3124.4),
                    PtTo = new IDSPoint3D(56.4,657.76,6546534.54)
                },
                OsteotomyIntersection = new OsteotomyIntersectionContent()
                {
                    HasOsteotomyPlane = true,
                    IsFloatingScrew = true,
                    IsIntersected = true
                },
                VicinityResult = new ImplantScrewVicinitySerializableContent()
                {
                    ScrewsInVicinity = { implantScrewB }
                },
                PastilleDeformed = new PastilleDeformedContent()
                {
                    IsPastilleDeformed = true
                },
                BarrelType = new BarrelTypeContent()
                {
                    BarrelType = "Dummy Barrel Type"
                }
            };
        }

        private ImplantScrewQcDatabase GetImplantScrewQcDatabase()
        {
            var implantScrewA = new ImplantScrewSerializableDataModel()
            {
                Id = Guid.NewGuid(),
                NCase = 1,
                Index = 1
            };
            var implantScrewB = new ImplantScrewSerializableDataModel()
            {
                Id = Guid.NewGuid(),
                NCase = 1,
                Index = 2
            };

            var implantScrewQcResultDatabaseA = GetIndividualImplantScrewQcResultDatabase(implantScrewA, implantScrewB);
            var implantScrewQcResultDatabaseB = GetIndividualImplantScrewQcResultDatabase(implantScrewB, implantScrewA);

            return new ImplantScrewQcDatabase()
            {
                LatestImplantScrewInfoRecords = new List<ImplantScrewSerializableDataModel>()
                {
                    implantScrewA,
                    implantScrewB
                },
                ImplantScrewQcResultDatabase = new List<IndividualImplantScrewQcResultDatabase>()
                {
                    implantScrewQcResultDatabaseA,
                    implantScrewQcResultDatabaseB
                }
            };
        }

        [TestMethod]
        public void ImplantScrewQcLiveUpdateHandler_Copy_Test()
        {
            // Arrange
            var database = GetImplantScrewQcDatabase();

            // Act
            var liveUpdateHandler = ImplantScrewQcDatabaseUtilities.GetImplantScrewLiveUpdateHandler(database);
            var newDatabase = ImplantScrewQcDatabaseUtilities.GetImplantScrewQcDatabase(liveUpdateHandler);
            // Sorted the LatestImplantScrewInfoRecords
            database.LatestImplantScrewInfoRecords =
                database.LatestImplantScrewInfoRecords.OrderBy(s => s.Id.GetHashCode()).ToList();
            newDatabase.LatestImplantScrewInfoRecords =
                newDatabase.LatestImplantScrewInfoRecords.OrderBy(s => s.Id.GetHashCode()).ToList();
            // Sorted the ImplantScrewQcResultDatabase
            database.ImplantScrewQcResultDatabase =
                database.ImplantScrewQcResultDatabase.OrderBy(s => s.ScrewId.GetHashCode()).ToList();
            newDatabase.ImplantScrewQcResultDatabase =
                newDatabase.ImplantScrewQcResultDatabase.OrderBy(s => s.ScrewId.GetHashCode()).ToList();

            var expectedResult = Convert.ToBase64String(BsonUtilities.Serialize(database));
            var actualResult = Convert.ToBase64String(BsonUtilities.Serialize(newDatabase));

            // Assert
            Assert.AreEqual(expectedResult, actualResult, "The implant live update handler failed to duplicate data");
        }

        [TestMethod]
        public void ImplantScrewQcLiveUpdateHandler_Update_Test()
        {
            // Arrange
            //      Using Test Library to create case, to know the config of the case
            //      Can refer JSON at IDS_CMFUnitTests/Resources/JsonConfig/Screw/ImplantScrewSerializationTestData.json
            var resource = new TestResources();
            var director = CMFImplantDirectorConverter.ParseHeadlessFromFile(
                resource.ImplantScrewSerializationTestDataFilePath, string.Empty);
            var screwInfoTracker = new ScrewInfoRecordTracker(false);
            var screwQcResults = new Dictionary<Guid, ImmutableList<IScrewQcResult>>();
            var screwQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, screwQcResults);
            screwQcLiveUpdateHandler.GetSerializableData(out var trackerScrewsAtBegin, out var screwQcResultsAtBegin);
            var oldDatabase = ImplantScrewQcDatabaseUtilities.GetImplantScrewQcDatabase(trackerScrewsAtBegin, screwQcResultsAtBegin);
            // Simplify the check
            var screwQcCheckerManager = new ScrewQcCheckerManager(director, new []
            {
                new ImplantScrewVicinityProxyChecker(director),
            });

            // Act
            screwQcLiveUpdateHandler.Update(director, screwQcCheckerManager, out _);
            screwQcLiveUpdateHandler.GetSerializableData(out var trackerScrewsAtEnd, out var screwQcResultsAtEnd);
            var newDatabase = ImplantScrewQcDatabaseUtilities.GetImplantScrewQcDatabase(trackerScrewsAtEnd, screwQcResultsAtEnd);
            var expectedResult = Convert.ToBase64String(BsonUtilities.Serialize(oldDatabase));
            var actualResult = Convert.ToBase64String(BsonUtilities.Serialize(newDatabase));

            // Assert
            Assert.AreEqual(0, oldDatabase.ImplantScrewQcResultDatabase.Count, "It should be empty at beginning");
            Assert.AreEqual(0, oldDatabase.LatestImplantScrewInfoRecords.Count, "It should be empty at beginning");
            Assert.AreEqual(1, newDatabase.ImplantScrewQcResultDatabase.Count, "It should be 1 at the end");
            Assert.AreEqual(1, newDatabase.LatestImplantScrewInfoRecords.Count, "It should be 1 at the end");
            Assert.AreNotEqual(expectedResult, actualResult, "The implant live update handler failed update");
        }

        [TestMethod]
        public void ImplantScrewQcLiveUpdateHandler_Recheck_Test()
        {
            // Arrange
            //      Using Test Library to create case, to know the config of the case
            //      Can refer JSON at IDS_CMFUnitTests/Resources/JsonConfig/Screw/ImplantScrewSerializationTestData.json
            var resource = new TestResources();
            var director = CMFImplantDirectorConverter.ParseHeadlessFromFile(
                resource.ImplantScrewSerializationTestDataFilePath, string.Empty);
            var screwManager = new ScrewManager(director);
            var screw = screwManager.GetAllScrews(false)[0];
            var implantScrewSerializableDataModel = (new ImplantScrewInfoRecord(screw)).GetImplantScrewSerializableDataModel();
            // Create a ScrewInfoRecordTracker with a screw exist
            var screwInfoTracker = new ScrewInfoRecordTracker(new List<ImplantScrewSerializableDataModel>()
            {
                implantScrewSerializableDataModel
            });
            // Create a screw dummy result that expect will be changed later
            var screwQcResults = new Dictionary<Guid, ImmutableList<IScrewQcResult>>()
            {
                {
                    implantScrewSerializableDataModel.Id, 
                    new List<IScrewQcResult>()
                    {
                        new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(),
                        new ImplantScrewVicinityContent()
                        {
                            ScrewsInVicinity = new List<ImplantScrewInfoRecordV2>()
                            {
                                new ImplantScrewInfoRecord(implantScrewSerializableDataModel)
                            }
                        })
                    }.ToImmutableList()
                }
            };
            var screwQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, screwQcResults);
            // Simplify the check
            var screwQcCheckerManager = new ScrewQcCheckerManager(director, new[]
            {
                new ImplantScrewVicinityProxyChecker(director),
            });
            var oldResult = (ImplantScrewVicinitySerializableContent)screwQcResults[screw.Id][0].GetSerializableScrewQcResult();

            // Act
            screwQcLiveUpdateHandler.RecheckCertainResult(screwQcCheckerManager, new List<Screw>()
            {
                screw
            });

            // Assert
            var newResult = (ImplantScrewVicinitySerializableContent)screwQcResults[screw.Id][0].GetSerializableScrewQcResult();
            Assert.AreEqual(1, oldResult.ScrewsInVicinity.Count, "It should be a screw inside old result");
            Assert.AreEqual(screw.Id, oldResult.ScrewsInVicinity[0].Id, "It should be a screw inside old result");
            Assert.AreEqual(0, newResult.ScrewsInVicinity.Count, "It should be no screw inside old result");
        }

        [TestMethod]
        public void BarrelTypeChanged_Recheck_Test()
        {
            // Arrange
            ImplantScrewTestUtilities.CreateScrew(testPoint: new IDSPoint3D(1, 1, 2), Transform.Identity, out var director,true);
            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);
            var implantPreferenceModel = (ImplantPreferenceModel)director.CasePrefManager.CasePreferences[0];
            var curBarrelType = implantPreferenceModel.SelectedBarrelType;

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
                screw.BarrelType = curBarrelType;
            }
            
            var records = screws.Select(screw => 
                (new ImplantScrewInfoRecord(screw)).GetImplantScrewSerializableDataModel());
            // Create a ScrewInfoRecordTracker with a screw exist
            var screwInfoTracker = new ScrewInfoRecordTracker(records);
            // Create a screw simple result that expect will be changed later
            var screwQcResults = screws.ToDictionary(s => s.Id,
                s => new List<IScrewQcResult>()
                {
                    new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(),
                        new BarrelTypeContent()
                        {
                            BarrelType = s.BarrelType
                        })
                }.ToImmutableList());
            director.ImplantScrewQcLiveUpdateHandler = new ScrewQcLiveUpdateHandler(screwInfoTracker, screwQcResults);
            var oldResults = screwQcResults.ToDictionary(kv=> kv.Key, 
                kv=>(BarrelTypeContent)kv.Value[0].GetSerializableScrewQcResult());

            // Act
            implantPreferenceModel.SelectedBarrelType = implantPreferenceModel.BarrelTypes[1];
            var propertyHandler = new PropertyHandler(director);
            propertyHandler.HandleBarrelTypeChanged(implantPreferenceModel);

            // Assert
            var newResults = screwQcResults.ToDictionary(kv => kv.Key,
                kv => (BarrelTypeContent)kv.Value[0].GetSerializableScrewQcResult());
            Assert.AreEqual(oldResults.Count, newResults.Count, "The number of result for old and new should be same");

            foreach (var screw in screws)
            {
                Assert.AreNotEqual(oldResults[screw.Id].BarrelType, newResults[screw.Id].BarrelType, "The barrel type should be change");
            }
        }
    }
}
