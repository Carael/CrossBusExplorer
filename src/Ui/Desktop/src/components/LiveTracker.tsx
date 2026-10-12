import { useEffect, useMemo, useRef, useState } from "react";
import {
  Activity,
  Clipboard,
  Eraser,
  ExternalLink,
  Gauge,
  Inbox,
  Play,
  Radar,
  Square,
  X,
} from "lucide-react";
import { api, ApiError } from "../api";
import { formatCount } from "../format";
import { useDialogs } from "./Dialogs";
import { MessageDetails } from "./MessagesWorkspace";
import type {
  ActivityEvent,
  Connection,
  EntityActivity,
  ResourceSelection,
  TrackedEntityKind,
  TrackedMessage,
  TrackingSession,
} from "../types";

const MAX_CLIENT_ITEMS = 5000;

interface Props {
  connection: Connection;
  onNavigate: (selection: ResourceSelection) => void;
}

export function LiveTracker({ connection, onNavigate }: Props) {
  const dialogs = useDialogs();
  const [sessionId, setSessionId] = useState<string>();
  const [loadingExisting, setLoadingExisting] = useState(true);
  const [session, setSession] = useState<TrackingSession>();
  const [messages, setMessages] = useState<TrackedMessage[]>([]);
  const [events, setEvents] = useState<ActivityEvent[]>([]);
  const [entities, setEntities] = useState<EntityActivity[]>([]);
  const [selected, setSelected] = useState<TrackedMessage>();
  const [tab, setTab] = useState<"messages" | "activity" | "taps">("messages");
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [nameFilter, setNameFilter] = useState("");
  const [pollInterval, setPollInterval] = useState(5);
  const [includeDeadLetter, setIncludeDeadLetter] = useState(true);
  const [enableTopicTaps, setEnableTopicTaps] = useState(false);
  const cursor = useRef(0);

  // A session keeps running in the backend while the user browses other entities.
  useEffect(() => {
    let cancelled = false;
    api.trackingSessions()
      .then((sessions) => {
        if (cancelled) return;
        const existing = sessions.find((item) => item.connectionName === connection.name);
        if (existing) {
          setSessionId(existing.id);
          setNameFilter(existing.nameFilter ?? "");
          setPollInterval(existing.pollIntervalSeconds);
          setIncludeDeadLetter(existing.includeDeadLetter);
          setEnableTopicTaps(existing.enableTopicTaps);
        }
      })
      .catch((reason: unknown) => !cancelled && setError(errorMessage(reason)))
      .finally(() => !cancelled && setLoadingExisting(false));
    return () => { cancelled = true; };
  }, [connection.name]);

  useEffect(() => {
    if (!sessionId) return;
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;
    cursor.current = 0;
    setMessages([]);
    setEvents([]);
    setEntities([]);
    setSelected(undefined);

    async function poll() {
      try {
        const updates = await api.trackingUpdates(sessionId!, cursor.current);
        if (cancelled) return;
        cursor.current = updates.nextCursor;
        setSession(updates.session);
        setEntities(updates.entities);
        if (updates.messages.length) {
          setMessages((current) => [...updates.messages.slice().reverse(), ...current].slice(0, MAX_CLIENT_ITEMS));
        }
        if (updates.events.length) {
          setEvents((current) => [...updates.events.slice().reverse(), ...current].slice(0, MAX_CLIENT_ITEMS));
        }
        const active = updates.session.status === "Running" || updates.session.status === "Starting";
        if (active || updates.hasMore) {
          timer = setTimeout(poll, updates.hasMore ? 50 : 1000);
        }
      } catch (reason) {
        if (cancelled) return;
        if (reason instanceof ApiError && reason.status === 404) {
          setSessionId(undefined);
          setSession(undefined);
          return;
        }
        setError(errorMessage(reason));
        timer = setTimeout(poll, 3000);
      }
    }

    poll();
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [sessionId]);

  const running = session?.status === "Running" || session?.status === "Starting";

  async function start() {
    if (enableTopicTaps) {
      const confirmed = await dialogs.confirm({
        title: "Create temporary tap subscriptions?",
        message: "A subscription named cbe-tap-… is added to every matching topic so that each published message is copied to this tracker. Other users of the namespace can see them. They are deleted when tracking stops, or automatically after 5 idle minutes. This requires Manage rights.",
        confirmLabel: "Start tracking",
      });
      if (!confirmed) return;
    }

    setBusy(true);
    setError(undefined);
    setNotice(undefined);
    try {
      const created = await api.startTracking({
        connectionName: connection.name,
        nameFilter: nameFilter.trim() || undefined,
        enableTopicTaps,
        includeDeadLetter,
        pollIntervalSeconds: pollInterval,
      });
      setSession(created);
      setSessionId(created.id);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function stop() {
    if (!sessionId) return;
    setBusy(true);
    try {
      setSession(await api.stopTracking(sessionId));
      setNotice("Tracking stopped. Captured data stays available until you clear it or start a new session.");
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function clear() {
    if (!sessionId) return;
    setBusy(true);
    try {
      await api.deleteTracking(sessionId);
      setSessionId(undefined);
      setSession(undefined);
      setMessages([]);
      setEvents([]);
      setEntities([]);
      setSelected(undefined);
      setNotice(undefined);
    } catch (reason) {
      setError(errorMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function copyJson() {
    try {
      await navigator.clipboard.writeText(JSON.stringify({ session, entities, events, messages }, null, 2));
      setNotice(`Copied ${formatCount(messages.length)} messages and ${formatCount(events.length)} activity events as JSON.`);
    } catch (reason) {
      setError(errorMessage(reason));
    }
  }

  function openEntity(kind: TrackedEntityKind, entityName: string, subscriptionName?: string) {
    if (kind === "Queue") onNavigate({ kind: "queue", connectionName: connection.name, name: entityName });
    else if (kind === "Topic") onNavigate({ kind: "topic", connectionName: connection.name, name: entityName });
    else if (subscriptionName) onNavigate({ kind: "subscription", connectionName: connection.name, topicName: entityName, name: subscriptionName });
  }

  const visibleMessages = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();
    if (!term) return messages;
    return messages.filter((item) =>
      item.entityPath.toLocaleLowerCase().includes(term) ||
      item.message.id.toLocaleLowerCase().includes(term) ||
      (item.message.subject ?? "").toLocaleLowerCase().includes(term) ||
      item.message.body.toLocaleLowerCase().includes(term));
  }, [messages, search]);

  const unauthorizedTaps = session?.taps.filter((tap) => tap.state === "Unauthorized").length ?? 0;
  const activeTaps = session?.taps.filter((tap) => tap.state === "Active").length ?? 0;

  return (
    <div className="content-page wide-page">
      <header className="page-header">
        <div>
          <div className="breadcrumbs">Connections / {connection.name} / Live tracker</div>
          <div className="title-line">
            <div className="resource-icon"><Radar size={22} /></div>
            <div>
              <div className="title-with-status">
                <h1>Live tracker</h1>
                {session && <span className={`status ${running ? "" : "disabled"}`}>{session.status}</span>}
              </div>
              <p>See which queues, topics and subscriptions receive messages in {connection.fullyQualifiedName}.</p>
            </div>
          </div>
        </div>
        <div className="header-actions">
          {session && <button className="button secondary" onClick={copyJson} disabled={!messages.length && !events.length}><Clipboard size={16} /> Copy JSON</button>}
          {session && !running && <button className="button secondary" onClick={clear} disabled={busy}><Eraser size={16} /> Clear</button>}
          {running
            ? <button className="button danger-ghost" onClick={stop} disabled={busy}><Square size={16} /> {busy ? "Stopping…" : "Stop"}</button>
            : <button className="button primary" onClick={start} disabled={busy || loadingExisting}><Play size={16} /> {session ? "New session" : "Start tracking"}</button>}
        </div>
      </header>

      {notice && <div className="notice success"><span>{notice}</span><button className="notice-close" onClick={() => setNotice(undefined)}><X size={13} /></button></div>}
      {error && <div className="notice error"><span>{error}</span><button className="notice-close" onClick={() => setError(undefined)}><X size={13} /></button></div>}
      {session?.error && <div className="notice warning">{session.error}</div>}
      {unauthorizedTaps > 0 && <div className="notice warning">Tap subscriptions could not be created: your identity lacks Manage rights. Activity and queue peeking still work.</div>}

      {!running && (
        <div className="receive-bar tracker-form">
          <label className="grow">Name filter<input value={nameFilter} onChange={(event) => setNameFilter(event.target.value)} placeholder="All queues and topics (e.g. advisortool)" /></label>
          <label>Poll every<select value={pollInterval} onChange={(event) => setPollInterval(Number(event.target.value))}>{[2, 5, 10, 30, 60].map((value) => <option key={value} value={value}>{value}s</option>)}</select></label>
          <label className="toggle"><input type="checkbox" checked={includeDeadLetter} onChange={(event) => setIncludeDeadLetter(event.target.checked)} /> Dead letters</label>
          <label className="toggle" title="Creates a temporary subscription on each topic to capture every published message, even after consumers processed it."><input type="checkbox" checked={enableTopicTaps} onChange={(event) => setEnableTopicTaps(event.target.checked)} /> Topic taps (full content)</label>
        </div>
      )}

      {!session ? (
        <div className="card guidance-card">
          <span className="eyebrow">How it works</span>
          <h2>Find out where messages arrive</h2>
          <p><strong>Activity</strong> compares message counts and last-access times of every matching entity on each poll and lists what changed.</p>
          <p><strong>Peek</strong> reads new messages from changed queues and subscriptions without removing them. Messages that a consumer completes before the next poll can't be read.</p>
          <p><strong>Topic taps</strong> add a temporary subscription to each topic and capture a copy of every published message, including messages that were already processed. They require Manage rights.</p>
          <CaptureLimits open />
        </div>
      ) : (
        <>
          <div className="metric-grid">
            <Metric icon={<Gauge />} value={formatCount(session.watchedEntityCount)} label="Watched entities" />
            <Metric icon={<Activity />} value={formatCount(entities.length)} label="Entities with activity" accent />
            <Metric icon={<Inbox />} value={formatCount(messages.length)} label="Captured messages" accent />
            <Metric icon={<Radar />} value={session.enableTopicTaps ? `${activeTaps}/${session.taps.length}` : "Off"} label={`Taps · last poll ${formatTime(session.lastPollAt)}`} />
          </div>

          <div className="tab-strip">
            <button className={`tab ${tab === "messages" ? "active" : ""}`} onClick={() => setTab("messages")}>Messages<span>{formatCount(messages.length)}</span></button>
            <button className={`tab ${tab === "activity" ? "active" : ""}`} onClick={() => setTab("activity")}>Activity<span>{formatCount(entities.length)}</span></button>
            {session.enableTopicTaps && <button className={`tab ${tab === "taps" ? "active" : ""}`} onClick={() => setTab("taps")}>Taps<span>{formatCount(session.taps.length)}</span></button>}
          </div>

          {tab === "messages" && (
            <>
              <div className="receive-bar">
                <label className="grow">Search<input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Entity, message ID, subject or body" /></label>
              </div>
              <div className="message-layout">
                <div className="message-table-wrap">
                  {visibleMessages.length === 0 ? (
                    <div className="empty-messages"><Inbox size={23} /><strong>{running ? "Waiting for messages…" : "No messages captured"}</strong><span>New messages appear here as soon as they are detected.</span></div>
                  ) : (
                    <table className="data-table message-table">
                      <thead><tr><th>Captured</th><th>Source</th><th>Entity</th><th>Message ID</th><th>Subject</th><th>Enqueued</th><th>Size</th></tr></thead>
                      <tbody>
                        {visibleMessages.map((item) => (
                          <tr key={item.cursor} className={selected?.cursor === item.cursor ? "selected" : ""} onClick={() => setSelected(item)}>
                            <td>{formatTime(item.capturedAt)}</td>
                            <td><span className={`source-badge ${item.source.toLowerCase()}`}>{sourceLabel(item.source)}</span></td>
                            <td title={item.entityPath}>{item.entityPath}</td>
                            <td><button className="table-link">{item.message.id}</button></td>
                            <td>{item.message.subject || "—"}</td>
                            <td>{formatTime(item.message.systemProperties.enqueuedTime)}</td>
                            <td>{formatSize(item.message.body.length)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
                {selected && (
                  <MessageDetails
                    message={selected.message}
                    onClose={() => setSelected(undefined)}
                    actions={<button className="button secondary" onClick={() => openEntity(selected.entityKind, selected.entityName, selected.subscriptionName)}><ExternalLink size={14} /> Open {selected.entityKind.toLowerCase()}</button>}
                  />
                )}
              </div>
            </>
          )}

          {tab === "activity" && (
            <div className="tracker-activity">
              <section className="card table-card">
                <div className="card-heading"><div><span className="eyebrow">Where</span><h2>Entities with activity</h2></div></div>
                {entities.length === 0 ? <div className="empty-messages"><Activity size={23} /><strong>No activity yet</strong></div> : (
                  <div className="message-table-wrap">
                    <table className="data-table">
                      <thead><tr><th>Entity</th><th>Kind</th><th>Last activity</th><th>Changes</th><th>Captured</th><th>Active</th><th>Dead letter</th></tr></thead>
                      <tbody>
                        {entities.map((item) => (
                          <tr key={item.entityPath}>
                            <td><button className="table-link" onClick={() => openEntity(item.entityKind, item.entityName, item.subscriptionName)}>{item.entityPath}</button></td>
                            <td>{item.entityKind}</td>
                            <td>{formatTime(item.lastActivityAt)}</td>
                            <td>{formatCount(item.changeCount)}</td>
                            <td>{formatCount(item.capturedCount)}</td>
                            <td>{item.entityKind === "Topic" ? "—" : formatCount(item.activeCount)}</td>
                            <td>{item.entityKind === "Topic" ? "—" : formatCount(item.deadLetterCount)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
              <section className="card table-card">
                <div className="card-heading"><div><span className="eyebrow">When</span><h2>Recent changes</h2></div></div>
                {events.length === 0 ? <div className="empty-messages"><Activity size={23} /><strong>No changes detected yet</strong></div> : (
                  <div className="message-table-wrap">
                    <table className="data-table">
                      <thead><tr><th>Time</th><th>Entity</th><th>Change</th></tr></thead>
                      <tbody>
                        {events.slice(0, 300).map((item) => (
                          <tr key={item.cursor}>
                            <td>{formatTime(item.at)}</td>
                            <td title={item.entityPath}>{item.entityPath}</td>
                            <td>{describeChange(item)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </div>
          )}

          {tab === "taps" && (
            <section className="card table-card">
              <div className="card-heading"><div><span className="eyebrow">Temporary subscriptions</span><h2>Topic taps</h2></div></div>
              <table className="data-table">
                <thead><tr><th>Topic</th><th>Subscription</th><th>State</th><th>Last error</th></tr></thead>
                <tbody>
                  {session.taps.map((tap) => (
                    <tr key={tap.topicName}>
                      <td>{tap.topicName}</td>
                      <td><code>{tap.subscriptionName}</code></td>
                      <td><span className={`status ${tap.state === "Active" ? "" : "disabled"}`}>{tap.state}</span></td>
                      <td>{tap.error ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          )}

          {session.droppedCount > 0 && <p className="field-help">{formatCount(session.droppedCount)} older items were discarded to limit memory use.</p>}
          <CaptureLimits />
        </>
      )}
    </div>
  );
}

function CaptureLimits({ open = false }: { open?: boolean }) {
  return (
    <details className="capture-limits" open={open}>
      <summary>Wann erscheint eine Nachricht – und wann nicht?</summary>
      <div className="capture-limits-body">
        <h4>Die Nachricht erscheint sicher</h4>
        <ul>
          <li>Sie wird bei aktiven Topic taps auf ein Topic publiziert (z. B. ein MassTransit-Event mit <code>Publish</code>). Der Inhalt bleibt sichtbar, auch wenn ein Consumer sie bereits verarbeitet hat.</li>
          <li>Bei aktivierten Dead letters landet sie in einer Dead-Letter-Queue.</li>
        </ul>
        <h4>Die Nachricht erscheint nur manchmal</h4>
        <ul>
          <li>Sie wird direkt an eine Queue gesendet (z. B. ein MassTransit-Command mit <code>Send</code>). Topic taps sehen solche Nachrichten nicht. Sie erscheint nur, wenn sie beim nächsten Poll noch in der Queue liegt. Verarbeitet ein Consumer sie schneller, ist sie weg. Die Aktivität der Queue wird trotzdem angezeigt.</li>
        </ul>
        <h4>Die Nachricht erscheint nicht</h4>
        <ul>
          <li>Sie wurde vor dem Start gesendet. Topic taps werden beim ersten Poll nach dem Start angelegt, neue Topics beim nächsten Poll.</li>
          <li>Sie wurde nach dem Stoppen oder bei geschlossener App gesendet.</li>
          <li>Der Name des Topics oder der Queue passt nicht zum Namensfilter.</li>
          <li>Der Tap konnte nicht angelegt werden, etwa ohne Manage-Rechte (siehe Tab «Taps»).</li>
          <li>Die App war länger als 5 Minuten unterbrochen (Ruhezustand, Netzwerk). Kopien im Tap laufen dann ab, und der Tap wird automatisch gelöscht.</li>
          <li>Sie ist geplant (<code>ScheduledEnqueueTime</code>). Sie erscheint erst zum geplanten Zeitpunkt.</li>
          <li>Der Inhalt ist kein lesbarer Text. Dann erscheint ein Hinweis statt der Nachricht.</li>
          <li>Bei mehr als 5000 Nachrichten werden die ältesten verworfen.</li>
        </ul>
      </div>
    </details>
  );
}

function Metric({ icon, value, label, accent = false }: { icon: React.ReactNode; value: string; label: string; accent?: boolean }) {
  return <div className={`metric-card ${accent ? "accent" : ""}`}><div className="metric-icon">{icon}</div><div><strong>{value}</strong><span>{label}</span></div></div>;
}

function sourceLabel(source: TrackedMessage["source"]) {
  return source === "DeadLetter" ? "Dead letter" : source;
}

function describeChange(event: ActivityEvent) {
  const parts: string[] = [];
  if (event.activeDelta) parts.push(`${signed(event.activeDelta)} active (now ${formatCount(event.activeCount)})`);
  if (event.deadLetterDelta) parts.push(`${signed(event.deadLetterDelta)} dead letter (now ${formatCount(event.deadLetterCount)})`);
  return parts.length ? parts.join(", ") : event.entityKind === "Topic" ? "Message published" : "Sent or received";
}

function signed(value: number) {
  return value > 0 ? `+${formatCount(value)}` : formatCount(value);
}

function formatTime(value?: string) {
  if (!value || value.startsWith("0001-")) return "—";
  return new Date(value).toLocaleTimeString();
}

function formatSize(length: number) {
  return length < 1024 ? `${length} B` : `${(length / 1024).toFixed(1)} KB`;
}

function errorMessage(reason: unknown) {
  return reason instanceof Error ? reason.message : String(reason);
}
