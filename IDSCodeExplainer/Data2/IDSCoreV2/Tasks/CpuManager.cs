using System;
using System.Collections.Generic;

namespace IDS.Core.V2.Tasks
{
    public class CpuManager
    {
        private readonly Dictionary<Guid, int> _currentTaskConsumption;

        private readonly object _lock;

        private static CpuManager _instance;

        public static CpuManager Instance => _instance ?? (_instance = new CpuManager());

        // Reserve a core for main thread
        public readonly int MaxLogicalCoreResource = Environment.ProcessorCount - 1;

        private CpuManager()
        {
            _currentTaskConsumption = new Dictionary<Guid, int>();
            _lock = new object();
        }

        private int CurrentConsumption()
        {
            var consumptions = _currentTaskConsumption.Values;
            var totalConsumption = 0;
            foreach (var consumption in consumptions)
            {
                if (consumption < 0)
                {
                    return MaxLogicalCoreResource;
                }

                totalConsumption += consumption;
            }

            return totalConsumption;
        }

        public bool HasCapacity(int cpuConsumption)
        {
            lock (_lock)
            {
                var currentConsumption = CurrentConsumption();
                if (cpuConsumption < 0)
                {
                    // If CPU consumption is -1, then any core available will allow to run,
                    // Once it run, all the new task will not start anymore until this task done
                    // -1 for operation like Wall thickness analysis that optimized with multi threads. 
                    return (currentConsumption < MaxLogicalCoreResource);
                }
                return ((currentConsumption + cpuConsumption) <= MaxLogicalCoreResource);
            }
        }

        public bool Subscribe(Guid id, int cpuCapacity)
        {
            lock (_lock)
            {
                if (_currentTaskConsumption.ContainsKey(id) ||
                    !HasCapacity(cpuCapacity))
                {
                    return false;
                }

                _currentTaskConsumption.Add(id, cpuCapacity);
                return true;
            }
        }

        public bool Unsubscribe(Guid id)
        {
            lock (_lock)
            {
                if (!_currentTaskConsumption.ContainsKey(id))
                {
                    return false;
                }

                _currentTaskConsumption.Remove(id);
                return true;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _currentTaskConsumption.Clear();
            }
        }
    }
}
