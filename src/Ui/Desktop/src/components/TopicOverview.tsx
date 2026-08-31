import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Clock3, Copy, FolderTree, Layers3, RefreshCw, Save, Trash2 } from "lucide-react";
import { api } from "../api";
import type { TopicDetails } from "../types";
import { Capability, Metric } from "./QueueOverview";

export function TopicOverview({
  connectionName,
  topicName,
  onDeleted,
  onSelectSubscription,
}: {
  connectionName: string;
  topicName: string;
  onDeleted: () => void;
  onSelectSubscription: (name: string) => void;
}) {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"overview" | "subscriptions" | "settings">("overview");
  const topic = useQuery({ queryKey: ["topic", connectionName, topicName], queryFn: () => api.topic(connectionName, topicName) });
  const subscriptions = useQuery({ queryKey: ["subscriptions", connectionName, topicName], queryFn: () => api.subscriptions(connectionName, topicName) });
  const clone = useMutation({
    mutationFn: (name: string) => api.cloneTopic(connectionName, topicName, name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["topics", connectionName] }),
  });
  const remove = useMutation({
    mutationFn: () => api.deleteTopic(connectionName, topicName),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["topics", connectionName] });
      onDeleted();
    },
  });
  const createSubscription = useMutation({
    mutationFn: (subscriptionName: string) => api.createSubscription(connectionName, topicName, { topicName, subscriptionName }),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions", connectionName, topicName] });
      if (result.data) onSelectSubscription(result.data.info.subscriptionName);
    },
  });

  if (topic.isPending) return <TopicState title="Loading topic" detail="Reading settings and runtime metrics…" />;
  if (topic.error) return <TopicState title="Unable to load topic" detail={topic.error.message} error />;

  const details = topic.data;
  const error = clone.error ?? remove.error ?? createSubscription.error;

  function promptClone() {
    const name = window.prompt("Name for the cloned topic", `${topicName}-copy`)?.trim();
    if (name) clone.mutate(name);
  }
  function promptSubscription() {
    const name = window.prompt("New subscription name")?.trim();
    if (name) createSubscription.mutate(name);
  }

  return (
    <div className="content-page wide-page">
      <header className="page-header">
        <div><div className="breadcrumbs">{connectionName} / Topics / {topicName}</div><div className="title-line"><div className="resource-icon topic"><FolderTree size={22} /></div><div><div className="title-with-status"><h1>{topicName}</h1><span className={`status ${details.info.status.toLowerCase()}`}>{details.info.status}</span></div><p>Topic configuration and subscriptions</p></div></div></div>
        <div className="header-actions">
          <button className="button secondary" onClick={promptClone} disabled={clone.isPending}><Copy size={15} /> Clone</button>
          <button className="button secondary" onClick={() => { topic.refetch(); subscriptions.refetch(); }}><RefreshCw size={15} /> Refresh</button>
          <button className="button danger-ghost" onClick={() => window.confirm(`Delete topic ${topicName} and all its subscriptions?`) && remove.mutate()}><Trash2 size={15} /> Delete</button>
        </div>
      </header>
      {error && <div className="notice error">{error.message}</div>}

      <div className="metric-grid compact-metrics">
        <Metric label="Subscriptions" value={subscriptions.data?.length ?? 0} icon={<Layers3 />} tone="accent" />
        <Metric label="Scheduled" value={details.info.scheduledMessagesCount} icon={<Clock3 />} tone="neutral" />
        <Metric label="Size in MB" value={details.info.sizeInMB} icon={<FolderTree />} tone="neutral" />
      </div>

      <div className="tab-strip" role="tablist">
        <TopicTab active={tab === "overview"} onClick={() => setTab("overview")}>Overview</TopicTab>
        <TopicTab active={tab === "subscriptions"} onClick={() => setTab("subscriptions")}>Subscriptions <span>{subscriptions.data?.length ?? 0}</span></TopicTab>
        <TopicTab active={tab === "settings"} onClick={() => setTab("settings")}>Settings</TopicTab>
      </div>

      {tab === "overview" && <TopicSummary details={details} onEdit={() => setTab("settings")} />}
      {tab === "subscriptions" && (
        <section className="card table-card">
          <div className="card-heading"><div><span className="eyebrow">Subscriptions</span><h2>Consumers</h2></div><button className="button primary" onClick={promptSubscription}>Add subscription</button></div>
          {subscriptions.error ? <div className="notice error">{subscriptions.error.message}</div> : (
            <table className="data-table"><thead><tr><th>Name</th><th>Status</th><th>Active</th><th>Dead letter</th><th>Last accessed</th></tr></thead><tbody>
              {subscriptions.data?.map((item) => <tr key={item.subscriptionName} onDoubleClick={() => onSelectSubscription(item.subscriptionName)}><td><button className="table-link" onClick={() => onSelectSubscription(item.subscriptionName)}>{item.subscriptionName}</button></td><td><span className={`status ${item.status.toLowerCase()}`}>{item.status}</span></td><td>{item.activeMessagesCount.toLocaleString()}</td><td>{item.deadLetterMessagesCount.toLocaleString()}</td><td>{new Date(item.accessedAt).toLocaleString()}</td></tr>)}
            </tbody></table>
          )}
        </section>
      )}
      {tab === "settings" && <TopicSettings connectionName={connectionName} topicName={topicName} details={details} />}
    </div>
  );
}

function TopicSummary({ details, onEdit }: { details: TopicDetails; onEdit: () => void }) {
  return <div className="overview-grid"><section className="card"><div className="card-heading"><div><span className="eyebrow">Configuration</span><h2>Capacity and retention</h2></div><button className="button tertiary" onClick={onEdit}>Edit settings</button></div><dl className="definition-list compact"><div><dt>Maximum size</dt><dd>{details.properties.maxQueueSizeInMegabytes.toLocaleString()} MB</dd></div><div><dt>Maximum message</dt><dd>{details.properties.maxMessageSizeInKilobytes?.toLocaleString() ?? "Default"} KB</dd></div><div><dt>Default message TTL</dt><dd>{details.timeSettings.defaultMessageTimeToLive}</dd></div><div><dt>Auto-delete on idle</dt><dd>{details.timeSettings.autoDeleteOnIdle}</dd></div><div><dt>Duplicate detection window</dt><dd>{details.timeSettings.duplicateDetectionHistoryTimeWindow}</dd></div></dl></section><section className="card"><div className="card-heading"><div><span className="eyebrow">Behavior</span><h2>Capabilities</h2></div></div><div className="capability-list"><Capability label="Batched operations" enabled={details.settings.enableBatchedOperations} /><Capability label="Partitioning" enabled={details.settings.enablePartitioning} /><Capability label="Duplicate detection" enabled={details.settings.requiresDuplicateDetection} /><Capability label="Ordering" enabled={details.settings.supportOrdering} /></div></section></div>;
}

function TopicSettings({ connectionName, topicName, details }: { connectionName: string; topicName: string; details: TopicDetails }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => ({ name: topicName, maxSizeInMegabytes: details.properties.maxQueueSizeInMegabytes, maxMessageSizeInKilobytes: details.properties.maxMessageSizeInKilobytes, defaultMessageTimeToLive: details.timeSettings.defaultMessageTimeToLive, autoDeleteOnIdle: details.timeSettings.autoDeleteOnIdle, duplicateDetectionHistoryTimeWindow: details.timeSettings.duplicateDetectionHistoryTimeWindow, enableBatchedOperations: details.settings.enableBatchedOperations, supportOrdering: details.settings.supportOrdering, status: details.info.status, userMetadata: details.properties.userMetadata ?? "" }));
  const save = useMutation({ mutationFn: () => api.updateTopic(connectionName, topicName, clean(form)), onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["topic", connectionName, topicName] }); queryClient.invalidateQueries({ queryKey: ["topics", connectionName] }); } });
  return <form className="card settings-card" onSubmit={(event) => { event.preventDefault(); save.mutate(); }}><div className="card-heading"><div><span className="eyebrow">Topic</span><h2>Editable settings</h2></div><button className="button primary" disabled={save.isPending}><Save size={15} /> {save.isPending ? "Saving…" : "Save changes"}</button></div><div className="form-grid entity-form"><Field label="Maximum size (MB)" type="number" value={form.maxSizeInMegabytes} onChange={(value) => setForm({ ...form, maxSizeInMegabytes: Number(value) })} /><Field label="Maximum message size (KB)" type="number" value={form.maxMessageSizeInKilobytes ?? 0} onChange={(value) => setForm({ ...form, maxMessageSizeInKilobytes: Number(value) || undefined })} /><Field label="Default message TTL" value={form.defaultMessageTimeToLive} onChange={(value) => setForm({ ...form, defaultMessageTimeToLive: value })} /><Field label="Auto-delete on idle" value={form.autoDeleteOnIdle} onChange={(value) => setForm({ ...form, autoDeleteOnIdle: value })} /><Field label="Duplicate detection window" value={form.duplicateDetectionHistoryTimeWindow} onChange={(value) => setForm({ ...form, duplicateDetectionHistoryTimeWindow: value })} /><label><span>Status</span><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option>Active</option><option>Disabled</option><option>SendDisabled</option><option>ReceiveDisabled</option></select></label><label className="wide"><span>User metadata</span><textarea rows={4} value={form.userMetadata} onChange={(event) => setForm({ ...form, userMetadata: event.target.value })} /></label><div className="wide toggle-grid"><Toggle label="Batched operations" checked={form.enableBatchedOperations} onChange={(value) => setForm({ ...form, enableBatchedOperations: value })} /><Toggle label="Support ordering" checked={form.supportOrdering} onChange={(value) => setForm({ ...form, supportOrdering: value })} /></div></div>{save.isSuccess && <div className="notice success">Topic settings saved.</div>}{save.error && <div className="notice error">{save.error.message}</div>}</form>;
}

function TopicTab({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) { return <button className={`tab ${active ? "active" : ""}`} onClick={onClick}>{children}</button>; }
function Field({ label, type = "text", value, onChange }: { label: string; type?: string; value: string | number; onChange: (value: string) => void }) { return <label><span>{label}</span><input type={type} value={value} onChange={(event) => onChange(event.target.value)} /></label>; }
function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) { return <label className="toggle"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /><span>{label}</span></label>; }
function clean<T extends Record<string, unknown>>(value: T): T { return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, item === "" ? null : item])) as T; }
function TopicState({ title, detail, error = false }: { title: string; detail: string; error?: boolean }) { return <div className={`page-state ${error ? "error" : ""}`}><RefreshCw size={25} className={error ? "" : "spinning"} /><h2>{title}</h2><p>{detail}</p></div>; }
