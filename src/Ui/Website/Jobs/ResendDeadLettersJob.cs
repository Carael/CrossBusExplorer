using System.ComponentModel;
using System.Threading.Tasks;
namespace CrossBusExplorer.Website.Jobs;

public class ResendDeadLettersJob : IJob
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public int Progress { get; }
    public Task StartAsync()
    {
        throw new System.NotImplementedException();
    }
    public void Cancel()
    {
        throw new System.NotImplementedException();
    }
    public string Name { get; set; }
}