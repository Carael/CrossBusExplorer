import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ChevronDown,
  ChevronRight,
  CircleDot,
  Folder,
  Layers3,
  ListTree,
  RadioTower,
  RefreshCw,
  Search,
  Plus,
} from "lucide-react";
import { api } from "../api";
import type { Connection, ResourceSelection, TopicStructure } from "../types";

interface Props {
  connections: Connection[];
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
  onCreateEntity: (kind: "queue" | "topic", connectionName: string) => void;
}

export function ResourceExplorer({ connections, selection, onSelect, onCreateEntity }: Props) {
  const [filter, setFilter] = useState("");

  return (
    <aside className="resource-panel">
      <div className="panel-heading">
        <div>
          <span className="eyebrow">Workspace</span>
          <h2>Explorer</h2>
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
      <nav className="resource-tree" aria-label="Service Bus resources">
        {connections.length === 0 ? (
          <div className="empty-tree">Add a connection to start exploring.</div>
        ) : (
          groupConnections(connections).map(([folder, items]) => (
            <section className="connection-folder" key={folder || "__default"}>
              <div className="connection-folder-label"><Folder size={12} /><span>{folder || "Default"}</span></div>
              {items.map((connection) => (
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
          ))
        )}
      </nav>
    </aside>
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
  const [expanded, setExpanded] = useState(true);
  const [queuesExpanded, setQueuesExpanded] = useState(false);
  const [topicsExpanded, setTopicsExpanded] = useState(false);
  const queues = useQuery({
    queryKey: ["queues", connection.name],
    queryFn: () => api.queues(connection.name),
    enabled: queuesExpanded,
  });
  const topics = useQuery({
    queryKey: ["topics", connection.name],
    queryFn: () => api.topics(connection.name),
    enabled: topicsExpanded,
  });
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
          <span>{connection.name}</span>
        </button>
      </div>

      {expanded && (
        <div className="tree-children">
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
                  <span className="entity-name">{queue.name}</span>
                  <span className="count-badge" title="Active messages">
                    {queue.activeMessagesCount}
                  </span>
                  {queue.deadLetterMessagesCount > 0 && (
                    <span className="count-badge danger" title="Dead-letter messages">
                      {queue.deadLetterMessagesCount}
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
            {topics.data?.map((topic) => (
              <TopicNode
                key={topic.fullName ?? topic.name}
                topic={topic}
                connectionName={connection.name}
                filter={normalizedFilter}
                selection={selection}
                onSelect={onSelect}
              />
            ))}
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

function TopicNode({
  topic,
  connectionName,
  filter,
  selection,
  onSelect,
}: {
  topic: TopicStructure;
  connectionName: string;
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
}) {
  const visible =
    topic.name.toLocaleLowerCase().includes(filter) ||
    topic.childTopics.some((child) => child.name.toLocaleLowerCase().includes(filter));
  if (filter && !visible) return null;

  if (topic.isFolder) {
    return (
      <div className="topic-folder">
        <div className="entity-row folder-label">
          <Folder size={13} />
          <span>{topic.name}</span>
        </div>
        <div className="nested-topics">
          {topic.childTopics.map((child) => (
            <TopicNode
              key={child.fullName ?? child.name}
              topic={child}
              connectionName={connectionName}
              filter={filter}
              selection={selection}
              onSelect={onSelect}
            />
          ))}
        </div>
      </div>
    );
  }

  const name = topic.fullName ?? topic.name;
  return (
    <TopicLeaf
      topic={topic}
      name={name}
      connectionName={connectionName}
      filter={filter}
      selection={selection}
      onSelect={onSelect}
    />
  );
}

function TopicLeaf({
  topic,
  name,
  connectionName,
  filter,
  selection,
  onSelect,
}: {
  topic: TopicStructure;
  name: string;
  connectionName: string;
  filter: string;
  selection?: ResourceSelection;
  onSelect: (selection: ResourceSelection) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const subscriptions = useQuery({
    queryKey: ["subscriptions", connectionName, name],
    queryFn: () => api.subscriptions(connectionName, name),
    enabled: expanded,
  });

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
          <span className="entity-name">{topic.name}</span>
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
                <span className="entity-name">{item.subscriptionName}</span>
                <span className="count-badge">{item.activeMessagesCount}</span>
                {item.deadLetterMessagesCount > 0 && (
                  <span className="count-badge danger">{item.deadLetterMessagesCount}</span>
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
