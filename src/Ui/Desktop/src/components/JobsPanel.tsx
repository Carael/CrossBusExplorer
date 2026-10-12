import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, CheckCircle2, CircleDashed, X, XCircle } from "lucide-react";
import { api } from "../api";

export function JobsPanel({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const jobs = useQuery({
    queryKey: ["jobs"],
    queryFn: api.jobs,
    enabled: open,
    refetchInterval: ({ state }) =>
      state.data?.some((job) => job.status === "Running" || job.status === "Queued") ? 750 : false,
  });
  const cancel = useMutation({
    mutationFn: api.cancelJob,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["jobs"] }),
  });

  if (!open) return null;
  return (
    <aside className="jobs-panel" aria-label="Background operations">
      <header><div><span className="eyebrow">Activity</span><h2>Operations</h2></div><button className="icon-button" onClick={onClose} aria-label="Close operations"><X size={18} /></button></header>
      <div className="job-list">
        {jobs.data?.length === 0 && <div className="empty-jobs"><CircleDashed size={28} /><p>No background operations yet.</p></div>}
        {jobs.data?.map((job) => (
          <article className="job-card" key={job.id}>
            <div className="job-title"><JobIcon status={job.status} /><strong>{job.name}</strong></div>
            <div className="progress-track"><span style={{ width: `${job.progress}%` }} /></div>
            <div className="job-meta"><span>{job.status} · {job.progress}%</span>{(job.status === "Queued" || job.status === "Running") && <button onClick={() => cancel.mutate(job.id)}><Ban size={13} /> Cancel</button>}</div>
            {job.message && <p>{job.message}</p>}
          </article>
        ))}
      </div>
    </aside>
  );
}

function JobIcon({ status }: { status: string }) {
  if (status === "Succeeded") return <CheckCircle2 size={17} className="success-icon" />;
  if (status === "Failed") return <XCircle size={17} className="error-icon" />;
  return <CircleDashed size={17} className={status === "Running" ? "spinning" : ""} />;
}
