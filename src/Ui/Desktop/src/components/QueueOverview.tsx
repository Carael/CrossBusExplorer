import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowRightLeft,
  Clock3,
  Copy,
  Database,
  MessageSquareText,
  RefreshCw,
  Save,
  ShieldAlert,
  Trash2,
} from "lucide-react";
import { api } from "../api";
import type { QueueDetails } from "../types";
import { MessagesWorkspace } from "./MessagesWorkspace";

export function QueueOverview({
  connectionName,
  queueName,
  onDeleted,
  onJobStarted,
}: {
  connectionName: string;
  queueName: string;
  onDeleted: () => void;
  onJobStarted: () => void;
}) {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"overview" | "messages" | "settings">("overview");
  const queue = useQuery({
    queryKey: ["queue", connectionName, queueName],
    queryFn: () => api.queue(connectionName, queueName),
  });
  const clone = useMutation({
    mutationFn: (name: string) => api.cloneQueue(connectionName, queueName, name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["queues", connectionName] }),
  });
  const remove = useMutation({
    mutationFn: () => api.deleteQueue(connectionName, queueName),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["queues", connectionName] });
      onDeleted();
    },
  });

  if (queue.isPending) return <PageState title="Loading queue" detail="Reading settings and runtime metrics…" />;
  if (queue.error) return <PageState title="Unable to load queue" detail={queue.error.message} error />;

  const details = queue.data;
  const info = details.info;
  const mutationError = clone.error ?? remove.error;

  function cloneQueue() {
    const name = window.prompt("Name for the cloned queue", `${queueName}-copy`)?.trim();
    if (name) clone.mutate(name);
  }

  function deleteQueue() {
    if (window.confirm(`Delete queue ${queueName}? This cannot be undone.`)) remove.mutate();
  }

  return (
    <div className="content-page wide-page">
      <header className="page-header">
        <div>
          <div className="breadcrumbs">{connectionName} / Queues / {queueName}</div>
          <div className="title-line">
            <div className="resource-icon queue"><Database size={22} /></div>
            <div><div className="title-with-status"><h1>{queueName}</h1><span className={`status ${info.status.toLowerCase()}`}>{info.status}</span></div><p>Queue configuration, runtime health, and messages</p></div>
          </div>
        </div>
        <div className="header-actions">
          <button className="button secondary" onClick={cloneQueue} disabled={clone.isPending}><Copy size={16} /> Clone</button>
          <button className="button secondary" onClick={() => queue.refetch()}><RefreshCw size={16} /> Refresh</button>
          <button className="button danger-ghost" onClick={deleteQueue} disabled={remove.isPending}><Trash2 size={16} /> Delete</button>
        </div>
      </header>

      {mutationError && <div className="notice error">{mutationError.message}</div>}
      {clone.isSuccess && <div className="notice success">Queue cloned successfully.</div>}

      <div className="metric-grid">
        <Metric label="Active" value={info.activeMessagesCount} icon={<MessageSquareText />} tone="accent" />
        <Metric label="Dead letter" value={info.deadLetterMessagesCount} icon={<ShieldAlert />} tone={info.deadLetterMessagesCount ? "danger" : "neutral"} />
        <Metric label="Scheduled" value={info.scheduledMessagesCount} icon={<Clock3 />} tone="neutral" />
        <Metric label="In transfer" value={info.inTransferMessagesCount} icon={<ArrowRightLeft />} tone="neutral" />
      </div>

      <div className="tab-strip" role="tablist">
        <Tab active={tab === "overview"} onClick={() => setTab("overview")}>Overview</Tab>
        <Tab active={tab === "messages"} onClick={() => setTab("messages")}>Messages <span>{info.totalMessagesCount}</span></Tab>
        <Tab active={tab === "settings"} onClick={() => setTab("settings")}>Settings</Tab>
      </div>

      {tab === "overview" && <QueueSummary details={details} onEdit={() => setTab("settings")} />}
      {tab === "messages" && (
        <MessagesWorkspace
          connectionName={connectionName}
          entityName={queueName}
          activeCount={info.activeMessagesCount}
          deadLetterCount={info.deadLetterMessagesCount}
          transferDeadLetterCount={info.transferDeadLetterMessagesCount}
          onJobStarted={onJobStarted}
        />
      )}
      {tab === "settings" && <QueueSettings connectionName={connectionName} queueName={queueName} details={details} />}
    </div>
  );
}

function QueueSummary({ details, onEdit }: { details: QueueDetails; onEdit: () => void }) {
  return (
    <div className="overview-grid queue-overview">
      <section className="card">
        <div className="card-heading"><div><span className="eyebrow">Configuration</span><h2>Delivery and capacity</h2></div><button className="button tertiary" onClick={onEdit}>Edit settings</button></div>
        <dl className="definition-list compact">
          <div><dt>Max delivery count</dt><dd>{details.properties.maxDeliveryCount}</dd></div>
          <div><dt>Queue size</dt><dd>{details.properties.maxQueueSizeInMegabytes.toLocaleString()} MB</dd></div>
          <div><dt>Max message size</dt><dd>{details.properties.maxMessageSizeInKilobytes?.toLocaleString() ?? "Default"} KB</dd></div>
          <div><dt>Lock duration</dt><dd>{details.timeSettings.lockDuration}</dd></div>
          <div><dt>Message time to live</dt><dd>{details.timeSettings.defaultMessageTimeToLive}</dd></div>
          <div><dt>Auto-delete on idle</dt><dd>{details.timeSettings.autoDeleteOnIdle}</dd></div>
          <div><dt>Forward to</dt><dd>{details.properties.forwardTo || "—"}</dd></div>
          <div><dt>Dead letters forward to</dt><dd>{details.properties.forwardDeadLetteredMessagesTo || "—"}</dd></div>
        </dl>
      </section>
      <section className="card">
        <div className="card-heading"><div><span className="eyebrow">Behavior</span><h2>Capabilities</h2></div></div>
        <div className="capability-list">
          <Capability label="Sessions" enabled={details.settings.requiresSession} />
          <Capability label="Duplicate detection" enabled={details.settings.requiresDuplicateDetection} />
          <Capability label="Dead-letter on expiration" enabled={details.settings.enableDeadLetteringOnMessageExpiration} />
          <Capability label="Batched operations" enabled={details.settings.enableBatchedOperations} />
          <Capability label="Partitioning" enabled={details.settings.enablePartitioning} />
        </div>
      </section>
    </div>
  );
}

function QueueSettings({ connectionName, queueName, details }: { connectionName: string; queueName: string; details: QueueDetails }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => ({
    name: queueName,
    maxSizeInMegabytes: details.properties.maxQueueSizeInMegabytes,
    maxMessageSizeInKilobytes: details.properties.maxMessageSizeInKilobytes,
    maxDeliveryCount: details.properties.maxDeliveryCount,
    lockDuration: details.timeSettings.lockDuration,
    defaultMessageTimeToLive: details.timeSettings.defaultMessageTimeToLive,
    autoDeleteOnIdle: details.timeSettings.autoDeleteOnIdle,
    duplicateDetectionHistoryTimeWindow: details.timeSettings.duplicateDetectionHistoryTimeWindow,
    enableBatchedOperations: details.settings.enableBatchedOperations,
    deadLetteringOnMessageExpiration: details.settings.enableDeadLetteringOnMessageExpiration,
    requiresSession: details.settings.requiresSession,
    status: details.info.status,
    forwardTo: details.properties.forwardTo ?? "",
    forwardDeadLetteredMessagesTo: details.properties.forwardDeadLetteredMessagesTo ?? "",
    userMetadata: details.properties.userMetadata ?? "",
  }));
  const save = useMutation({
    mutationFn: () => api.updateQueue(connectionName, queueName, cleanOptions(form)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["queue", connectionName, queueName] });
      queryClient.invalidateQueries({ queryKey: ["queues", connectionName] });
    },
  });

  return (
    <form className="card settings-card" onSubmit={(event) => { event.preventDefault(); save.mutate(); }}>
      <div className="card-heading"><div><span className="eyebrow">Queue</span><h2>Editable settings</h2></div><button className="button primary" disabled={save.isPending}><Save size={15} /> {save.isPending ? "Saving…" : "Save changes"}</button></div>
      <div className="form-grid entity-form">
        <NumberField label="Max size (MB)" value={form.maxSizeInMegabytes} onChange={(value) => setForm({ ...form, maxSizeInMegabytes: value })} />
        <NumberField label="Max message size (KB)" value={form.maxMessageSizeInKilobytes ?? 0} onChange={(value) => setForm({ ...form, maxMessageSizeInKilobytes: value || undefined })} />
        <NumberField label="Max delivery count" value={form.maxDeliveryCount} onChange={(value) => setForm({ ...form, maxDeliveryCount: value })} />
        <TextField label="Lock duration" value={form.lockDuration} onChange={(value) => setForm({ ...form, lockDuration: value })} />
        <TextField label="Default message TTL" value={form.defaultMessageTimeToLive} onChange={(value) => setForm({ ...form, defaultMessageTimeToLive: value })} />
        <TextField label="Auto-delete on idle" value={form.autoDeleteOnIdle} onChange={(value) => setForm({ ...form, autoDeleteOnIdle: value })} />
        <TextField label="Duplicate detection window" value={form.duplicateDetectionHistoryTimeWindow} onChange={(value) => setForm({ ...form, duplicateDetectionHistoryTimeWindow: value })} />
        <label><span>Status</span><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option>Active</option><option>Disabled</option><option>SendDisabled</option><option>ReceiveDisabled</option></select></label>
        <TextField label="Forward to" value={form.forwardTo} onChange={(value) => setForm({ ...form, forwardTo: value })} />
        <TextField label="Forward dead letters to" value={form.forwardDeadLetteredMessagesTo} onChange={(value) => setForm({ ...form, forwardDeadLetteredMessagesTo: value })} />
        <label className="wide"><span>User metadata</span><textarea rows={4} value={form.userMetadata} onChange={(event) => setForm({ ...form, userMetadata: event.target.value })} /></label>
        <div className="wide toggle-grid">
          <Toggle label="Batched operations" checked={form.enableBatchedOperations} onChange={(value) => setForm({ ...form, enableBatchedOperations: value })} />
          <Toggle label="Dead-letter expired messages" checked={form.deadLetteringOnMessageExpiration} onChange={(value) => setForm({ ...form, deadLetteringOnMessageExpiration: value })} />
          <Toggle label="Requires sessions" checked={form.requiresSession} onChange={(value) => setForm({ ...form, requiresSession: value })} />
        </div>
      </div>
      {save.isSuccess && <div className="notice success">Queue settings saved.</div>}
      {save.error && <div className="notice error">{save.error.message}</div>}
    </form>
  );
}

export function Metric({ label, value, icon, tone }: { label: string; value: number; icon: React.ReactNode; tone: string }) {
  return <div className={`metric-card ${tone}`}><div className="metric-icon">{icon}</div><div><strong>{value.toLocaleString()}</strong><span>{label}</span></div></div>;
}

export function Capability({ label, enabled }: { label: string; enabled: boolean }) {
  return <div className="capability"><span>{label}</span><strong className={enabled ? "enabled" : "disabled"}>{enabled ? "Enabled" : "Disabled"}</strong></div>;
}

function Tab({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return <button className={`tab ${active ? "active" : ""}`} role="tab" aria-selected={active} onClick={onClick}>{children}</button>;
}

function TextField({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return <label><span>{label}</span><input value={value} onChange={(event) => onChange(event.target.value)} /></label>;
}

function NumberField({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) {
  return <label><span>{label}</span><input type="number" min={0} value={value} onChange={(event) => onChange(Number(event.target.value))} /></label>;
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="toggle"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /><span>{label}</span></label>;
}

function cleanOptions<T extends Record<string, unknown>>(value: T): T {
  return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, item === "" ? null : item])) as T;
}

function PageState({ title, detail, error = false }: { title: string; detail: string; error?: boolean }) {
  return <div className={`page-state ${error ? "error" : ""}`}><RefreshCw size={25} className={error ? "" : "spinning"} /><h2>{title}</h2><p>{detail}</p></div>;
}
