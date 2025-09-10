using IDS.Core.V2.Common;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Helper
{
    public class CircularDependencyDetector<TType> : IDisposable where TType : struct
    {
        private readonly List<TType> _parentsId;
        private readonly TType _currentGuid;

        public CircularDependencyDetector(List<TType> parentsId, TType currentId)
        {
            _parentsId = parentsId ?? throw new IDSExceptionV2(
                "Parameters 'parentsId' of \"CircularDependencyDetector\" shouldn't be null");
            _currentGuid = currentId;
            _parentsId.Add(currentId);
        }

        public bool HasCircularDependency()
        {
            var foundCount = 0;
            return _parentsId.Exists(p => p.Equals(_currentGuid) && foundCount++ > 0);
        }

        public void Dispose()
        {
            _parentsId.Remove(_currentGuid);
        }
    }
}
