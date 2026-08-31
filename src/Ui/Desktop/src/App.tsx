import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Activity,
  BusFront,
  Cable,
  FolderTree,
  Moon,
  Plus,
  Settings,
  Sun,
} from "lucide-react";
import { api } from "./api";
import { ConnectionDialog } from "./components/ConnectionDialog";
import { ConnectionOverview } from "./components/ConnectionOverview";
import { JobsPanel } from "./components/JobsPanel";
import { QueueOverview } from "./components/QueueOverview";
import { ResourceExplorer } from "./components/ResourceExplorer";
import { SubscriptionOverview } from "./components/SubscriptionOverview";
import { TopicOverview } from "./components/TopicOverview";
import type { Connection, ResourceSelection, SaveConnection } from "./types";

const EXPLORER_MIN_WIDTH = 245;
const EXPLORER_DEFAULT_WIDTH = 310;
const EXPLORER_ABSOLUTE_MAX_WIDTH = 720;
const MAIN_CONTENT_MIN_WIDTH = 480;
const EXPLORER_WIDTH_STORAGE_KEY = "explorer-width";

export function App() {
  const queryClient = useQueryClient();
  const [connectionDialogOpen, setConnectionDialogOpen] = useState(false);
  const [editingConnection, setEditingConnection] = useState<Connection>();
  const [jobsOpen, setJobsOpen] = useState(false);
  const [selection, setSelection] = useState<ResourceSelection>();
  const [dark, setDark] = useState(() => localStorage.getItem("theme") !== "light");
  const workspaceRef = useRef<HTMLDivElement>(null);
  const resizeStartRef = useRef<{ x: number; width: number } | undefined>(undefined);
  const [resizingExplorer, setResizingExplorer] = useState(false);
  const [explorerMaxWidth, setExplorerMaxWidth] = useState(EXPLORER_ABSOLUTE_MAX_WIDTH);
  const [explorerWidth, setExplorerWidth] = useState(() => {
    const storedWidth = Number.parseInt(localStorage.getItem(EXPLORER_WIDTH_STORAGE_KEY) ?? "", 10);
    return Number.isFinite(storedWidth)
      ? Math.min(EXPLORER_ABSOLUTE_MAX_WIDTH, Math.max(EXPLORER_MIN_WIDTH, storedWidth))
      : EXPLORER_DEFAULT_WIDTH;
  });
  const connections = useQuery({ queryKey: ["connections"], queryFn: api.connections });
  const saveConnection = useMutation({
    mutationFn: (connection: SaveConnection) => api.saveConnection(cleanConnection(connection)),
    onSuccess: (saved) => {
      queryClient.invalidateQueries({ queryKey: ["connections"] });
      setSelection({ kind: "connection", connectionName: saved.name });
      setConnectionDialogOpen(false);
      setEditingConnection(undefined);
    },
  });
  const createEntity = useMutation({
    mutationFn: async ({ kind, connectionName, name }: { kind: "queue" | "topic"; connectionName: string; name: string }) => {
      const created = kind === "queue"
        ? await api.createQueue(connectionName, { name })
        : await api.createTopic(connectionName, { name });
      return { name: created.data?.info.name };
    },
    onSuccess: (result, request) => {
      queryClient.invalidateQueries({ queryKey: [request.kind === "queue" ? "queues" : "topics", request.connectionName] });
      if (result.name) {
        setSelection({ kind: request.kind, connectionName: request.connectionName, name: result.name });
      }
    },
  });

  useEffect(() => {
    document.documentElement.dataset.theme = dark ? "dark" : "light";
    localStorage.setItem("theme", dark ? "dark" : "light");
  }, [dark]);

  useEffect(() => {
    localStorage.setItem(EXPLORER_WIDTH_STORAGE_KEY, explorerWidth.toString());
  }, [explorerWidth]);

  useEffect(() => {
    const workspace = workspaceRef.current;
    if (!workspace) return;

    const updateBounds = () => {
      const maxWidth = Math.max(
        EXPLORER_MIN_WIDTH,
        Math.min(EXPLORER_ABSOLUTE_MAX_WIDTH, workspace.clientWidth - MAIN_CONTENT_MIN_WIDTH),
      );
      setExplorerMaxWidth(maxWidth);
      setExplorerWidth((width) => Math.min(width, maxWidth));
    };

    updateBounds();
    const observer = new ResizeObserver(updateBounds);
    observer.observe(workspace);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    if (!selection && connections.data?.length) {
      setSelection({ kind: "connection", connectionName: connections.data[0].name });
    }
  }, [connections.data, selection]);

  const selectedConnection = useMemo(
    () => connections.data?.find((item) => item.name === selection?.connectionName),
    [connections.data, selection],
  );

  return (
    <div className="app-shell">
      <nav className="activity-rail" aria-label="Main navigation">
        <div className="brand-mark" title="Cross Bus Explorer"><BusFront size={23} /></div>
        <button className="rail-button active" title="Explorer" aria-label="Explorer"><FolderTree size={20} /></button>
        <button className="rail-button" title="Connections" aria-label="Connections" onClick={() => { setEditingConnection(undefined); setConnectionDialogOpen(true); }}><Cable size={20} /></button>
        <button className={`rail-button ${jobsOpen ? "active" : ""}`} title="Operations" aria-label="Operations" onClick={() => setJobsOpen((value) => !value)}><Activity size={20} /></button>
        <div className="rail-spacer" />
        <button className="rail-button" title={dark ? "Use light theme" : "Use dark theme"} aria-label={dark ? "Use light theme" : "Use dark theme"} onClick={() => setDark((value) => !value)}>{dark ? <Sun size={19} /> : <Moon size={19} />}</button>
        <button className="rail-button" title="Settings" aria-label="Settings"><Settings size={19} /></button>
      </nav>

      <div
        className={`workspace ${resizingExplorer ? "is-resizing" : ""}`}
        ref={workspaceRef}
        style={{ gridTemplateColumns: `${explorerWidth}px 6px minmax(0, 1fr)` }}
      >
        <ResourceExplorer
          connections={connections.data ?? []}
          selection={selection}
          onSelect={setSelection}
          onCreateEntity={(kind, connectionName) => {
            const name = window.prompt(`New ${kind} name`)?.trim();
            if (name) createEntity.mutate({ kind, connectionName, name });
          }}
        />

        <div
          className="resource-resizer"
          role="separator"
          aria-label="Resize resource explorer"
          aria-orientation="vertical"
          aria-valuemin={EXPLORER_MIN_WIDTH}
          aria-valuemax={explorerMaxWidth}
          aria-valuenow={explorerWidth}
          tabIndex={0}
          title="Drag to resize · Double-click to reset"
          onDoubleClick={() => setExplorerWidth(Math.min(EXPLORER_DEFAULT_WIDTH, explorerMaxWidth))}
          onKeyDown={(event) => {
            let nextWidth: number | undefined;
            if (event.key === "ArrowLeft") nextWidth = explorerWidth - 16;
            if (event.key === "ArrowRight") nextWidth = explorerWidth + 16;
            if (event.key === "Home") nextWidth = EXPLORER_MIN_WIDTH;
            if (event.key === "End") nextWidth = explorerMaxWidth;
            if (nextWidth === undefined) return;
            event.preventDefault();
            setExplorerWidth(Math.min(explorerMaxWidth, Math.max(EXPLORER_MIN_WIDTH, nextWidth)));
          }}
          onPointerDown={(event) => {
            if (event.button !== 0) return;
            event.preventDefault();
            event.currentTarget.setPointerCapture(event.pointerId);
            resizeStartRef.current = { x: event.clientX, width: explorerWidth };
            setResizingExplorer(true);
          }}
          onPointerMove={(event) => {
            const start = resizeStartRef.current;
            if (!start) return;
            const nextWidth = start.width + event.clientX - start.x;
            setExplorerWidth(Math.min(explorerMaxWidth, Math.max(EXPLORER_MIN_WIDTH, nextWidth)));
          }}
          onPointerUp={(event) => {
            if (!resizeStartRef.current) return;
            resizeStartRef.current = undefined;
            setResizingExplorer(false);
            event.currentTarget.releasePointerCapture(event.pointerId);
          }}
          onPointerCancel={() => {
            resizeStartRef.current = undefined;
            setResizingExplorer(false);
          }}
        >
          <span />
        </div>

        <main className="main-content">
          {connections.isPending ? (
            <WelcomeState title="Opening workspace" detail="Loading connection profiles…" />
          ) : connections.error ? (
            <WelcomeState title="The backend is unavailable" detail={connections.error.message} error />
          ) : !selection || !selectedConnection ? (
            <WelcomeState
              title="Connect to Azure Service Bus"
              detail="Add a namespace using Azure CLI, workload identity, interactive login, or a connection string."
              action={<button className="button primary" onClick={() => { setEditingConnection(undefined); setConnectionDialogOpen(true); }}><Plus size={17} /> Add connection</button>}
            />
          ) : selection.kind === "connection" ? (
            <ConnectionOverview connection={selectedConnection} onEdit={() => { setEditingConnection(selectedConnection); setConnectionDialogOpen(true); }} onDeleted={() => setSelection(undefined)} />
          ) : selection.kind === "queue" ? (
            <QueueOverview
              connectionName={selection.connectionName}
              queueName={selection.name}
              onDeleted={() => setSelection({ kind: "connection", connectionName: selection.connectionName })}
              onJobStarted={() => setJobsOpen(true)}
            />
          ) : selection.kind === "topic" ? (
            <TopicOverview
              connectionName={selection.connectionName}
              topicName={selection.name}
              onDeleted={() => setSelection({ kind: "connection", connectionName: selection.connectionName })}
              onSelectSubscription={(name) => setSelection({ kind: "subscription", connectionName: selection.connectionName, topicName: selection.name, name })}
            />
          ) : (
            <SubscriptionOverview
              connectionName={selection.connectionName}
              topicName={selection.topicName}
              subscriptionName={selection.name}
              onDeleted={() => setSelection({ kind: "topic", connectionName: selection.connectionName, name: selection.topicName })}
              onJobStarted={() => setJobsOpen(true)}
            />
          )}
        </main>
      </div>

      <button className="floating-add" onClick={() => { setEditingConnection(undefined); setConnectionDialogOpen(true); }} title="Add connection" aria-label="Add connection"><Plus size={20} /></button>
      <JobsPanel open={jobsOpen} onClose={() => setJobsOpen(false)} />
      <ConnectionDialog
        open={connectionDialogOpen}
        busy={saveConnection.isPending}
        error={saveConnection.error?.message}
        initial={editingConnection}
        onClose={() => { setConnectionDialogOpen(false); setEditingConnection(undefined); }}
        onSave={(connection) => saveConnection.mutate(connection)}
      />
      {createEntity.error && <div className="global-error">{createEntity.error.message}</div>}
    </div>
  );
}

function WelcomeState({ title, detail, error = false, action }: { title: string; detail: string; error?: boolean; action?: React.ReactNode }) {
  return <div className={`welcome-state ${error ? "error" : ""}`}><div className="welcome-orbit"><BusFront size={32} /></div><span className="eyebrow">Cross Bus Explorer</span><h1>{title}</h1><p>{detail}</p>{action}</div>;
}

function cleanConnection(connection: SaveConnection): SaveConnection {
  return Object.fromEntries(
    Object.entries(connection).map(([key, value]) => [key, typeof value === "string" ? value.trim() || undefined : value]),
  ) as unknown as SaveConnection;
}
