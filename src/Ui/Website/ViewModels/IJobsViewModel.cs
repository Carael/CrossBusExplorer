using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CrossBusExplorer.Website.Jobs;
namespace CrossBusExplorer.Website.ViewModels;

public interface IJobsViewModel : INotifyPropertyChanged
{
    ObservableCollection<IJob> Jobs { get; }
    Task ScheduleJob(IJob job);
    void CancelJob(IJob job);
}