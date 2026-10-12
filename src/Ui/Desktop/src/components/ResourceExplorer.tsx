import { createContext, useContext, useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ChevronDown,
  ChevronRight,
  ChevronsDownUp,
  ChevronsUpDown,
  CircleDot,
  Folder,
  Layers3,
  ListTree,
  RadioTower,
  RefreshCw,
  Search,
  Plus,
  Radar,
  Timer,
} from "lucide-react";
import { api } from "../api";
import { formatCount } from "../format";
import type { Connection, QueueDetails, ResourceSelection, SubscriptionDetails, TopicStructure } from "../types";
import { syncQueryData, useAutoRefresh } from "./AutoRefresh";

const AUTO_REFRESH_OPTIONS = [
  { label: "15s", value: 15_000 },
  { label: "30s", value: 30_000 },
  { label: "1m", value: 60_000 },
  { label: "5m", value: 300_000 },
];
const DEFAULT_AUTO_REFRESH_MS = 30_000;

interface Props {
  connections: Connection[];
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  onCreateEntity: (kind: "queue" | "topic", connectionName: string) => void;
}

interface TreeCommand {
  id: number;
  action: "collapse" | "expand";
}

const TreeCommandContext = createContext<TreeCommand>({ id: 0, action: "collapse" });

// respondsToExpand stays false for levels whose expansion triggers a Service Bus request.
function useTreeExpansion(initial: boolean, respondsToExpand = true) {
  const command = useContext(TreeCommandContext);
  const [expanded, setExpanded] = useState(initial);

  useEffect(() => {
    if (command.id === 0) return;
    if (command.action === "collapse") setExpanded(false);
    else if (respondsToExpand) setExpanded(true);
  }, [command, respondsToExpand]);

  return [expanded, setExpanded] as const;
}

export function ResourceExplorer({ connections, selection, onSelect, onCreateEntity }: Props) {
  const [filter, setFilter] = useState("");
  const [command, setCommand] = useState<TreeCommand>({ id: 0, action: "collapse" });

  function broadcast(action: TreeCommand["action"]) {
    setCommand((value) => ({ id: value.id + 1, action }));
  }

  return (
    <aside className="resource-panel">
      <div className="panel-heading">
        <div>
          <span className="eyebrow">Workspace</span>
          <h2>Explorer</h2>
        </div>
        <div className="panel-heading-actions">
          <button
            className="icon-button"
            onClick={() => broadcast("expand")}
            title="Expand all folders and connections"
            aria-label="Expand all folders and connections"
          >
            <ChevronsUpDown size={16} />
          </button>
          <button
            className="icon-button"
            onClick={() => broadcast("collapse")}
            title="Collapse all"
            aria-label="Collapse all"
          >
            <ChevronsDownUp size={16} />
          </button>
        </div>
      </div>
      <label className="search-box">
        <Search size={15} />
        <input
          aria-label="Filter resources"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          placeholder="Filter resources"
        />
      </label>
      <TreeCommandContext.Provider value={command}>
        <nav className="resource-tree" aria-label="Service Bus resources">
          {connections.length === 0 ? (
            <div className="empty-tree">Add a connection to start exploring.</div>
          ) : (
            groupConnections(connections).map(([folder, items]) => (
              <ConnectionFolder
                key={folder || "__default"}
                label={folder || "Default"}
                connections={items}
                filter={filter}
                selection={selection}
                onSelect={onSelect}
                onCreateEntity={onCreateEntity}
              />
            ))
          )}
        </nav>
      </TreeCommandContext.Provider>
    </aside>
  );
}

function ConnectionFolder({
  label,
  connections,
  filter,
  selection,
  onSelect,
  onCreateEntity,
}: {
  label: string;
  connections: Connection[];
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  onCreateEntity: (kind: "queue" | "topic", connectionName: string) => void;
}) {
  const [expanded, setExpanded] = useTreeExpansion(true);

  return (
    <section className="connection-folder">
      <button
        className="connection-folder-label"
        onClick={() => setExpanded((value) => !value)}
        aria-expanded={expanded}
      >
        {expanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
        <Folder size={12} />
        <span>{label}</span>
      </button>
      {expanded &&
        connections.map((connection) => (
          <ConnectionTree
            key={connection.name}
            connection={connection}
            filter={filter}
            selection={selection}
            onSelect={onSelect}
            onCreateEntity={onCreateEntity}
          />
        ))}
    </section>
  );
}

function ConnectionTree({
  connection,
  filter,
  selection,
  onSelect,
  onCreateEntity,
}: {
  connection: Connection;
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  onCreateEntity: (kind: "queue" | "topic", connectionName: string) => void;
}) {
  const [expanded, setExpanded] = useTreeExpansion(true);
  const [queuesExpanded, setQueuesExpanded] = useTreeExpansion(false, false);
  const [topicsExpanded, setTopicsExpanded] = useTreeExpansion(false, false);
  const [autoRefreshMs, setAutoRefreshMs] = useAutoRefresh(connection.name);
  // Polling only runs for branches the user opened, and pauses while the window is hidden.
  const refetchInterval: number | false = autoRefreshMs > 0 ? autoRefreshMs : false;
  const queues = useQuery({
    queryKey: ["queues", connection.name],
    queryFn: () => api.queues(connection.name),
    enabled: queuesExpanded,
    refetchInterval,
    refetchIntervalInBackground: false,
  });
  const topics = useQuery({
    queryKey: ["topics", connection.name],
    queryFn: () => api.topics(connection.name),
    enabled: topicsExpanded,
    refetchInterval,
    refetchIntervalInBackground: false,
  });
  const queryClient = useQueryClient();
  useEffect(() => {
    for (const queue of queues.data ?? []) {
      syncQueryData<QueueDetails>(queryClient, ["queue", connection.name, queue.name], queues.dataUpdatedAt, (details) => ({ ...details, info: queue }));
    }
  }, [queryClient, connection.name, queues.data, queues.dataUpdatedAt]);
  const normalizedFilter = filter.trim().toLocaleLowerCase();

  return (
    <div className="tree-connection">
      <div className="tree-row connection-row">
        <button
          className="tree-expander"
          onClick={() => setExpanded((value) => !value)}
          aria-label={expanded ? "Collapse connection" : "Expand connection"}
        >
          {expanded ? <ChevronDown size={15} /> : <ChevronRight size={15} />}
        </button>
        <button
          className={`tree-label ${selection?.kind === "connection" && selection.connectionName === connection.name ? "active" : ""}`}
          onClick={() => onSelect({ kind: "connection", connectionName: connection.name })}
        >
          <RadioTower size={15} />
          <span className="entity-name" title={connection.name}>{connection.name}</span>
        </button>
        <span className="tree-row-actions">
          {autoRefreshMs > 0 && (
            <select
              className="auto-refresh-interval"
              value={autoRefreshMs}
              onChange={(event) => setAutoRefreshMs(Number(event.target.value))}
              aria-label={`Auto-refresh interval for ${connection.name}`}
            >
              {AUTO_REFRESH_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          )}
          <button
            className={`tree-refresh ${autoRefreshMs > 0 ? "active" : ""}`}
            onClick={() => setAutoRefreshMs(autoRefreshMs > 0 ? 0 : DEFAULT_AUTO_REFRESH_MS)}
            title={autoRefreshMs > 0 ? "Turn off auto-refresh" : "Auto-refresh message counts"}
            aria-pressed={autoRefreshMs > 0}
            aria-label={`Auto-refresh ${connection.name}`}
          >
            <Timer size={13} />
          </button>
        </span>
      </div>

      {expanded && (
        <div className="tree-children">
          <div className="tree-group">
            <div className="tree-row group-row">
              <button
                className={`tree-label ${selection?.kind === "tracker" && selection.connectionName === connection.name ? "active" : ""}`}
                onClick={() => onSelect({ kind: "tracker", connectionName: connection.name })}
              >
                <span className="tree-spacer" />
                <Radar size={15} />
                <span>Live tracker</span>
              </button>
            </div>
          </div>
          <ResourceGroup
            label="Queues"
            icon={<ListTree size={15} />}
            expanded={queuesExpanded}
            loading={queues.isFetching}
            onToggle={() => setQueuesExpanded((value) => !value)}
            onRefresh={() => queues.refetch()}
            onCreate={() => onCreateEntity("queue", connection.name)}
          >
            {queues.error && <TreeError message={(queues.error as Error).message} />}
            {queues.data
              ?.filter((queue) => queue.name.toLocaleLowerCase().includes(normalizedFilter))
              .map((queue) => (
                <button
                  key={queue.name}
                  className={`entity-row ${selection?.kind === "queue" && selection.connectionName === connection.name && selection.name === queue.name ? "active" : ""}`}
                  onClick={() =>
                    onSelect({
                      kind: "queue",
                      connectionName: connection.name,
                      name: queue.name,
                    })
                  }
                >
                  <CircleDot size={13} />
                  <span className="entity-name" title={queue.name}>{queue.name}</span>
                  <span className="count-badge" title="Active messages">
                    {formatCount(queue.activeMessagesCount)}
                  </span>
                  {queue.deadLetterMessagesCount > 0 && (
                    <span className="count-badge danger" title="Dead-letter messages">
                      {formatCount(queue.deadLetterMessagesCount)}
                    </span>
                  )}
                </button>
              ))}
          </ResourceGroup>

          <ResourceGroup
            label="Topics"
            icon={<Folder size={15} />}
            expanded={topicsExpanded}
            loading={topics.isFetching}
            onToggle={() => setTopicsExpanded((value) => !value)}
            onRefresh={() => topics.refetch()}
            onCreate={() => onCreateEntity("topic", connection.name)}
          >
            {topics.error && <TreeError message={(topics.error as Error).message} />}
            <TopicTreeItems
              folder={buildTopicTree(flattenTopics(topics.data ?? []).filter((name) => name.toLocaleLowerCase().includes(normalizedFilter)))}
              connectionName={connection.name}
              filter={normalizedFilter}
              selection={selection}
              onSelect={onSelect}
              autoRefreshMs={autoRefreshMs}
            />
          </ResourceGroup>
        </div>
      )}
    </div>
  );
}

function ResourceGroup({
  label,
  icon,
  expanded,
  loading,
  onToggle,
  onRefresh,
  onCreate,
  children,
}: {
  label: string;
  icon: React.ReactNode;
  expanded: boolean;
  loading: boolean;
  onToggle: () => void;
  onRefresh: () => void;
  onCreate?: () => void;
  children: React.ReactNode;
}) {
  return (
    <div className="tree-group">
      <div className="tree-row group-row">
        <button className="tree-label" onClick={onToggle}>
          {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          {icon}
          <span>{label}</span>
        </button>
        {expanded && (
          <span className="tree-group-actions">
            {onCreate && <button className="tree-refresh" onClick={onCreate} aria-label={`Create ${label}`}><Plus size={13} /></button>}
            <button className={`tree-refresh ${loading ? "spinning" : ""}`} onClick={onRefresh} aria-label={`Refresh ${label}`}><RefreshCw size={13} /></button>
          </span>
        )}
      </div>
      {expanded && <div className="entity-list">{children}</div>}
    </div>
  );
}

function flattenTopics(topics: TopicStructure[]): string[] {
  return topics
    .flatMap((topic) => (topic.isFolder ? flattenTopics(topic.childTopics) : [topic.fullName ?? topic.name]))
    .sort((left, right) => left.localeCompare(right));
}

interface TopicFolderNode {
  label: string;
  folders: TopicFolderNode[];
  topics: string[];
}

function buildTopicTree(names: string[]): TopicFolderNode {
  const root: TopicFolderNode = { label: "", folders: [], topics: [] };
  for (const name of names) {
    let node = root;
    for (const segment of name.split("/").slice(0, -1)) {
      let child = node.folders.find((folder) => folder.label === segment);
      if (!child) {
        child = { label: segment, folders: [], topics: [] };
        node.folders.push(child);
      }
      node = child;
    }
    node.topics.push(name);
  }
  return compactFolder(root);
}

// Merges single-child folder chains like VS Code's compact folders.
function compactFolder(folder: TopicFolderNode): TopicFolderNode {
  let current = folder;
  while (current.label && current.topics.length === 0 && current.folders.length === 1) {
    const child = current.folders[0];
    current = { label: `${current.label}/${child.label}`, folders: child.folders, topics: child.topics };
  }
  return {
    ...current,
    folders: current.folders.map(compactFolder).sort((left, right) => left.label.localeCompare(right.label)),
  };
}

function countTopics(folder: TopicFolderNode): number {
  return folder.topics.length + folder.folders.reduce((sum, child) => sum + countTopics(child), 0);
}

interface TopicItemProps {
  connectionName: string;
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  autoRefreshMs: number;
}

function TopicTreeItems({ folder, ...props }: TopicItemProps & { folder: TopicFolderNode }) {
  return (
    <>
      {folder.folders.map((child) => <TopicFolder key={child.label} folder={child} {...props} />)}
      {folder.topics.map((name) => <TopicLeaf key={name} name={name} {...props} />)}
    </>
  );
}

function TopicFolder({ folder, ...props }: TopicItemProps & { folder: TopicFolderNode }) {
  const [expanded, setExpanded] = useTreeExpansion(false);
  const open = expanded || props.filter.length > 0;
  const toggle = () => setExpanded((value) => !value);

  return (
    <div>
      <div className="tree-row topic-row">
        <button className="tree-expander" onClick={toggle} aria-label={open ? "Collapse folder" : "Expand folder"}>
          {open ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
        </button>
        <button className="entity-row" onClick={toggle} aria-expanded={open}>
          <Folder size={13} />
          <span className="entity-name" title={folder.label}>{folder.label}</span>
          <span className="count-badge" title="Topics">{formatCount(countTopics(folder))}</span>
        </button>
      </div>
      {open && <div className="nested-topics"><TopicTreeItems folder={folder} {...props} /></div>}
    </div>
  );
}

function TopicLeaf({
  name,
  connectionName,
  filter,
  selection,
  onSelect,
  autoRefreshMs,
}: {
  name: string;
  connectionName: string;
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  autoRefreshMs: number;
}) {
  const [expanded, setExpanded] = useTreeExpansion(false, false);
  const label = name.slice(name.lastIndexOf("/") + 1);
  const subscriptions = useQuery({
    queryKey: ["subscriptions", connectionName, name],
    queryFn: () => api.subscriptions(connectionName, name),
    enabled: expanded,
    refetchInterval: autoRefreshMs > 0 ? autoRefreshMs : false,
    refetchIntervalInBackground: false,
  });
  const queryClient = useQueryClient();
  useEffect(() => {
    for (const item of subscriptions.data ?? []) {
      syncQueryData<SubscriptionDetails>(queryClient, ["subscription", connectionName, name, item.subscriptionName], subscriptions.dataUpdatedAt, (details) => ({ ...details, info: item }));
    }
  }, [queryClient, connectionName, name, subscriptions.data, subscriptions.dataUpdatedAt]);

  return (
    <div>
      <div className="tree-row topic-row">
        <button
          className="tree-expander"
          onClick={() => setExpanded((value) => !value)}
          aria-label={expanded ? "Collapse subscriptions" : "Expand subscriptions"}
        >
          {expanded ? <ChevronDown size={13} /> : <ChevronRight size={13} />}
        </button>
        <button
          className={`entity-row ${selection?.kind === "topic" && selection.connectionName === connectionName && selection.name === name ? "active" : ""}`}
          onClick={() => onSelect({ kind: "topic", connectionName, name })}
        >
          <CircleDot size={13} />
          <span className="entity-name" title={name}>{label}</span>
        </button>
      </div>
      {expanded && (
        <div className="subscription-list">
          {subscriptions.isFetching && !subscriptions.data && <div className="tree-loading">Loading subscriptions…</div>}
          {subscriptions.error && <TreeError message={subscriptions.error.message} />}
          {subscriptions.data
            ?.filter((item) => item.subscriptionName.toLocaleLowerCase().includes(filter))
            .map((item) => (
              <button
                key={item.subscriptionName}
                className={`entity-row ${selection?.kind === "subscription" && selection.connectionName === connectionName && selection.topicName === name && selection.name === item.subscriptionName ? "active" : ""}`}
                onClick={() => onSelect({
                  kind: "subscription",
                  connectionName,
                  topicName: name,
                  name: item.subscriptionName,
                })}
              >
                <Layers3 size={12} />
                <span className="entity-name" title={item.subscriptionName}>{item.subscriptionName}</span>
                <span className="count-badge">{formatCount(item.activeMessagesCount)}</span>
                {item.deadLetterMessagesCount > 0 && (
                  <span className="count-badge danger">{formatCount(item.deadLetterMessagesCount)}</span>
                )}
              </button>
            ))}
        </div>
      )}
    </div>
  );
}

function TreeError({ message }: { message: string }) {
  return <div className="tree-error" title={message}>Unable to load resources</div>;
}

function groupConnections(connections: Connection[]) {
  const groups = new Map<string, Connection[]>();
  for (const connection of connections) {
    const folder = connection.folder.trim();
    groups.set(folder, [...(groups.get(folder) ?? []), connection]);
  }

  return [...groups.entries()]
    .sort(([left], [right]) => left === "" ? -1 : right === "" ? 1 : left.localeCompare(right))
    .map(([folder, items]) => [folder, items.sort((left, right) => left.name.localeCompare(right.name))] as const);
}
