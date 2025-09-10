using IDS.Core.V2.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace IDS.PICMF.Forms.BackgroundProcess
{
    public class BackgroundProcessPanelViewModel : INotifyPropertyChanged
    {
        private const int MaxCompletedTasksCount = 10;

        private readonly DispatcherTimer _dispatcherTimer;

        public TaskQueue Model { get; } 

        private readonly List<BackgroundProcessViewModel> _activeTasks;

        private readonly List<BackgroundProcessViewModel> _completedTasks;

        public ObservableCollection<BackgroundProcessViewModel> ActiveTasks => new ObservableCollection<BackgroundProcessViewModel>(_activeTasks);

        public ObservableCollection<BackgroundProcessViewModel> CompletedTasks => new ObservableCollection<BackgroundProcessViewModel>(_completedTasks);

        public BackgroundProcessPanelViewModel()
        {
            _activeTasks = new List<BackgroundProcessViewModel>();
            _completedTasks = new List<BackgroundProcessViewModel>();
            Model = new TaskQueue();
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);

            _dispatcherTimer.Tick += TaskQueueSecondUpdate;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            _dispatcherTimer.Start();
        }

        private void TaskQueueSecondUpdate(object sender, EventArgs e)
        {
            Model.Update();
            for (var i = _activeTasks.Count - 1; i >= 0; i--)
            {
                var activeTaskViewModel = _activeTasks[i];
                var activeTaskModel = activeTaskViewModel.Model;
                activeTaskModel.Update();

                if (activeTaskModel.IsCompleted())
                {
                    while (_completedTasks.Count >= MaxCompletedTasksCount)
                    {
                        _completedTasks.RemoveAt(0);
                    }

                    _completedTasks.Add(activeTaskViewModel);
                    _activeTasks.RemoveAt(i);

                    OnPropertyChanged(nameof(ActiveTasks));
                    OnPropertyChanged(nameof(CompletedTasks));
                }
            }
        }

        public void AddActiveTask(BackgroundProcessViewModel activeTaskViewModel)
        {
            Model.Add(activeTaskViewModel.Model);
            _activeTasks.Add(activeTaskViewModel);
            OnPropertyChanged(nameof(ActiveTasks));
        }

        public void ClearCompletedTasks()
        {
            _completedTasks.Clear();
            OnPropertyChanged(nameof(CompletedTasks));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
