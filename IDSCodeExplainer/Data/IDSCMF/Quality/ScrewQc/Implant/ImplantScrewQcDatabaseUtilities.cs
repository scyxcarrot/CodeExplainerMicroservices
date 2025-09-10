using IDS.CMF.DataModel;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.PluginHelper;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.CMF.ScrewQc
{
    public static class ImplantScrewQcDatabaseUtilities
    {
        private static IndividualImplantScrewQcResultDatabase GetIndividualImplantScrewQcResultDatabase(Guid screwId, ImmutableDictionary<string, object> results)
        {
            var implantScrewQcResultDatabase = new IndividualImplantScrewQcResultDatabase
            {
                ScrewId = screwId
            };

            foreach (var result in results)
            {
                var checkerName = result.Key;
                var serializableResult = result.Value;
                if (Enum.TryParse<ImplantScrewQcCheck>(checkerName, out var checkerEnum))
                {
                    switch (checkerEnum)
                    {
                        case ImplantScrewQcCheck.SkipOstDistAndIntersect:
                            implantScrewQcResultDatabase.SkipOstDistAndIntersect =
                                (SkipOstDistAndIntersectContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.MinMaxDistance:
                            implantScrewQcResultDatabase.MinMaxDistance =
                                (MinMaxDistanceSerializableContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle:
                            implantScrewQcResultDatabase.AnatomicalObstacle =
                                (ImplantScrewAnatomicalObstacleContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.OsteotomyDistance:
                            implantScrewQcResultDatabase.OsteotomyDistance =
                                (OsteotomyDistanceSerializableContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.OsteotomyIntersection:
                            implantScrewQcResultDatabase.OsteotomyIntersection =
                                (OsteotomyIntersectionContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.ImplantScrewVicinity:
                            implantScrewQcResultDatabase.VicinityResult =
                                (ImplantScrewVicinitySerializableContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.PastilleDeformed:
                            implantScrewQcResultDatabase.PastilleDeformed =
                                (PastilleDeformedContent)serializableResult;
                            break;
                        case ImplantScrewQcCheck.BarrelType:
                            implantScrewQcResultDatabase.BarrelType =
                                (BarrelTypeContent)serializableResult;
                            break;
                        default:
                            throw new IDSException($"'{checkerName}' is not handled");
                    }
                }
                else
                {
                    throw new IDSException($"'{checkerName}' is not a enum of 'ImplantScrewQcCheck'");
                }
            }

            return implantScrewQcResultDatabase;
        }

        public static ImplantScrewQcDatabase GetImplantScrewQcDatabaseFromDirector(CMFImplantDirector director)
        {
            return director.ImplantScrewQcLiveUpdateHandler == null ? 
                null : 
                GetImplantScrewQcDatabase(director.ImplantScrewQcLiveUpdateHandler);
        }

        public static ImplantScrewQcDatabase GetImplantScrewQcDatabase(ScrewQcLiveUpdateHandler implantScrewQcLiveUpdateHandler)
        {
            implantScrewQcLiveUpdateHandler.GetSerializableData(out var latestScrewSerializableDataModels,
                out var latestScrewSerializableQcResult);

            return GetImplantScrewQcDatabase(latestScrewSerializableDataModels, latestScrewSerializableQcResult);
        }

        public static ImplantScrewQcDatabase GetImplantScrewQcDatabase(ImmutableList<CommonScrewSerializableDataModel> latestScrewSerializableDataModels,
            ImmutableDictionary<Guid, ImmutableDictionary<string, object>> latestScrewSerializableQcResult)
        {
            var implantScrewQcDatabase = new ImplantScrewQcDatabase
            {
                LatestImplantScrewInfoRecords = latestScrewSerializableDataModels
                    .Cast<ImplantScrewSerializableDataModel>().ToList(),
                ImplantScrewQcResultDatabase = latestScrewSerializableQcResult
                    .Select(r => GetIndividualImplantScrewQcResultDatabase(r.Key, r.Value)).ToList()
            };

            return implantScrewQcDatabase;
        }

        private static ImmutableList<IScrewQcResult> GetIndividualScrewResultsFromDatabaseInOrder(IEnumerable<ImplantScrewQcCheck> order, 
            IndividualImplantScrewQcResultDatabase database)
        {
            var screwQcResults = new List<IScrewQcResult>();

            foreach (var checkerType in order)
            {
                switch (checkerType)
                {
                    case ImplantScrewQcCheck.SkipOstDistAndIntersect:
                        screwQcResults.Add(new SkipOstDistAndIntersectResult(ImplantScrewQcCheck.SkipOstDistAndIntersect.ToString(),
                            new SkipOstDistAndIntersectContent(database.SkipOstDistAndIntersect)));
                        break;
                    case ImplantScrewQcCheck.MinMaxDistance:
                        screwQcResults.Add(new MinMaxDistanceResult(ImplantScrewQcCheck.MinMaxDistance.ToString(),
                            new MinMaxDistanceContent(database.MinMaxDistance)));
                        break;
                    case ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle:
                        screwQcResults.Add(new ImplantScrewAnatomicalObstacleResult(ImplantScrewQcCheck.ImplantScrewAnatomicalObstacle.ToString(),
                            new ImplantScrewAnatomicalObstacleContent(database.AnatomicalObstacle)));
                        break;
                    case ImplantScrewQcCheck.OsteotomyDistance:
                        screwQcResults.Add(new OsteotomyDistanceResult(ImplantScrewQcCheck.OsteotomyDistance.ToString(),
                            new OsteotomyDistanceContent(database.OsteotomyDistance)));
                        break;
                    case ImplantScrewQcCheck.OsteotomyIntersection:
                        screwQcResults.Add(new OsteotomyIntersectionResult(ImplantScrewQcCheck.OsteotomyIntersection.ToString(),
                            new OsteotomyIntersectionContent(database.OsteotomyIntersection)));
                        break;
                    case ImplantScrewQcCheck.ImplantScrewVicinity:
                        screwQcResults.Add(new ImplantScrewVicinityResult(ImplantScrewQcCheck.ImplantScrewVicinity.ToString(),
                            new ImplantScrewVicinityContent(database.VicinityResult)));
                        break;
                    case ImplantScrewQcCheck.PastilleDeformed:
                        screwQcResults.Add(new PastilleDeformedResult(ImplantScrewQcCheck.PastilleDeformed.ToString(),
                            new PastilleDeformedContent(database.PastilleDeformed)));
                        break;
                    case ImplantScrewQcCheck.BarrelType:
                        screwQcResults.Add(new BarrelTypeResult(ImplantScrewQcCheck.BarrelType.ToString(),
                            new BarrelTypeContent(database.BarrelType)));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return screwQcResults.ToImmutableList();
        }

        private static ImmutableList<IScrewQcResult> GetIndividualScrewResults(IndividualImplantScrewQcResultDatabase database)
        {
            return GetIndividualScrewResultsFromDatabaseInOrder(ImplantScrewQcUtilities.OrderOfResults, database);
        }

        public static ScrewQcLiveUpdateHandler GetImplantScrewLiveUpdateHandler(ImplantScrewQcDatabase screwQcDatabase)
        {
            var implantScrewTracker = new ScrewInfoRecordTracker(screwQcDatabase.LatestImplantScrewInfoRecords);
            var screwQcResults = screwQcDatabase.ImplantScrewQcResultDatabase
                .ToDictionary(s => s.ScrewId, GetIndividualScrewResults);

            return new ScrewQcLiveUpdateHandler(implantScrewTracker, screwQcResults);
        }

        
    }
}
