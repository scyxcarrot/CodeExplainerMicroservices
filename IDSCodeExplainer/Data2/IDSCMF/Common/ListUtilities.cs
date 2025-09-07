using System.Collections.Generic;

namespace IDS.CMF.Common
{
    public static class ListUtilities
    {
        public static List<List<T>> SplitListEvenly<T>(List<T> listToSplit, int splitTimes)
        {
            var outputList = new List<List<T>>();
            for (var splitIndex = 0; splitIndex < splitTimes; splitIndex++)
            {
                outputList.Add(new List<T>());
            }

            for (var listIndex = 0; listIndex < listToSplit.Count; listIndex++)
            {
                for (var splitIndex = 0; splitIndex < splitTimes; splitIndex++)
                {
                    if (listIndex % splitTimes == splitIndex)
                    {
                        outputList[splitIndex].Add(listToSplit[listIndex]);
                        break;
                    }
                }
            }

            return outputList;
        }
    }
}
