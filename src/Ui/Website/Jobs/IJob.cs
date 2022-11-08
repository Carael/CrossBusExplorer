using System.ComponentModel;
using System.Threading.Tasks;
namespace CrossBusExplorer.Website.Jobs;

public interface IJob : INotifyPropertyChanged
{
    int Progress { get; }
    Task StartAsync();
    void Cancel();
    string Name { get; }
    bool ViewDetails { get; set; }
}