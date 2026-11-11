import { invoke } from "@tauri-apps/api/core";
import { describeError, logFrontend } from "./diagnostics";
import type {
  BackgroundJob,
  Connection,
  OperationResult,
  QueueDetails,
  QueueInfo,
  BackendLogEntry,
  DesktopLogEntry,
  ReceiveType,
  Rule,
  SaveConnection,
  SendMessage,
  ServiceBusMessage,
  StartTracking,
  SubQueue,
  SubscriptionDetails,
  SubscriptionInfo,
  TopicDetails,
  TopicStructure,
  TrackingSession,
  TrackingUpdates,
} from "./types";

interface BridgeResponse {
  status: number;
  body?: string;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

async function request<T>(
  path: string,
  options: { method?: string; body?: unknown; silent?: boolean } = {},
): Promise<T> {
  const method = options.method ?? "GET";
  let status: number;
  let body: string | undefined;

  const logFailure = (message: string, details?: string) => {
    if (!options.silent) logFrontend("error", `${method} ${path}`, message, details);
  };

  if ("__TAURI_INTERNALS__" in window) {
    try {
      const response = await invoke<BridgeResponse>("backend_request", {
        request: {
          method,
          path,
          body: options.body === undefined ? undefined : JSON.stringify(options.body),
        },
      });
      status = response.status;
      body = response.body;
    } catch (error) {
      // The desktop bridge records its own failures in the Rust diagnostics log.
      throw new ApiError(describeError(error).message, 0);
    }
  } else {
    const baseUrl = import.meta.env.VITE_API_URL ?? "http://127.0.0.1:5055";
    const response = await fetch(`${baseUrl}${path}`, {
      method,
      headers: {
        "Content-Type": "application/json",
        ...(import.meta.env.VITE_API_TOKEN
          ? { Authorization: `Bearer ${import.meta.env.VITE_API_TOKEN}` }
          : {}),
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    }).catch((error: unknown) => {
      const { message, details } = describeError(error);
      logFailure(message, details);
      throw new ApiError(message, 0);
    });
    status = response.status;
    body = await response.text();
  }

  if (status < 200 || status >= 300) {
    let message = `Request failed with status ${status}.`;
    if (body) {
      try {
        const problem = JSON.parse(body) as { title?: string; detail?: string; error?: string };
        message = problem.detail ?? problem.error ?? problem.title ?? message;
      } catch {
        message = body;
      }
    }
    logFailure(message, [`HTTP ${status}`, body].filter(Boolean).join("\n"));
    throw new ApiError(message, status);
  }

  return body ? (JSON.parse(body) as T) : (undefined as T);
}

const segment = encodeURIComponent;

export const api = {
  connections: () => request<Connection[]>("/api/v1/connections/"),
  saveConnection: (connection: SaveConnection) =>
    request<Connection>(`/api/v1/connections/${segment(connection.name)}`, {
      method: "PUT",
      body: connection,
    }),
  deleteConnection: (name: string) =>
    request<void>(`/api/v1/connections/${segment(name)}`, { method: "DELETE" }),
  testConnection: (name: string) =>
    request<{ success: boolean; message: string; duration: string }>(
      `/api/v1/connections/${segment(name)}/test`,
      { method: "POST" },
    ),
  queues: (connectionName: string) =>
    request<QueueInfo[]>(
      `/api/v1/connections/${segment(connectionName)}/queues/`,
    ),
  queue: (connectionName: string, queueName: string) =>
    request<QueueDetails>(
      `/api/v1/connections/${segment(connectionName)}/queues/${segment(queueName)}`,
    ),
  createQueue: (connectionName: string, options: Record<string, unknown>) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionName)}/queues/`,
      { method: "POST", body: options },
    ),
  updateQueue: (connectionName: string, queueName: string, options: Record<string, unknown>) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionName)}/queues/${segment(queueName)}`,
      { method: "PUT", body: options },
    ),
  cloneQueue: (connectionName: string, queueName: string, name: string) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionName)}/queues/${segment(queueName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteQueue: (connectionName: string, queueName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionName)}/queues/${segment(queueName)}`,
      { method: "DELETE" },
    ),
  topics: (connectionName: string) =>
    request<TopicStructure[]>(
      `/api/v1/connections/${segment(connectionName)}/topics/`,
    ),
  topic: (connectionName: string, topicName: string) =>
    request<TopicDetails>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}`,
    ),
  createTopic: (connectionName: string, options: Record<string, unknown>) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/`,
      { method: "POST", body: options },
    ),
  updateTopic: (connectionName: string, topicName: string, options: Record<string, unknown>) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}`,
      { method: "PUT", body: options },
    ),
  cloneTopic: (connectionName: string, topicName: string, name: string) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteTopic: (connectionName: string, topicName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}`,
      { method: "DELETE" },
    ),
  subscriptions: (connectionName: string, topicName: string) =>
    request<SubscriptionInfo[]>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/`,
    ),
  subscription: (connectionName: string, topicName: string, subscriptionName: string) =>
    request<SubscriptionDetails>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
    ),
  createSubscription: (connectionName: string, topicName: string, options: Record<string, unknown>) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/`,
      { method: "POST", body: options },
    ),
  updateSubscription: (connectionName: string, topicName: string, subscriptionName: string, options: Record<string, unknown>) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
      { method: "PUT", body: options },
    ),
  cloneSubscription: (connectionName: string, topicName: string, subscriptionName: string, name: string) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteSubscription: (connectionName: string, topicName: string, subscriptionName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
      { method: "DELETE" },
    ),
  rules: (connectionName: string, topicName: string, subscriptionName: string) =>
    request<Rule[]>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/`,
    ),
  saveRule: (connectionName: string, topicName: string, subscriptionName: string, rule: Rule, originalName?: string) =>
    request<OperationResult<Rule>>(
      originalName
        ? `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/${segment(originalName)}`
        : `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/`,
      { method: originalName ? "PUT" : "POST", body: rule },
    ),
  deleteRule: (connectionName: string, topicName: string, subscriptionName: string, ruleName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionName)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/${segment(ruleName)}`,
      { method: "DELETE" },
    ),
  receiveMessages: (connectionName: string, options: {
    entityName: string;
    subscriptionName?: string;
    subQueue: SubQueue;
    mode: "PeekLock" | "ReceiveAndDelete";
    type: ReceiveType;
    messagesCount?: number;
    fromSequenceNumber?: number;
  }) => request<ServiceBusMessage[]>(
    `/api/v1/connections/${segment(connectionName)}/messages/receive`,
    { method: "POST", body: options },
  ),
  sendMessages: (connectionName: string, entityName: string, messages: SendMessage[]) =>
    request<{ count: number }>(
      `/api/v1/connections/${segment(connectionName)}/messages/send`,
      { method: "POST", body: { entityName, messages } },
    ),
  purgeMessages: (connectionName: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, totalCount: number) =>
    request<BackgroundJob>("/api/v1/jobs/purge", {
      method: "POST",
      body: { connectionName, entityName, subscriptionName, subQueue, totalCount },
    }),
  resendMessages: (connectionName: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, destinationEntityName: string, totalCount: number) =>
    request<BackgroundJob>("/api/v1/jobs/resend", {
      method: "POST",
      body: { connectionName, entityName, subscriptionName, subQueue, destinationEntityName, totalCount },
    }),
  deleteMessage: (connectionName: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, sequenceNumber: number) =>
    request<BackgroundJob>("/api/v1/jobs/delete-message", {
      method: "POST",
      body: { connectionName, entityName, subscriptionName, subQueue, sequenceNumber },
    }),
  jobs: () => request<BackgroundJob[]>("/api/v1/jobs/"),
  cancelJob: (id: string) =>
    request<void>(`/api/v1/jobs/${segment(id)}`, { method: "DELETE" }),
  backendLogs: () =>
    request<BackendLogEntry[]>("/api/v1/diagnostics/logs", { silent: true }),
  clearBackendLogs: () =>
    request<void>("/api/v1/diagnostics/logs", { method: "DELETE", silent: true }),
  desktopLogs: () =>
    "__TAURI_INTERNALS__" in window
      ? invoke<DesktopLogEntry[]>("get_diagnostics")
      : Promise.resolve([] as DesktopLogEntry[]),
  clearDesktopLogs: () =>
    "__TAURI_INTERNALS__" in window ? invoke<void>("clear_diagnostics") : Promise.resolve(),
  trackingSessions: () => request<TrackingSession[]>("/api/v1/tracking/sessions/"),
  startTracking: (options: StartTracking) =>
    request<TrackingSession>("/api/v1/tracking/sessions/", { method: "POST", body: options }),
  trackingUpdates: (id: string, after: number) =>
    request<TrackingUpdates>(`/api/v1/tracking/sessions/${segment(id)}/updates?after=${after}`),
  stopTracking: (id: string) =>
    request<TrackingSession>(`/api/v1/tracking/sessions/${segment(id)}/stop`, { method: "POST" }),
  deleteTracking: (id: string) =>
    request<void>(`/api/v1/tracking/sessions/${segment(id)}`, { method: "DELETE" }),
};
