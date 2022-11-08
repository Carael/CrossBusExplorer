using System.ComponentModel;
using System.Threading.Tasks;
namespace CrossBusExplorer.Website.Jobs;

public interface IJob : INotifyPropertyChanged
{
    int Progress { get; }
    Task ExecuteAsync();
    void Cancel();
    string Name { get; }
    bool ViewDetails { get; set; }
    JobStatus Status { get; }
    string? ErrorMessage { get; }
}