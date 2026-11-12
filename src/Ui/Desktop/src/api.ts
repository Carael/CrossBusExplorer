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
    request<Connection>(`/api/v1/connections/${segment(connection.id)}`, {
      method: "PUT",
      body: connection,
    }),
  deleteConnection: (id: string) =>
    request<void>(`/api/v1/connections/${segment(id)}`, { method: "DELETE" }),
  testConnection: (id: string) =>
    request<{ success: boolean; message: string; duration: string }>(
      `/api/v1/connections/${segment(id)}/test`,
      { method: "POST" },
    ),
  queues: (connectionId: string) =>
    request<QueueInfo[]>(
      `/api/v1/connections/${segment(connectionId)}/queues/`,
    ),
  queue: (connectionId: string, queueName: string) =>
    request<QueueDetails>(
      `/api/v1/connections/${segment(connectionId)}/queues/${segment(queueName)}`,
    ),
  createQueue: (connectionId: string, options: Record<string, unknown>) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionId)}/queues/`,
      { method: "POST", body: options },
    ),
  updateQueue: (connectionId: string, queueName: string, options: Record<string, unknown>) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionId)}/queues/${segment(queueName)}`,
      { method: "PUT", body: options },
    ),
  cloneQueue: (connectionId: string, queueName: string, name: string) =>
    request<OperationResult<QueueDetails>>(
      `/api/v1/connections/${segment(connectionId)}/queues/${segment(queueName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteQueue: (connectionId: string, queueName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionId)}/queues/${segment(queueName)}`,
      { method: "DELETE" },
    ),
  topics: (connectionId: string) =>
    request<TopicStructure[]>(
      `/api/v1/connections/${segment(connectionId)}/topics/`,
    ),
  topic: (connectionId: string, topicName: string) =>
    request<TopicDetails>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}`,
    ),
  createTopic: (connectionId: string, options: Record<string, unknown>) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/`,
      { method: "POST", body: options },
    ),
  updateTopic: (connectionId: string, topicName: string, options: Record<string, unknown>) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}`,
      { method: "PUT", body: options },
    ),
  cloneTopic: (connectionId: string, topicName: string, name: string) =>
    request<OperationResult<TopicDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteTopic: (connectionId: string, topicName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}`,
      { method: "DELETE" },
    ),
  subscriptions: (connectionId: string, topicName: string) =>
    request<SubscriptionInfo[]>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/`,
    ),
  subscription: (connectionId: string, topicName: string, subscriptionName: string) =>
    request<SubscriptionDetails>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
    ),
  createSubscription: (connectionId: string, topicName: string, options: Record<string, unknown>) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/`,
      { method: "POST", body: options },
    ),
  updateSubscription: (connectionId: string, topicName: string, subscriptionName: string, options: Record<string, unknown>) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
      { method: "PUT", body: options },
    ),
  cloneSubscription: (connectionId: string, topicName: string, subscriptionName: string, name: string) =>
    request<OperationResult<SubscriptionDetails>>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/clone`,
      { method: "POST", body: { name } },
    ),
  deleteSubscription: (connectionId: string, topicName: string, subscriptionName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}`,
      { method: "DELETE" },
    ),
  rules: (connectionId: string, topicName: string, subscriptionName: string) =>
    request<Rule[]>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/`,
    ),
  saveRule: (connectionId: string, topicName: string, subscriptionName: string, rule: Rule, originalName?: string) =>
    request<OperationResult<Rule>>(
      originalName
        ? `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/${segment(originalName)}`
        : `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/`,
      { method: originalName ? "PUT" : "POST", body: rule },
    ),
  deleteRule: (connectionId: string, topicName: string, subscriptionName: string, ruleName: string) =>
    request<OperationResult>(
      `/api/v1/connections/${segment(connectionId)}/topics/${segment(topicName)}/subscriptions/${segment(subscriptionName)}/rules/${segment(ruleName)}`,
      { method: "DELETE" },
    ),
  receiveMessages: (connectionId: string, options: {
    entityName: string;
    subscriptionName?: string;
    subQueue: SubQueue;
    mode: "PeekLock" | "ReceiveAndDelete";
    type: ReceiveType;
    messagesCount?: number;
    fromSequenceNumber?: number;
  }) => request<ServiceBusMessage[]>(
    `/api/v1/connections/${segment(connectionId)}/messages/receive`,
    { method: "POST", body: options },
  ),
  sendMessages: (connectionId: string, entityName: string, messages: SendMessage[]) =>
    request<{ count: number }>(
      `/api/v1/connections/${segment(connectionId)}/messages/send`,
      { method: "POST", body: { entityName, messages } },
    ),
  purgeMessages: (connectionId: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, totalCount: number) =>
    request<BackgroundJob>("/api/v1/jobs/purge", {
      method: "POST",
      body: { connectionId, entityName, subscriptionName, subQueue, totalCount },
    }),
  resendMessages: (connectionId: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, destinationEntityName: string, totalCount: number) =>
    request<BackgroundJob>("/api/v1/jobs/resend", {
      method: "POST",
      body: { connectionId, entityName, subscriptionName, subQueue, destinationEntityName, totalCount },
    }),
  deleteMessage: (connectionId: string, entityName: string, subscriptionName: string | undefined, subQueue: SubQueue, sequenceNumber: number) =>
    request<BackgroundJob>("/api/v1/jobs/delete-message", {
      method: "POST",
      body: { connectionId, entityName, subscriptionName, subQueue, sequenceNumber },
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
