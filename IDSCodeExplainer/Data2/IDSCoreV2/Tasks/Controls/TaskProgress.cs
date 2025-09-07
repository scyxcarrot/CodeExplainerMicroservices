using IDS.Interface.Tasks;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.Core.V2.Tasks
{
    public class TaskProgress: ITaskProgress
    {
        private double _progressValue;

        public double Progress
        {
            get
            {
                lock (this)
                {
                    return _progressValue;
                }
            }
            set
            {
                lock (this)
                {
                    var constraintValue = ConstrainProgress(value);
                    if (Math.Abs(_progressValue - constraintValue) < 0.001)
                    {
                        return;
                    }

                    _progressValue = constraintValue;
                }
                OnPropertyChanged(nameof(Progress));
            }
        }

        private double ConstrainProgress(double progress)
        {
            if (progress >= 100)
            {
                return 100;
            }

            if (progress < 0)
            {
                return 0;
            }

            return progress;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
