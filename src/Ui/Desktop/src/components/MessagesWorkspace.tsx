import { useMemo, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  ArrowDownToLine,
  CopyPlus,
  Eye,
  FileUp,
  MessageSquarePlus,
  Play,
  RefreshCw,
  RotateCcw,
  Send,
  Trash2,
  X,
} from "lucide-react";
import { api } from "../api";
import type { SendMessage, ServiceBusMessage, SubQueue } from "../types";

interface Props {
  connectionName: string;
  entityName: string;
  subscriptionName?: string;
  activeCount: number;
  deadLetterCount: number;
  transferDeadLetterCount?: number;
  onJobStarted?: () => void;
}

export function MessagesWorkspace({
  connectionName,
  entityName,
  subscriptionName,
  activeCount,
  deadLetterCount,
  transferDeadLetterCount = 0,
  onJobStarted,
}: Props) {
  const queryClient = useQueryClient();
  const fileInput = useRef<HTMLInputElement>(null);
  const [subQueue, setSubQueue] = useState<SubQueue>("None");
  const [mode, setMode] = useState<"PeekLock" | "ReceiveAndDelete">("PeekLock");
  const [receiveType, setReceiveType] = useState<"All" | "ByCount">("ByCount");
  const [count, setCount] = useState(25);
  const [fromSequence, setFromSequence] = useState("");
  const [messages, setMessages] = useState<ServiceBusMessage[]>([]);
  const [selected, setSelected] = useState<ServiceBusMessage>();
  const [composer, setComposer] = useState<SendMessage>();
  const [notice, setNotice] = useState<string>();

  const availableCount = subQueue === "None"
    ? activeCount
    : subQueue === "DeadLetter"
      ? deadLetterCount
      : transferDeadLetterCount;

  const receive = useMutation({
    mutationFn: (append: boolean) => api.receiveMessages(connectionName, {
      entityName,
      subscriptionName,
      subQueue,
      mode,
      type: receiveType,
      messagesCount: receiveType === "ByCount" ? count : undefined,
      fromSequenceNumber: append
        ? nextSequence(messages)
        : fromSequence
          ? Number(fromSequence)
          : undefined,
    }),
    onSuccess: (result, append) => {
      setMessages((current) => append ? [...current, ...result] : result);
      setSelected(undefined);
      setNotice(`${append ? "Loaded" : "Received"} ${result.length} message${result.length === 1 ? "" : "s"}.`);
    },
  });

  const send = useMutation({
    mutationFn: (message: SendMessage) => api.sendMessages(connectionName, entityName, [message]),
    onSuccess: (result) => {
      setComposer(undefined);
      setNotice(`Sent ${result.count} message${result.count === 1 ? "" : "s"} to ${entityName}.`);
    },
  });

  const startJob = useMutation({
    mutationFn: ({ action, message }: { action: "purge" | "resend" | "delete"; message?: ServiceBusMessage }) => {
      if (action === "delete" && message) {
        return api.deleteMessage(
          connectionName,
          entityName,
          subscriptionName,
          subQueue,
          message.systemProperties.sequenceNumber,
        );
      }
      if (action === "resend") {
        const destination = window.prompt("Destination queue or topic", entityName)?.trim();
        if (!destination) throw new Error("A destination is required.");
        return api.resendMessages(
          connectionName,
          entityName,
          subscriptionName,
          subQueue,
          destination,
          availableCount,
        );
      }
      return api.purgeMessages(
        connectionName,
        entityName,
        subscriptionName,
        subQueue,
        availableCount,
      );
    },
    onSuccess: (_, { action, message }) => {
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
      if (action === "delete" && message) {
        setMessages((current) => current.filter((item) => item !== message));
        setSelected(undefined);
      }
      setNotice("Operation scheduled. Progress is available in Operations.");
      onJobStarted?.();
    },
  });

  async function importFiles(files: FileList | null) {
    if (!files?.length) return;
    try {
      const imported = await Promise.all(
        Array.from(files).map(async (file) => ({ body: await file.text() })),
      );
      const result = await api.sendMessages(connectionName, entityName, imported);
      setNotice(`Imported and sent ${result.count} message${result.count === 1 ? "" : "s"}.`);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Unable to import messages.");
    } finally {
      if (fileInput.current) fileInput.current.value = "";
    }
  }

  function confirmPurge() {
    if (availableCount <= 0) return;
    if (window.confirm(`Permanently purge up to ${availableCount.toLocaleString()} messages from this source?`)) {
      startJob.mutate({ action: "purge" });
    }
  }

  function requeue(message: ServiceBusMessage) {
    send.mutate(messageToSend(message));
  }

  const error = receive.error ?? send.error ?? startJob.error;

  return (
    <section className="messages-workspace">
      <div className="message-toolbar">
        <div className="segmented-control" aria-label="Message source">
          <SourceButton label="Active" count={activeCount} active={subQueue === "None"} onClick={() => setSubQueue("None")} />
          <SourceButton label="Dead letter" count={deadLetterCount} active={subQueue === "DeadLetter"} onClick={() => setSubQueue("DeadLetter")} />
          {transferDeadLetterCount > 0 && (
            <SourceButton label="Transfer DLQ" count={transferDeadLetterCount} active={subQueue === "TransferDeadLetter"} onClick={() => setSubQueue("TransferDeadLetter")} />
          )}
        </div>
        <div className="toolbar-actions">
          <button className="button secondary" onClick={() => setComposer({ body: "" })}><MessageSquarePlus size={15} /> New message</button>
          <input ref={fileInput} type="file" multiple hidden onChange={(event) => importFiles(event.target.files)} />
          <button className="button secondary" onClick={() => fileInput.current?.click()}><FileUp size={15} /> Import files</button>
          {subQueue !== "None" && <button className="button secondary" onClick={() => startJob.mutate({ action: "resend" })} disabled={!availableCount}><RotateCcw size={15} /> Resend all</button>}
          <button className="button danger-ghost" onClick={confirmPurge} disabled={!availableCount}><Trash2 size={15} /> Purge</button>
        </div>
      </div>

      <div className="receive-bar">
        <label>Mode<select value={mode} onChange={(event) => setMode(event.target.value as typeof mode)}><option value="PeekLock">Peek (safe)</option><option value="ReceiveAndDelete">Receive and delete</option></select></label>
        <label>Receive<select value={receiveType} onChange={(event) => setReceiveType(event.target.value as typeof receiveType)}><option value="ByCount">By count</option><option value="All">All messages</option></select></label>
        <label>Count<input type="number" min={1} max={250} disabled={receiveType === "All"} value={count} onChange={(event) => setCount(Math.min(250, Math.max(1, Number(event.target.value))))} /></label>
        <label>From sequence<input inputMode="numeric" disabled={mode === "ReceiveAndDelete"} value={fromSequence} onChange={(event) => setFromSequence(event.target.value)} placeholder="Beginning" /></label>
        <button className="button primary" onClick={() => receive.mutate(false)} disabled={receive.isPending}><Play size={15} /> {receive.isPending ? "Receiving…" : "Receive"}</button>
        {messages.length > 0 && mode === "PeekLock" && receiveType === "ByCount" && <button className="button secondary" onClick={() => receive.mutate(true)} disabled={receive.isPending}><ArrowDownToLine size={15} /> Peek more</button>}
      </div>

      {mode === "ReceiveAndDelete" && <div className="notice warning">Receive and delete removes messages as they are read. This cannot be undone.</div>}
      {notice && <div className="notice success"><span>{notice}</span><button className="notice-close" onClick={() => setNotice(undefined)}><X size={13} /></button></div>}
      {error && <div className="notice error">{error.message}</div>}

      <div className="message-layout">
        <div className="message-table-wrap">
          {messages.length === 0 ? (
            <div className="empty-messages"><RefreshCw size={23} /><strong>No messages loaded</strong><span>Choose a source and receive mode, then load up to 250 messages.</span></div>
          ) : (
            <table className="data-table message-table">
              <thead><tr><th>Actions</th><th>Message ID</th><th>Sequence</th><th>Subject</th><th>Enqueued</th><th>Expires</th></tr></thead>
              <tbody>
                {messages.map((message) => (
                  <tr key={`${message.id}-${message.systemProperties.sequenceNumber}`} className={selected === message ? "selected" : ""}>
                    <td className="row-actions">
                      <button title="View" onClick={() => setSelected(message)}><Eye size={14} /></button>
                      <button title="Edit and send a copy" onClick={() => setComposer(messageToSend(message))}><CopyPlus size={14} /></button>
                      <button title="Requeue to this entity" onClick={() => requeue(message)}><Send size={14} /></button>
                      <button title="Delete" onClick={() => { if (window.confirm(`Delete sequence ${message.systemProperties.sequenceNumber}?`)) startJob.mutate({ action: "delete", message }); }}><Trash2 size={14} /></button>
                    </td>
                    <td><button className="table-link" onClick={() => setSelected(message)}>{message.id}</button></td>
                    <td>{message.systemProperties.sequenceNumber}</td>
                    <td>{message.subject || "—"}</td>
                    <td>{formatDate(message.systemProperties.enqueuedTime)}</td>
                    <td>{formatDate(message.systemProperties.expiresAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
        {selected && <MessageDetails message={selected} onClose={() => setSelected(undefined)} onEdit={() => setComposer(messageToSend(selected))} onRequeue={() => requeue(selected)} />}
      </div>

      {composer && (
        <MessageComposer
          initial={composer}
          busy={send.isPending}
          error={send.error?.message}
          onClose={() => setComposer(undefined)}
          onSend={(message) => send.mutate(message)}
        />
      )}
    </section>
  );
}

function SourceButton({ label, count, active, onClick }: { label: string; count: number; active: boolean; onClick: () => void }) {
  return <button className={active ? "active" : ""} onClick={onClick}>{label}<span>{count.toLocaleString()}</span></button>;
}

function MessageDetails({ message, onClose, onEdit, onRequeue }: { message: ServiceBusMessage; onClose: () => void; onEdit: () => void; onRequeue: () => void }) {
  const body = useMemo(() => prettyBody(message.body), [message.body]);
  return (
    <aside className="message-details-panel">
      <header><div><span className="eyebrow">Message</span><h3>{message.subject || message.id}</h3></div><button className="icon-button" onClick={onClose}><X size={17} /></button></header>
      <div className="detail-actions"><button className="button secondary" onClick={onEdit}><CopyPlus size={14} /> Edit copy</button><button className="button primary" onClick={onRequeue}><Send size={14} /> Requeue</button></div>
      <section><h4>Body</h4><pre className="code-view">{body}</pre></section>
      <section><h4>System properties</h4><PropertyTable value={{ id: message.id, subject: message.subject, ...message.systemProperties }} /></section>
      <section><h4>Application properties</h4><PropertyTable value={message.applicationProperties ?? {}} /></section>
    </aside>
  );
}

function PropertyTable({ value }: { value: Record<string, unknown> }) {
  return <dl className="property-table">{Object.entries(value).map(([key, item]) => <div key={key}><dt>{key}</dt><dd>{formatValue(item)}</dd></div>)}</dl>;
}

function MessageComposer({ initial, busy, error, onClose, onSend }: { initial: SendMessage; busy: boolean; error?: string; onClose: () => void; onSend: (message: SendMessage) => void }) {
  const [message, setMessage] = useState(initial);
  const [properties, setProperties] = useState(JSON.stringify(initial.applicationProperties ?? {}, null, 2));
  const [validation, setValidation] = useState<string>();

  function submit(event: React.FormEvent) {
    event.preventDefault();
    try {
      const applicationProperties = JSON.parse(properties) as Record<string, unknown>;
      setValidation(undefined);
      onSend({ ...message, applicationProperties });
    } catch {
      setValidation("Application properties must be a valid JSON object.");
    }
  }

  return (
    <div className="dialog-backdrop" role="presentation">
      <div className="dialog message-composer" role="dialog" aria-modal="true" aria-label="Send message">
        <div className="dialog-header"><div><span className="eyebrow">Composer</span><h2>Send message</h2></div><button className="icon-button" onClick={onClose}><X size={18} /></button></div>
        <form onSubmit={submit}>
          <div className="form-grid">
            <label className="wide"><span>Body</span><textarea rows={13} required value={message.body} onChange={(event) => setMessage({ ...message, body: event.target.value })} /></label>
            <label><span>Message ID</span><input value={message.id ?? ""} onChange={(event) => setMessage({ ...message, id: event.target.value || undefined })} /></label>
            <label><span>Subject</span><input value={message.subject ?? ""} onChange={(event) => setMessage({ ...message, subject: event.target.value || undefined })} /></label>
            <label><span>Content type</span><input value={message.contentType ?? ""} onChange={(event) => setMessage({ ...message, contentType: event.target.value || undefined })} placeholder="application/json" /></label>
            <label><span>Correlation ID</span><input value={message.correlationId ?? ""} onChange={(event) => setMessage({ ...message, correlationId: event.target.value || undefined })} /></label>
            <label><span>Session ID</span><input value={message.sessionId ?? ""} onChange={(event) => setMessage({ ...message, sessionId: event.target.value || undefined })} /></label>
            <label><span>Partition key</span><input value={message.partitionKey ?? ""} onChange={(event) => setMessage({ ...message, partitionKey: event.target.value || undefined })} /></label>
            <label className="wide"><span>Application properties (JSON)</span><textarea rows={5} value={properties} onChange={(event) => setProperties(event.target.value)} /></label>
          </div>
          {(validation || error) && <div className="inline-error">{validation ?? error}</div>}
          <div className="dialog-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={busy}><Send size={15} /> {busy ? "Sending…" : "Send"}</button></div>
        </form>
      </div>
    </div>
  );
}

function messageToSend(message: ServiceBusMessage): SendMessage {
  return {
    body: message.body,
    subject: message.subject,
    contentType: message.systemProperties.contentType,
    correlationId: message.systemProperties.correlationId,
    partitionKey: message.systemProperties.partitionKey,
    replyTo: message.systemProperties.replyTo,
    sessionId: message.systemProperties.sessionId,
    to: message.systemProperties.to,
    applicationProperties: message.applicationProperties,
  };
}

function nextSequence(messages: ServiceBusMessage[]) {
  return messages.length
    ? Math.max(...messages.map((message) => message.systemProperties.sequenceNumber)) + 1
    : undefined;
}

function prettyBody(value: string) {
  try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; }
}

function formatDate(value?: string) {
  if (!value || value.startsWith("0001-")) return "—";
  return new Date(value).toLocaleString();
}

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
}
