using System.ComponentModel;

namespace IDS.Interface.Tasks
{
    public interface ITaskProgress: INotifyPropertyChanged
    {
        double Progress { get; }
    }
}
