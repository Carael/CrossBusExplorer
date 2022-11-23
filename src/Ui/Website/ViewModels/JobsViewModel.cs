using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Jobs;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class JobsViewModel : IJobsViewModel
{
    private readonly ISnackbar _snackbar;
    public event PropertyChangedEventHandler? PropertyChanged;

    public JobsViewModel(ISnackbar snackbar)
    {
        _snackbar = snackbar;
        Jobs = new ObservableCollection<IJob>();
    }

    private ObservableCollection<IJob> _jobs;
    public ObservableCollection<IJob> Jobs
    {
        get => _jobs;
        set
        {
            _jobs = value;
            _jobs.CollectionChanged += (_, _) => this.Notify(PropertyChanged);
        }
    }
    public async Task ScheduleJob(IJob job)
    {
        job.PropertyChanged += (_, _) => HandleJobUpdate(job);
        Jobs.Add(job);
        await job.ExecuteAsync();
    }
    
    private void HandleJobUpdate(IJob job)
    {
        if (job.Status != JobStatus.Running)
        {
            job.PropertyChanged -= (_, _) => HandleJobUpdate(job);
            _jobs.Remove(job);

            if (job.Status == JobStatus.Succeeded)
            {
                _snackbar.Add($"Job {job.Name} completed.", Severity.Success);
            }
            else if (job.Status == JobStatus.Failed)
            {
                _snackbar.Add(
                    job.ErrorMessage ?? $"Error while executing job {job.Name}",
                    Severity.Error);
            }
            else if (job.Status == JobStatus.Cancelled)
            {
                _snackbar.Add($"Job {job.Name} was cancelled.", Severity.Warning);
            }
        }

        this.Notify(PropertyChanged);
    }
    public void CancelJob(IJob job)
    {
        job.Cancel();
    }
}