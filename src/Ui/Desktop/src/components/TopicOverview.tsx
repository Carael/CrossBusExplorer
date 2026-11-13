import { useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  Clock3,
  Copy,
  FileUp,
  FolderTree,
  Layers3,
  MessageSquarePlus,
  RefreshCw,
  Save,
  Star,
  Trash2,
  X,
} from "lucide-react";
import { api } from "../api";
import { favoriteKey, toggleFavorite, useFavorites } from "../favorites";
import { formatCount } from "../format";
import type { SendMessage, SubscriptionInfo, TopicDetails } from "../types";
import { useRefetchInterval } from "./AutoRefresh";
import { useDialogs } from "./Dialogs";
import { MessageComposer, parseImportedMessageContent } from "./MessagesWorkspace";
import { Capability, Metric } from "./QueueOverview";

type SubscriptionSortKey = "name" | "status" | "active" | "deadLetter" | "accessed";
type SubscriptionSort = { key: SubscriptionSortKey; direction: "asc" | "desc" };

const subscriptionSortValue: Record<SubscriptionSortKey, (item: SubscriptionInfo) => string | number> = {
  name: (item) => item.subscriptionName.toLocaleLowerCase(),
  status: (item) => item.status.toLocaleLowerCase(),
  active: (item) => item.activeMessagesCount,
  deadLetter: (item) => item.deadLetterMessagesCount,
  accessed: (item) => item.accessedAt ?? "",
};

export function TopicOverview({
  connectionId,
  connectionName,
  topicName,
  onDeleted,
  onSelectSubscription,
}: {
  connectionId: string;
  connectionName: string;
  topicName: string;
  onDeleted: () => void;
  onSelectSubscription: (name: string) => void;
}) {
  const queryClient = useQueryClient();
  const dialogs = useDialogs();
  const fileInput = useRef<HTMLInputElement>(null);
  const [tab, setTab] = useState<"overview" | "subscriptions" | "settings">("overview");
  const [subSort, setSubSort] = useState<SubscriptionSort>({ key: "name", direction: "asc" });
  const [composer, setComposer] = useState<SendMessage>();
  const [notice, setNotice] = useState<string>();
  const favorites = useFavorites();
  const isFavorite = favorites.has(favoriteKey("topic", connectionId, topicName));
  const refetchInterval = useRefetchInterval(connectionId);
  const topic = useQuery({ queryKey: ["topic", connectionId, topicName], queryFn: () => api.topic(connectionId, topicName), staleTime: 0, refetchOnMount: "always", refetchInterval, refetchIntervalInBackground: false });
  const subscriptions = useQuery({ queryKey: ["subscriptions", connectionId, topicName], queryFn: () => api.subscriptions(connectionId, topicName), staleTime: 0, refetchOnMount: "always", refetchInterval, refetchIntervalInBackground: false });
  const send = useMutation({
    mutationFn: ({ message, destination = topicName }: { message: SendMessage; destination?: string }) =>
      api.sendMessages(connectionId, destination, [message]),
    onSuccess: (result, { destination = topicName }) => {
      setComposer(undefined);
      setNotice(`Sent ${result.count} message${result.count === 1 ? "" : "s"} to ${destination}.`);
      subscriptions.refetch();
    },
  });
  const sortedSubscriptions = useMemo(() => {
    const items = subscriptions.data ?? [];
    const value = subscriptionSortValue[subSort.key];
    const factor = subSort.direction === "asc" ? 1 : -1;
    return [...items].sort((left, right) => {
      const a = value(left);
      const b = value(right);
      return (a < b ? -1 : a > b ? 1 : 0) * factor;
    });
  }, [subscriptions.data, subSort]);

  function toggleSubSort(key: SubscriptionSortKey) {
    setSubSort((current) => ({
      key,
      direction: current.key === key && current.direction === "asc" ? "desc" : "asc",
    }));
  }

  async function importFiles(files: FileList | null) {
    if (!files?.length) return;
    try {
      const imported = await Promise.all(
        Array.from(files).map(async (file) => parseImportedMessageContent(await file.text())),
      );
      const result = await api.sendMessages(connectionId, topicName, imported);
      setNotice(`Imported and sent ${result.count} message${result.count === 1 ? "" : "s"} to ${topicName}.`);
      subscriptions.refetch();
    } catch (err) {
      setNotice(err instanceof Error ? err.message : "Unable to import messages.");
    } finally {
      if (fileInput.current) fileInput.current.value = "";
    }
  }
  const clone = useMutation({
    mutationFn: (name: string) => api.cloneTopic(connectionId, topicName, name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["topics", connectionId] }),
  });
  const remove = useMutation({
    mutationFn: () => api.deleteTopic(connectionId, topicName),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["topics", connectionId] });
      onDeleted();
    },
  });
  const createSubscription = useMutation({
    mutationFn: (subscriptionName: string) => api.createSubscription(connectionId, topicName, { topicName, subscriptionName }),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions", connectionId, topicName] });
      if (result.data) onSelectSubscription(result.data.info.subscriptionName);
    },
  });

  if (topic.isPending) return <TopicState title="Loading topic" detail="Reading settings and runtime metrics…" />;
  if (topic.error) return <TopicState title="Unable to load topic" detail={topic.error.message} error />;

  const details = topic.data;
  const error = clone.error ?? remove.error ?? createSubscription.error;

  async function promptClone() {
    const name = await dialogs.prompt({
      title: "Clone topic",
      message: `Create a copy of ${topicName} with the same settings.`,
      defaultValue: `${topicName}-copy`,
      confirmLabel: "Clone",
    });
    if (name) clone.mutate(name);
  }
  async function promptSubscription() {
    const name = await dialogs.prompt({
      title: "New subscription",
      message: `Add a subscription to ${topicName}.`,
      placeholder: "Subscription name",
      confirmLabel: "Create",
    });
    if (name) createSubscription.mutate(name);
  }
  async function promptDelete() {
    const confirmed = await dialogs.confirm({
      title: `Delete topic ${topicName}?`,
      message: "The topic, its subscriptions, and all messages are permanently removed.",
      confirmLabel: "Delete",
      danger: true,
    });
    if (confirmed) remove.mutate();
  }

  return (
    <div className="content-page wide-page">
      <header className="page-header">
        <div><div className="breadcrumbs">{connectionName} / Topics / {topicName}</div><div className="title-line"><div className="resource-icon topic"><FolderTree size={22} /></div><div><div className="title-with-status"><h1>{topicName}</h1><span className={`status ${details.info.status.toLowerCase()}`}>{details.info.status}</span></div><p>Topic configuration and subscriptions</p></div></div></div>
        <div className="header-actions">
          <button
            className={`button secondary favorite-action ${isFavorite ? "active" : ""}`}
            onClick={() => toggleFavorite("topic", connectionId, topicName)}
            title={isFavorite ? "Remove from favorites" : "Add to favorites"}
            aria-pressed={isFavorite}
          >
            <Star size={15} /> {isFavorite ? "Favorited" : "Favorite"}
          </button>
          <button className="button secondary" onClick={() => setComposer({ body: "" })}><MessageSquarePlus size={15} /> Send message</button>
          <input ref={fileInput} type="file" multiple hidden onChange={(event) => importFiles(event.target.files)} />
          <button className="button secondary" onClick={() => fileInput.current?.click()}><FileUp size={15} /> Import files</button>
          <button className="button secondary" onClick={promptClone} disabled={clone.isPending}><Copy size={15} /> Clone</button>
          <button className="button secondary" onClick={() => { topic.refetch(); subscriptions.refetch(); }}><RefreshCw size={15} /> Refresh</button>
          <button className="button danger-ghost" onClick={promptDelete}><Trash2 size={15} /> Delete</button>
        </div>
      </header>
      {notice && <div className="notice success"><span>{notice}</span><button className="notice-close" onClick={() => setNotice(undefined)}><X size={13} /></button></div>}
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
            <table className="data-table">
              <thead>
                <tr>
                  <SubscriptionSortHeader label="Name" column="name" sort={subSort} onSort={toggleSubSort} />
                  <SubscriptionSortHeader label="Status" column="status" sort={subSort} onSort={toggleSubSort} />
                  <SubscriptionSortHeader label="Active" column="active" sort={subSort} onSort={toggleSubSort} />
                  <SubscriptionSortHeader label="Dead letter" column="deadLetter" sort={subSort} onSort={toggleSubSort} />
                  <SubscriptionSortHeader label="Last accessed" column="accessed" sort={subSort} onSort={toggleSubSort} />
                </tr>
              </thead>
              <tbody>
                {sortedSubscriptions.map((item) => (
                  <tr key={item.subscriptionName} onDoubleClick={() => onSelectSubscription(item.subscriptionName)}>
                    <td><button className="table-link" onClick={() => onSelectSubscription(item.subscriptionName)}>{item.subscriptionName}</button></td>
                    <td><span className={`status ${item.status.toLowerCase()}`}>{item.status}</span></td>
                    <td>{formatCount(item.activeMessagesCount)}</td>
                    <td className={item.deadLetterMessagesCount > 0 ? "danger-text" : ""}>{formatCount(item.deadLetterMessagesCount)}</td>
                    <td>{new Date(item.accessedAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}
      {tab === "settings" && <TopicSettings connectionId={connectionId} topicName={topicName} details={details} />}
      {composer && (
        <MessageComposer
          initial={composer}
          defaultDestination={topicName}
          busy={send.isPending}
          error={send.error?.message}
          onClose={() => setComposer(undefined)}
          onSend={(message, destination) => send.mutate({ message, destination })}
        />
      )}
    </div>
  );
}

function SubscriptionSortHeader({
  label,
  column,
  sort,
  onSort,
}: {
  label: string;
  column: SubscriptionSortKey;
  sort: SubscriptionSort;
  onSort: (key: SubscriptionSortKey) => void;
}) {
  const active = sort.key === column;
  const Icon = !active ? ArrowUpDown : sort.direction === "asc" ? ArrowUp : ArrowDown;
  return (
    <th aria-sort={active ? (sort.direction === "asc" ? "ascending" : "descending") : "none"}>
      <button className="sort-header" onClick={() => onSort(column)}>
        {label}
        <Icon size={12} />
      </button>
    </th>
  );
}

function TopicSummary({ details, onEdit }: { details: TopicDetails; onEdit: () => void }) {
  return <div className="overview-grid"><section className="card"><div className="card-heading"><div><span className="eyebrow">Configuration</span><h2>Capacity and retention</h2></div><button className="button tertiary" onClick={onEdit}>Edit settings</button></div><dl className="definition-list compact"><div><dt>Maximum size</dt><dd>{formatCount(details.properties.maxQueueSizeInMegabytes)} MB</dd></div><div><dt>Maximum message</dt><dd>{details.properties.maxMessageSizeInKilobytes != null ? formatCount(details.properties.maxMessageSizeInKilobytes) : "Default"} KB</dd></div><div><dt>Default message TTL</dt><dd>{details.timeSettings.defaultMessageTimeToLive}</dd></div><div><dt>Auto-delete on idle</dt><dd>{details.timeSettings.autoDeleteOnIdle}</dd></div><div><dt>Duplicate detection window</dt><dd>{details.timeSettings.duplicateDetectionHistoryTimeWindow}</dd></div></dl></section><section className="card"><div className="card-heading"><div><span className="eyebrow">Behavior</span><h2>Capabilities</h2></div></div><div className="capability-list"><Capability label="Batched operations" enabled={details.settings.enableBatchedOperations} /><Capability label="Partitioning" enabled={details.settings.enablePartitioning} /><Capability label="Duplicate detection" enabled={details.settings.requiresDuplicateDetection} /><Capability label="Ordering" enabled={details.settings.supportOrdering} /></div></section></div>;
}

function TopicSettings({ connectionId, topicName, details }: { connectionId: string; topicName: string; details: TopicDetails }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => ({ name: topicName, maxSizeInMegabytes: details.properties.maxQueueSizeInMegabytes, maxMessageSizeInKilobytes: details.properties.maxMessageSizeInKilobytes, defaultMessageTimeToLive: details.timeSettings.defaultMessageTimeToLive, autoDeleteOnIdle: details.timeSettings.autoDeleteOnIdle, duplicateDetectionHistoryTimeWindow: details.timeSettings.duplicateDetectionHistoryTimeWindow, enableBatchedOperations: details.settings.enableBatchedOperations, supportOrdering: details.settings.supportOrdering, status: details.info.status, userMetadata: details.properties.userMetadata ?? "" }));
  const save = useMutation({ mutationFn: () => api.updateTopic(connectionId, topicName, clean(form)), onSuccess: () => { queryClient.invalidateQueries({ queryKey: ["topic", connectionId, topicName] }); queryClient.invalidateQueries({ queryKey: ["topics", connectionId] }); } });
  return <form className="card settings-card" onSubmit={(event) => { event.preventDefault(); save.mutate(); }}><div className="card-heading"><div><span className="eyebrow">Topic</span><h2>Editable settings</h2></div><button className="button primary" disabled={save.isPending}><Save size={15} /> {save.isPending ? "Saving…" : "Save changes"}</button></div><div className="form-grid entity-form"><Field label="Maximum size (MB)" type="number" value={form.maxSizeInMegabytes} onChange={(value) => setForm({ ...form, maxSizeInMegabytes: Number(value) })} /><Field label="Maximum message size (KB)" type="number" value={form.maxMessageSizeInKilobytes ?? 0} onChange={(value) => setForm({ ...form, maxMessageSizeInKilobytes: Number(value) || undefined })} /><Field label="Default message TTL" value={form.defaultMessageTimeToLive} onChange={(value) => setForm({ ...form, defaultMessageTimeToLive: value })} /><Field label="Auto-delete on idle" value={form.autoDeleteOnIdle} onChange={(value) => setForm({ ...form, autoDeleteOnIdle: value })} /><Field label="Duplicate detection window" value={form.duplicateDetectionHistoryTimeWindow} onChange={(value) => setForm({ ...form, duplicateDetectionHistoryTimeWindow: value })} /><label><span>Status</span><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option>Active</option><option>Disabled</option><option>SendDisabled</option><option>ReceiveDisabled</option></select></label><label className="wide"><span>User metadata</span><textarea rows={4} value={form.userMetadata} onChange={(event) => setForm({ ...form, userMetadata: event.target.value })} /></label><div className="wide toggle-grid"><Toggle label="Batched operations" checked={form.enableBatchedOperations} onChange={(value) => setForm({ ...form, enableBatchedOperations: value })} /><Toggle label="Support ordering" checked={form.supportOrdering} onChange={(value) => setForm({ ...form, supportOrdering: value })} /></div></div>{save.isSuccess && <div className="notice success">Topic settings saved.</div>}{save.error && <div className="notice error">{save.error.message}</div>}</form>;
}

function TopicTab({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) { return <button className={`tab ${active ? "active" : ""}`} onClick={onClick}>{children}</button>; }
function Field({ label, type = "text", value, onChange }: { label: string; type?: string; value: string | number; onChange: (value: string) => void }) { return <label><span>{label}</span><input type={type} value={value} onChange={(event) => onChange(event.target.value)} /></label>; }
function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) { return <label className="toggle"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /><span>{label}</span></label>; }
function clean<T extends Record<string, unknown>>(value: T): T { return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, item === "" ? null : item])) as T; }
function TopicState({ title, detail, error = false }: { title: string; detail: string; error?: boolean }) { return <div className={`page-state ${error ? "error" : ""}`}><RefreshCw size={25} className={error ? "" : "spinning"} /><h2>{title}</h2><p>{detail}</p></div>; }
