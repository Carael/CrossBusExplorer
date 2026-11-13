import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  ArrowDown,
  ArrowDownToLine,
  ArrowRightLeft,
  ArrowUp,
  ArrowUpDown,
  CopyPlus,
  Eye,
  FileDown,
  FileUp,
  Filter,
  MessageSquarePlus,
  Play,
  RefreshCw,
  RotateCcw,
  Send,
  Trash2,
  X,
} from "lucide-react";
import { api } from "../api";
import { formatCount } from "../format";
import { useDialogs } from "./Dialogs";
import type {
  ReceiveMode,
  ReceiveType,
  SendMessage,
  ServiceBusMessage,
  SubQueue,
  UploadFileType,
} from "../types";

const MAX_COUNT = 5000;

type SortKey = "id" | "sequence" | "subject" | "enqueued" | "expires";
type Sort = { key: SortKey; direction: "asc" | "desc" };

const sortValue: Record<SortKey, (message: ServiceBusMessage) => string | number> = {
  id: (message) => message.id,
  sequence: (message) => message.systemProperties.sequenceNumber,
  subject: (message) => message.subject ?? "",
  enqueued: (message) => message.systemProperties.enqueuedTime ?? "",
  expires: (message) => message.systemProperties.expiresAt ?? "",
};

interface Props {
  connectionId: string;
  entityName: string;
  subscriptionName?: string;
  activeCount: number;
  deadLetterCount: number;
  transferDeadLetterCount?: number;
  onJobStarted?: () => void;
}

export function MessagesWorkspace({
  connectionId,
  entityName,
  subscriptionName,
  activeCount,
  deadLetterCount,
  transferDeadLetterCount = 0,
  onJobStarted,
}: Props) {
  const queryClient = useQueryClient();
  const dialogs = useDialogs();
  const fileInput = useRef<HTMLInputElement>(null);
  const [subQueue, setSubQueue] = useState<SubQueue>("None");
  const [mode, setMode] = useState<"PeekLock" | "ReceiveAndDelete">("PeekLock");
  const [receiveType, setReceiveType] = useState<ReceiveType>("ByCount");
  const [count, setCount] = useState(25);
  const [fromSequence, setFromSequence] = useState("");
  const [messages, setMessages] = useState<ServiceBusMessage[]>([]);
  const [contentFilter, setContentFilter] = useState("");
  const [headerFilter, setHeaderFilter] = useState("");
  const [sort, setSort] = useState<Sort>({ key: "sequence", direction: "asc" });
  const [selected, setSelected] = useState<ServiceBusMessage>();
  const [composer, setComposer] = useState<SendMessage>();
  const [moveDialog, setMoveDialog] = useState<
    | { kind: "resend-all" }
    | { kind: "move-one"; message: ServiceBusMessage }
    | { kind: "copy-one"; message: ServiceBusMessage }
  >();
  const [exportOpen, setExportOpen] = useState(false);
  const [notice, setNotice] = useState<string>();

  const availableCount = subQueue === "None"
    ? activeCount
    : subQueue === "DeadLetter"
      ? deadLetterCount
      : transferDeadLetterCount;

  const filteredMessages = useMemo(() => {
    return messages.filter((message) =>
      matchesContentFilter(message, contentFilter) &&
      matchesHeaderFilter(message.applicationProperties, headerFilter),
    );
  }, [messages, contentFilter, headerFilter]);

  const sortedMessages = useMemo(() => {
    const value = sortValue[sort.key];
    const factor = sort.direction === "asc" ? 1 : -1;
    return [...filteredMessages].sort((a, b) => {
      const left = value(a);
      const right = value(b);
      return (left < right ? -1 : left > right ? 1 : 0) * factor;
    });
  }, [filteredMessages, sort]);

  function toggleSort(key: SortKey) {
    setSort((current) => ({
      key,
      direction: current.key === key && current.direction === "asc" ? "desc" : "asc",
    }));
  }

  const receive = useMutation({
    mutationFn: (append: boolean) => api.receiveMessages(connectionId, {
      entityName,
      subscriptionName,
      subQueue,
      mode,
      type: receiveType,
      messagesCount: receiveType === "All" ? undefined : count,
      fromSequenceNumber: append
        ? nextSequence(messages)
        : fromSequence
          ? Number(fromSequence)
          : undefined,
    }),
    onSuccess: (result, append) => {
      setMessages((current) => append ? [...current, ...result] : result);
      if (!append && receiveType === "Newest") setSort({ key: "sequence", direction: "desc" });
      setSelected(undefined);
      setNotice(`${append ? "Loaded" : "Received"} ${result.length} message${result.length === 1 ? "" : "s"}.`);
    },
  });

  const send = useMutation({
    mutationFn: ({ message, destination = entityName }: { message: SendMessage; destination?: string }) =>
      api.sendMessages(connectionId, destination, [message]),
    onSuccess: (result, { destination = entityName }) => {
      setComposer(undefined);
      setMoveDialog(undefined);
      setNotice(`Sent ${result.count} message${result.count === 1 ? "" : "s"} to ${destination}.`);
    },
  });

  const move = useMutation({
    mutationFn: async ({
      message,
      destination,
      applicationProperties,
    }: {
      message: ServiceBusMessage;
      destination: string;
      applicationProperties?: Record<string, unknown>;
    }) => {
      if (subQueue === "None" && destination === entityName) {
        throw new Error("Choose a different destination to move an active message.");
      }
      const baseMessage = messageToSend(message);
      const mergedProperties =
        applicationProperties && Object.keys(applicationProperties).length > 0
          ? { ...(baseMessage.applicationProperties ?? {}), ...applicationProperties }
          : baseMessage.applicationProperties;
      await api.sendMessages(connectionId, destination, [
        { ...baseMessage, id: message.id, applicationProperties: mergedProperties },
      ]);
      return api.deleteMessage(
        connectionId,
        entityName,
        subscriptionName,
        subQueue,
        message.systemProperties.sequenceNumber,
      );
    },
    onSuccess: (_, { message, destination }) => {
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
      setMessages((current) => current.filter((item) => item !== message));
      setSelected(undefined);
      setMoveDialog(undefined);
      setNotice(`Sent sequence ${message.systemProperties.sequenceNumber} to ${destination}. Removal from this source is running in Operations.`);
      onJobStarted?.();
    },
  });

  const startJob = useMutation({
    mutationFn: async ({
      action,
      message,
      destination,
      applicationProperties,
    }: {
      action: "purge" | "resend" | "delete";
      message?: ServiceBusMessage;
      destination?: string;
      applicationProperties?: Record<string, unknown>;
    }) => {
      if (action === "delete" && message) {
        return api.deleteMessage(
          connectionId,
          entityName,
          subscriptionName,
          subQueue,
          message.systemProperties.sequenceNumber,
        );
      }
      if (action === "resend") {
        const target = destination?.trim();
        if (!target) throw new Error("A destination is required.");
        if (subQueue === "None" && target === entityName) {
          throw new Error("Choose a different destination to move active messages.");
        }
        return api.resendMessages(
          connectionId,
          entityName,
          subscriptionName,
          subQueue,
          target,
          availableCount,
          applicationProperties && Object.keys(applicationProperties).length > 0
            ? applicationProperties
            : undefined,
        );
      }
      return api.purgeMessages(
        connectionId,
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
      if (action === "resend") {
        setMoveDialog(undefined);
      }
      setNotice("Operation scheduled. Progress is available in Operations.");
      onJobStarted?.();
    },
  });

  async function importFiles(files: FileList | null) {
    if (!files?.length) return;
    try {
      const imported = await Promise.all(
        Array.from(files).map(async (file) => parseImportedMessageContent(await file.text())),
      );
      const result = await api.sendMessages(connectionId, entityName, imported);
      setNotice(`Imported and sent ${result.count} message${result.count === 1 ? "" : "s"}.`);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Unable to import messages.");
    } finally {
      if (fileInput.current) fileInput.current.value = "";
    }
  }

  async function confirmPurge() {
    if (availableCount <= 0) return;
    const confirmed = await dialogs.confirm({
      title: "Purge messages?",
      message: `Up to ${formatCount(availableCount)} messages are permanently removed from this source.`,
      confirmLabel: "Purge",
      danger: true,
    });
    if (confirmed) startJob.mutate({ action: "purge" });
  }

  function requeue(message: ServiceBusMessage) {
    setMoveDialog({ kind: "copy-one", message });
  }

  function moveMessage(message: ServiceBusMessage) {
    setMoveDialog({ kind: "move-one", message });
  }

  const error = receive.error ?? send.error ?? move.error ?? startJob.error;
  const hasActiveFilters = contentFilter.trim().length > 0 || headerFilter.trim().length > 0;

  return (
    <section className="messages-workspace">
      <div className="message-toolbar">
        <div className="segmented-control" aria-label="Message source">
          <SourceButton label="Active" count={activeCount} active={subQueue === "None"} onClick={() => setSubQueue("None")} />
          <SourceButton label="Dead letter" count={deadLetterCount} active={subQueue === "DeadLetter"} danger={deadLetterCount > 0} onClick={() => setSubQueue("DeadLetter")} />
          {transferDeadLetterCount > 0 && (
            <SourceButton label="Transfer DLQ" count={transferDeadLetterCount} active={subQueue === "TransferDeadLetter"} danger={transferDeadLetterCount > 0} onClick={() => setSubQueue("TransferDeadLetter")} />
          )}
        </div>
        <div className="toolbar-actions">
          <button className="button secondary" onClick={() => setComposer({ body: "" })}><MessageSquarePlus size={15} /> New message</button>
          <input ref={fileInput} type="file" multiple hidden onChange={(event) => importFiles(event.target.files)} />
          <button className="button secondary" onClick={() => fileInput.current?.click()}><FileUp size={15} /> Import files</button>
          <button className="button secondary" onClick={() => setExportOpen(true)} disabled={!availableCount && messages.length === 0}><FileDown size={15} /> Export</button>
          <button className="button secondary" onClick={() => setMoveDialog({ kind: "resend-all" })} disabled={!availableCount}><RotateCcw size={15} /> Move all</button>
          <button className="button danger-ghost" onClick={confirmPurge} disabled={!availableCount}><Trash2 size={15} /> Purge</button>
        </div>
      </div>

      <div className="receive-bar">
        <label>Mode<select value={mode} onChange={(event) => { const value = event.target.value as typeof mode; setMode(value); if (value === "ReceiveAndDelete" && receiveType === "Newest") setReceiveType("ByCount"); }}><option value="PeekLock">Peek (safe)</option><option value="ReceiveAndDelete">Receive and delete</option></select></label>
        <label>Receive<select value={receiveType} onChange={(event) => setReceiveType(event.target.value as ReceiveType)}><option value="ByCount">Oldest first</option><option value="Newest" disabled={mode === "ReceiveAndDelete"}>Newest first</option><option value="All">All messages</option></select></label>
        <label>Count<input type="number" min={1} max={MAX_COUNT} disabled={receiveType === "All"} value={count} onChange={(event) => setCount(Math.min(MAX_COUNT, Math.max(1, Number(event.target.value))))} /></label>
        <label>From sequence<input inputMode="numeric" disabled={mode === "ReceiveAndDelete"} value={fromSequence} onChange={(event) => setFromSequence(event.target.value)} placeholder="Beginning" /></label>
        <button className="button primary" onClick={() => receive.mutate(false)} disabled={receive.isPending}><Play size={15} /> {receive.isPending ? "Receiving…" : "Receive"}</button>
        {messages.length > 0 && mode === "PeekLock" && receiveType === "ByCount" && <button className="button secondary" onClick={() => receive.mutate(true)} disabled={receive.isPending}><ArrowDownToLine size={15} /> Peek more</button>}
      </div>

      {messages.length > 0 && (
        <div className="receive-bar filter-bar">
          <label className="grow">
            Content filter (wildcard * / ?)
            <input
              value={contentFilter}
              onChange={(event) => setContentFilter(event.target.value)}
              placeholder="Search body, subject or ID (e.g. *order*123*)"
              aria-label="Filter messages by body, subject or ID"
            />
          </label>
          <label className="grow">
            Custom header filter (ApplicationProperties)
            <input
              value={headerFilter}
              onChange={(event) => setHeaderFilter(event.target.value)}
              placeholder="e.g. tenant=acme*, retryCount=3, or *error*"
              aria-label="Filter messages by custom header"
            />
          </label>
          {hasActiveFilters && (
            <button
              className="button secondary"
              onClick={() => {
                setContentFilter("");
                setHeaderFilter("");
              }}
            >
              <X size={14} /> Clear ({formatCount(filteredMessages.length)}/{formatCount(messages.length)})
            </button>
          )}
        </div>
      )}

      {mode === "ReceiveAndDelete" && <div className="notice warning">Receive and delete removes messages as they are read. This cannot be undone.</div>}
      {notice && <div className="notice success"><span>{notice}</span><button className="notice-close" onClick={() => setNotice(undefined)}><X size={13} /></button></div>}
      {error && <div className="notice error">{error.message}</div>}

      <div className="message-layout">
        <div className="message-table-wrap">
          {messages.length === 0 ? (
            <div className="empty-messages"><RefreshCw size={23} /><strong>No messages loaded</strong><span>Choose a source and receive mode, then load up to {formatCount(MAX_COUNT)} messages or all of them.</span></div>
          ) : sortedMessages.length === 0 ? (
            <div className="empty-messages"><Filter size={23} /><strong>No messages match the current filters</strong><span>Try adjusting the content wildcard or custom header filter.</span></div>
          ) : (
            <table className="data-table message-table">
              <thead><tr><th>Actions</th><SortHeader label="Message ID" column="id" sort={sort} onSort={toggleSort} /><SortHeader label="Sequence" column="sequence" sort={sort} onSort={toggleSort} /><SortHeader label="Subject" column="subject" sort={sort} onSort={toggleSort} /><SortHeader label="Enqueued" column="enqueued" sort={sort} onSort={toggleSort} /><SortHeader label="Expires" column="expires" sort={sort} onSort={toggleSort} /></tr></thead>
              <tbody>
                {sortedMessages.map((message) => (
                  <tr key={`${message.id}-${message.systemProperties.sequenceNumber}`} className={selected === message ? "selected" : ""}>
                    <td className="row-actions">
                      <button title="View" onClick={() => setSelected(message)}><Eye size={14} /></button>
                      <button title="Edit and send a copy" onClick={() => setComposer(messageToSend(message))}><CopyPlus size={14} /></button>
                      <button title="Send a copy to…" onClick={() => requeue(message)}><Send size={14} /></button>
                      <button title="Move to…" onClick={() => moveMessage(message)} disabled={move.isPending}><ArrowRightLeft size={14} /></button>
                      <button title="Delete" onClick={async () => { if (await dialogs.confirm({ title: `Delete sequence ${message.systemProperties.sequenceNumber}?`, message: "The message is permanently removed from this source.", confirmLabel: "Delete", danger: true })) startJob.mutate({ action: "delete", message }); }}><Trash2 size={14} /></button>
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
        {selected && (
          <MessageDetails
            message={selected}
            onClose={() => setSelected(undefined)}
            onEdit={() => {
              const draft = messageToSend(selected);
              setSelected(undefined);
              setComposer(draft);
            }}
            onRequeue={() => requeue(selected)}
            actions={
              <button className="button secondary" onClick={() => moveMessage(selected)} disabled={move.isPending}>
                <ArrowRightLeft size={14} /> Move
              </button>
            }
          />
        )}
      </div>

      {composer && (
        <MessageComposer
          initial={composer}
          defaultDestination={entityName}
          busy={send.isPending}
          error={send.error?.message}
          onClose={() => setComposer(undefined)}
          onSend={(message, destination) => send.mutate({ message, destination })}
        />
      )}

      {moveDialog && (
        <MoveResendDialog
          kind={moveDialog.kind}
          entityName={entityName}
          availableCount={availableCount}
          initialApplicationProperties={
            moveDialog.kind === "resend-all"
              ? {}
              : (moveDialog.message.applicationProperties ?? {})
          }
          busy={send.isPending || move.isPending || startJob.isPending}
          error={(send.error ?? move.error ?? startJob.error)?.message}
          onClose={() => setMoveDialog(undefined)}
          onSubmit={({ destination, applicationProperties }) => {
            if (moveDialog.kind === "resend-all") {
              startJob.mutate({ action: "resend", destination, applicationProperties });
            } else if (moveDialog.kind === "move-one") {
              move.mutate({ message: moveDialog.message, destination, applicationProperties });
            } else {
              const base = messageToSend(moveDialog.message);
              const mergedProperties =
                Object.keys(applicationProperties).length > 0
                  ? { ...(base.applicationProperties ?? {}), ...applicationProperties }
                  : base.applicationProperties;
              send.mutate({
                message: { ...base, applicationProperties: mergedProperties },
                destination,
              });
            }
          }}
        />
      )}

      {exportOpen && (
        <ExportMessagesDialog
          connectionId={connectionId}
          entityName={entityName}
          subscriptionName={subscriptionName}
          defaultSubQueue={subQueue}
          loadedMessages={filteredMessages}
          onClose={() => setExportOpen(false)}
          onExported={(exportedCount, folderPath) => {
            setExportOpen(false);
            setNotice(`Exported ${formatCount(exportedCount)} message${exportedCount === 1 ? "" : "s"} to ${folderPath}.`);
          }}
        />
      )}
    </section>
  );
}

function SourceButton({
  label,
  count,
  active,
  danger,
  onClick,
}: {
  label: string;
  count: number;
  active: boolean;
  danger?: boolean;
  onClick: () => void;
}) {
  return (
    <button className={`${active ? "active" : ""} ${danger ? "danger" : ""}`} onClick={onClick}>
      {label}
      <span className={danger ? "danger-count" : ""}>{formatCount(count)}</span>
    </button>
  );
}

function SortHeader({ label, column, sort, onSort }: { label: string; column: SortKey; sort: Sort; onSort: (key: SortKey) => void }) {
  const active = sort.key === column;
  const Icon = !active ? ArrowUpDown : sort.direction === "asc" ? ArrowUp : ArrowDown;
  return (
    <th aria-sort={active ? (sort.direction === "asc" ? "ascending" : "descending") : "none"}>
      <button className="sort-header" onClick={() => onSort(column)}>{label}<Icon size={12} /></button>
    </th>
  );
}

export function MessageDetails({ message, onClose, onEdit, onRequeue, actions }: { message: ServiceBusMessage; onClose: () => void; onEdit?: () => void; onRequeue?: () => void; actions?: React.ReactNode }) {
  const body = useMemo(() => prettyBody(message.body), [message.body]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      if (document.querySelectorAll(".dialog-backdrop").length > 1) return;
      onClose();
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  return (
    <div className="dialog-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="dialog message-details-dialog"
        role="dialog"
        aria-modal="true"
        aria-label={`Message ${message.subject || message.id}`}
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="dialog-header message-details-header">
          <div className="message-details-title">
            <span className="eyebrow">Message</span>
            <h2>{message.subject || message.id}</h2>
          </div>
          <div className="message-details-header-actions">
            {(onEdit || onRequeue || actions) && (
              <div className="detail-actions">
                {actions}
                {onEdit && <button className="button secondary" onClick={onEdit}><CopyPlus size={14} /> Edit copy</button>}
                {onRequeue && <button className="button primary" onClick={onRequeue}><Send size={14} /> Requeue</button>}
              </div>
            )}
            <button className="icon-button" onClick={onClose} aria-label="Close message details"><X size={18} /></button>
          </div>
        </header>
        <div className="message-details-body">
          <section className="message-details-section">
            <h4>Body</h4>
            <pre className="code-view">{body}</pre>
          </section>
          <section className="message-details-section">
            <h4>System properties</h4>
            <PropertyTable value={{ id: message.id, subject: message.subject, ...message.systemProperties }} />
          </section>
          <section className="message-details-section">
            <h4>Application properties</h4>
            <PropertyTable value={message.applicationProperties ?? {}} />
          </section>
        </div>
      </section>
    </div>
  );
}

function PropertyTable({ value }: { value: Record<string, unknown> }) {
  return <dl className="property-table">{Object.entries(value).map(([key, item]) => <div key={key}><dt>{key}</dt><dd>{formatValue(item)}</dd></div>)}</dl>;
}

export function MessageComposer({
  initial,
  defaultDestination,
  busy,
  error,
  onClose,
  onSend,
}: {
  initial: SendMessage;
  defaultDestination?: string;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSend: (message: SendMessage, destination?: string) => void;
}) {
  const [message, setMessage] = useState(initial);
  const [destination, setDestination] = useState(defaultDestination ?? "");
  const [properties, setProperties] = useState(JSON.stringify(initial.applicationProperties ?? {}, null, 2));
  const [validation, setValidation] = useState<string>();

  function submit(event: React.FormEvent) {
    event.preventDefault();
    try {
      const parsed = JSON.parse(properties);
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
        setValidation("Application properties must be a JSON object.");
        return;
      }
      setValidation(undefined);
      onSend(
        { ...message, applicationProperties: parsed as Record<string, unknown> },
        destination.trim() || defaultDestination,
      );
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
            {defaultDestination !== undefined && (
              <label className="wide">
                <span>Destination queue or topic</span>
                <input
                  required
                  value={destination}
                  onChange={(event) => setDestination(event.target.value)}
                  placeholder={defaultDestination}
                />
              </label>
            )}
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

function MoveResendDialog({
  kind,
  entityName,
  availableCount,
  initialApplicationProperties,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  kind: "resend-all" | "move-one" | "copy-one";
  entityName: string;
  availableCount: number;
  initialApplicationProperties: Record<string, unknown>;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSubmit: (values: { destination: string; applicationProperties: Record<string, unknown> }) => void;
}) {
  const [destination, setDestination] = useState(() => suggestedDestination(entityName));
  const [propertiesText, setPropertiesText] = useState(() =>
    JSON.stringify(initialApplicationProperties, null, 2),
  );
  const [validation, setValidation] = useState<string>();

  const title =
    kind === "resend-all"
      ? "Move all messages"
      : kind === "move-one"
        ? "Move message"
        : "Send copy";
  const description =
    kind === "resend-all"
      ? `Move up to ${formatCount(availableCount)} messages to a queue or topic. Custom application properties are added or overridden on each resent message.`
      : kind === "move-one"
        ? "Send the message to a queue or topic with optional application property overrides and remove it from this source."
        : "Send a copy to a queue or topic with optional application property overrides. The original stays in this source.";
  const confirmLabel = kind === "copy-one" ? "Send" : "Move";

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedDestination = destination.trim();
    if (!trimmedDestination) {
      setValidation("A destination queue or topic is required.");
      return;
    }

    try {
      const parsed = propertiesText.trim() ? JSON.parse(propertiesText) : {};
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
        setValidation("Application properties must be a JSON object.");
        return;
      }
      setValidation(undefined);
      onSubmit({
        destination: trimmedDestination,
        applicationProperties: parsed as Record<string, unknown>,
      });
    } catch {
      setValidation("Application properties must be a valid JSON object.");
    }
  }

  return (
    <div className="dialog-backdrop" role="presentation">
      <div className="dialog" role="dialog" aria-modal="true" aria-label={title}>
        <div className="dialog-header">
          <div>
            <span className="eyebrow">Routing</span>
            <h2>{title}</h2>
          </div>
          <button className="icon-button" onClick={onClose}><X size={18} /></button>
        </div>
        <form onSubmit={handleSubmit}>
          <p className="dialog-copy">{description}</p>
          <div className="form-grid">
            <label className="wide">
              <span>Destination queue or topic</span>
              <input
                autoFocus
                required
                value={destination}
                onChange={(event) => setDestination(event.target.value)}
                placeholder="Queue or topic name"
              />
            </label>
            <label className="wide">
              <span>Application properties (JSON, optional)</span>
              <textarea
                rows={5}
                value={propertiesText}
                onChange={(event) => setPropertiesText(event.target.value)}
                placeholder='{"routingKey": "orders"}'
              />
            </label>
          </div>
          {(validation || error) && <div className="inline-error">{validation ?? error}</div>}
          <div className="dialog-actions">
            <button type="button" className="button secondary" onClick={onClose}>Cancel</button>
            <button className="button primary" disabled={busy}>
              {busy ? "Working…" : confirmLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function ExportMessagesDialog({
  connectionId,
  entityName,
  subscriptionName,
  defaultSubQueue,
  loadedMessages,
  onClose,
  onExported,
}: {
  connectionId: string;
  entityName: string;
  subscriptionName?: string;
  defaultSubQueue: SubQueue;
  loadedMessages: ServiceBusMessage[];
  onClose: () => void;
  onExported: (count: number, folderPath: string) => void;
}) {
  const [source, setSource] = useState<"loaded" | "receive">(
    loadedMessages.length > 0 ? "loaded" : "receive",
  );
  const [subQueue, setSubQueue] = useState<SubQueue>(defaultSubQueue);
  const [mode, setMode] = useState<ReceiveMode>("PeekLock");
  const [receiveType, setReceiveType] = useState<ReceiveType>("ByCount");
  const [count, setCount] = useState(50);
  const [fromSequence, setFromSequence] = useState("");
  const [fileType, setFileType] = useState<UploadFileType>("BodyWithApplicationProperties");
  const [folderPath, setFolderPath] = useState("");

  useEffect(() => {
    let cancelled = false;
    api.defaultExportFolder()
      .then((res) => {
        if (!cancelled && res.folderPath) {
          setFolderPath((current) => current || res.folderPath);
        }
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, []);

  const exportMutation = useMutation({
    mutationFn: () =>
      api.exportMessages(connectionId, {
        entityName,
        subscriptionName,
        folderPath: folderPath.trim(),
        fileType,
        ...(source === "loaded"
          ? { messages: loadedMessages }
          : {
              subQueue,
              mode,
              type: receiveType,
              messagesCount: receiveType === "All" ? undefined : count,
              fromSequenceNumber: fromSequence ? Number(fromSequence) : undefined,
            }),
      }),
    onSuccess: (result) => {
      onExported(result.count, result.folderPath);
    },
  });

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    exportMutation.mutate();
  }

  return (
    <div className="dialog-backdrop" role="presentation">
      <div className="dialog" role="dialog" aria-modal="true" aria-label="Export messages">
        <div className="dialog-header">
          <div>
            <span className="eyebrow">Export</span>
            <h2>Export messages to folder</h2>
          </div>
          <button className="icon-button" onClick={onClose}><X size={18} /></button>
        </div>
        <form onSubmit={handleSubmit}>
          <p className="dialog-copy">
            Writes each message to a separate file in the destination folder.
          </p>
          <div className="form-grid">
            <label className="wide">
              <span>Messages to export</span>
              <select
                value={source}
                onChange={(event) => setSource(event.target.value as "loaded" | "receive")}
              >
                <option value="loaded" disabled={loadedMessages.length === 0}>
                  Currently loaded messages ({formatCount(loadedMessages.length)})
                </option>
                <option value="receive">Receive from Service Bus</option>
              </select>
            </label>

            {source === "receive" && (
              <>
                <label>
                  <span>Sub-queue</span>
                  <select
                    value={subQueue}
                    onChange={(event) => setSubQueue(event.target.value as SubQueue)}
                  >
                    <option value="None">Active</option>
                    <option value="DeadLetter">Dead letter</option>
                    <option value="TransferDeadLetter">Transfer DLQ</option>
                  </select>
                </label>
                <label>
                  <span>Mode</span>
                  <select
                    value={mode}
                    onChange={(event) => {
                      const nextMode = event.target.value as ReceiveMode;
                      setMode(nextMode);
                      if (nextMode === "ReceiveAndDelete" && receiveType === "Newest") {
                        setReceiveType("ByCount");
                      }
                    }}
                  >
                    <option value="PeekLock">Peek only (keep messages)</option>
                    <option value="ReceiveAndDelete">Receive and delete (remove messages)</option>
                  </select>
                </label>
                <label>
                  <span>Order / scope</span>
                  <select
                    value={receiveType}
                    onChange={(event) => setReceiveType(event.target.value as ReceiveType)}
                  >
                    <option value="ByCount">Oldest first (by count)</option>
                    <option value="Newest" disabled={mode === "ReceiveAndDelete"}>
                      Newest first (by count)
                    </option>
                    <option value="All">All messages</option>
                  </select>
                </label>
                <label>
                  <span>Count</span>
                  <input
                    type="number"
                    min={1}
                    max={MAX_COUNT}
                    disabled={receiveType === "All"}
                    value={count}
                    onChange={(event) =>
                      setCount(Math.min(MAX_COUNT, Math.max(1, Number(event.target.value))))
                    }
                  />
                </label>
                <label className="wide">
                  <span>From sequence number</span>
                  <input
                    inputMode="numeric"
                    disabled={mode === "ReceiveAndDelete"}
                    value={fromSequence}
                    onChange={(event) => setFromSequence(event.target.value)}
                    placeholder="Beginning"
                  />
                </label>
              </>
            )}

            <label className="wide">
              <span>Export format</span>
              <select
                value={fileType}
                onChange={(event) => setFileType(event.target.value as UploadFileType)}
              >
                <option value="BodyWithApplicationProperties">
                  CrossBusExplorer format (body &amp; application properties)
                </option>
                <option value="Body">Body only</option>
              </select>
            </label>

            <label className="wide">
              <span>Destination folder path</span>
              <input
                required
                value={folderPath}
                onChange={(event) => setFolderPath(event.target.value)}
                placeholder="/path/to/export/folder"
              />
            </label>
          </div>
          {source === "receive" && mode === "ReceiveAndDelete" && (
            <div className="notice warning">
              Receive and delete removes exported messages from the queue/subscription.
            </div>
          )}
          {exportMutation.error && <div className="inline-error">{exportMutation.error.message}</div>}
          <div className="dialog-actions">
            <button type="button" className="button secondary" onClick={onClose}>Cancel</button>
            <button className="button primary" disabled={exportMutation.isPending}>
              <FileDown size={15} /> {exportMutation.isPending ? "Exporting…" : "Export"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export function parseImportedMessageContent(content: string): SendMessage {
  const trimmed = content.trim();
  if (trimmed.startsWith("{") && trimmed.endsWith("}")) {
    try {
      const parsed = JSON.parse(trimmed) as Record<string, unknown>;
      if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) {
        const format = typeof parsed.format === "string" ? parsed.format : "";
        const isCrossBusFormat =
          format.toLowerCase().startsWith("crossbusexplorer") ||
          "crossBusExplorerFormat" in parsed ||
          (typeof parsed.body === "string" &&
            ("applicationProperties" in parsed || "systemProperties" in parsed));

        if (isCrossBusFormat) {
          return {
            body: typeof parsed.body === "string" ? parsed.body : JSON.stringify(parsed.body ?? ""),
            subject: typeof parsed.subject === "string" ? parsed.subject : undefined,
            to: typeof parsed.to === "string" ? parsed.to : undefined,
            contentType: typeof parsed.contentType === "string" ? parsed.contentType : undefined,
            correlationId: typeof parsed.correlationId === "string" ? parsed.correlationId : undefined,
            id: typeof parsed.id === "string" ? parsed.id : undefined,
            partitionKey: typeof parsed.partitionKey === "string" ? parsed.partitionKey : undefined,
            replyTo: typeof parsed.replyTo === "string" ? parsed.replyTo : undefined,
            sessionId: typeof parsed.sessionId === "string" ? parsed.sessionId : undefined,
            applicationProperties:
              parsed.applicationProperties &&
              typeof parsed.applicationProperties === "object" &&
              !Array.isArray(parsed.applicationProperties)
                ? (parsed.applicationProperties as Record<string, unknown>)
                : undefined,
          };
        }
      }
    } catch {
      // Not JSON or not CrossBusExplorer format; fall back to raw body.
    }
  }

  return { body: content };
}

function matchesWildcard(value: string, rawPattern: string): boolean {
  const pattern = rawPattern.trim();
  if (!pattern) return true;
  if (!pattern.includes("*") && !pattern.includes("?")) {
    return value.toLocaleLowerCase().includes(pattern.toLocaleLowerCase());
  }

  const escaped = pattern
    .replace(/[.+^${}()|[\]\\]/g, "\\$&")
    .replace(/\*/g, "[\\s\\S]*")
    .replace(/\?/g, "[\\s\\S]");
  const regex = new RegExp(`^${escaped}$`, "i");
  return regex.test(value);
}

function matchesContentFilter(message: ServiceBusMessage, rawFilter: string): boolean {
  const filter = rawFilter.trim();
  if (!filter) return true;
  const searchPattern = filter.includes("*") || filter.includes("?") ? filter : `*${filter}*`;
  return (
    matchesWildcard(message.body, searchPattern) ||
    matchesWildcard(message.subject ?? "", searchPattern) ||
    matchesWildcard(message.id, searchPattern)
  );
}

function matchesHeaderFilter(
  applicationProperties: Record<string, unknown> | undefined,
  rawFilter: string,
): boolean {
  const filter = rawFilter.trim();
  if (!filter) return true;
  const entries = Object.entries(applicationProperties ?? {});
  if (entries.length === 0) return false;

  const clauses = filter
    .split(/[,;]/)
    .map((clause) => clause.trim())
    .filter(Boolean);

  return clauses.every((clause) => {
    const separatorIndex = clause.search(/[=:]/);
    if (separatorIndex >= 0) {
      const keyPattern = clause.slice(0, separatorIndex).trim();
      const valuePattern = clause.slice(separatorIndex + 1).trim();
      return entries.some(([key, value]) => {
        const keyMatches = !keyPattern || matchesWildcard(key, keyPattern);
        const valueMatches = !valuePattern || matchesWildcard(formatValue(value), valuePattern);
        return keyMatches && valueMatches;
      });
    }

    return entries.some(
      ([key, value]) =>
        matchesWildcard(key, clause) || matchesWildcard(formatValue(value), clause),
    );
  });
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

// MassTransit-style error/skipped queues map back to their source queue.
function suggestedDestination(entityName: string) {
  return entityName.replace(/_(error|skipped)$/i, "");
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
