export type AuthenticationType =
  | "ConnectionString"
  | "AzureCli"
  | "DefaultAzureCredential"
  | "WorkloadIdentity"
  | "InteractiveBrowser";

export type TransportType = "AmqpTcp" | "AmqpWebSockets";

export interface Connection {
  name: string;
  fullyQualifiedName: string;
  authenticationType: AuthenticationType;
  transportType: TransportType;
  tenantId?: string;
  clientId?: string;
  tokenFilePath?: string;
  folder: string;
  hasStoredSecret: boolean;
}

export interface SaveConnection {
  name: string;
  authenticationType: AuthenticationType;
  fullyQualifiedName?: string;
  connectionString?: string;
  transportType: TransportType;
  tenantId?: string;
  clientId?: string;
  tokenFilePath?: string;
  folder?: string;
}

export interface QueueInfo {
  name: string;
  status: string;
  sizeInBytes: number;
  sizeInMB: number;
  createdAt: string;
  accessedAt: string;
  updatedAt: string;
  activeMessagesCount: number;
  deadLetterMessagesCount: number;
  scheduledMessagesCount: number;
  inTransferMessagesCount: number;
  transferDeadLetterMessagesCount: number;
  totalMessagesCount: number;
}

export interface QueueDetails {
  info: QueueInfo;
  settings: {
    enableBatchedOperations: boolean;
    enableDeadLetteringOnMessageExpiration: boolean;
    enablePartitioning: boolean;
    requiresDuplicateDetection: boolean;
    requiresSession: boolean;
  };
  timeSettings: {
    autoDeleteOnIdle: string;
    defaultMessageTimeToLive: string;
    duplicateDetectionHistoryTimeWindow: string;
    lockDuration: string;
  };
  properties: {
    maxQueueSizeInMegabytes: number;
    maxMessageSizeInKilobytes?: number;
    maxDeliveryCount: number;
    userMetadata?: string;
    forwardTo?: string;
    forwardDeadLetteredMessagesTo?: string;
  };
}

export interface TopicStructure {
  name: string;
  isFolder: boolean;
  fullName?: string;
  childTopics: TopicStructure[];
}

export interface TopicInfo {
  name: string;
  status: string;
  sizeInBytes: number;
  sizeInMB: number;
  createdAt: string;
  accessedAt: string;
  updatedAt: string;
  scheduledMessagesCount: number;
}

export interface TopicDetails {
  info: TopicInfo;
  settings: {
    enableBatchedOperations: boolean;
    enablePartitioning: boolean;
    requiresDuplicateDetection: boolean;
    supportOrdering: boolean;
  };
  timeSettings: {
    autoDeleteOnIdle: string;
    defaultMessageTimeToLive: string;
    duplicateDetectionHistoryTimeWindow: string;
  };
  properties: {
    maxQueueSizeInMegabytes: number;
    maxMessageSizeInKilobytes?: number;
    userMetadata?: string;
  };
}

export interface SubscriptionInfo {
  topicName: string;
  subscriptionName: string;
  status: string;
  createdAt: string;
  accessedAt: string;
  updatedAt: string;
  activeMessagesCount: number;
  deadLetterMessagesCount: number;
  transferMessagesCount: number;
}

export interface SubscriptionDetails {
  info: SubscriptionInfo;
  settings: {
    enableBatchedOperations: boolean;
    enableDeadLetteringOnMessageExpiration: boolean;
    requiresSession: boolean;
    enableDeadLetteringOnFilterEvaluationExceptions: boolean;
  };
  timeSettings: {
    autoDeleteOnIdle: string;
    defaultMessageTimeToLive: string;
    lockDuration: string;
  };
  properties: {
    maxDeliveryCount: number;
    userMetadata?: string;
    forwardTo?: string;
    forwardDeadLetteredMessagesTo?: string;
  };
}

export interface Rule {
  name: string;
  type: "Sql" | "CorrelationId" | "TrueFilter" | "FalseFilter";
  value?: string;
}

export type SubQueue = "None" | "DeadLetter" | "TransferDeadLetter";
export type ReceiveMode = "PeekLock" | "ReceiveAndDelete";

export interface ServiceBusMessage {
  id: string;
  subject?: string;
  body: string;
  systemProperties: {
    contentType?: string;
    correlationId?: string;
    deadLetterSource?: string;
    deadLetterReason?: string;
    deadLetterErrorDescription?: string;
    deliveryCount: number;
    enqueuedSequenceNumber: number;
    enqueuedTime: string;
    expiresAt: string;
    lockedUntil: string;
    lockToken: string;
    partitionKey?: string;
    transactionPartitionKey?: string;
    replyTo?: string;
    replyToSessionId?: string;
    scheduledEnqueueTime?: string;
    sequenceNumber: number;
    sessionId?: string;
    state?: string;
    timeToLive: string;
    to?: string;
  };
  applicationProperties?: Record<string, unknown>;
}

export interface SendMessage {
  body: string;
  subject?: string;
  to?: string;
  contentType?: string;
  correlationId?: string;
  id?: string;
  partitionKey?: string;
  replyTo?: string;
  sessionId?: string;
  scheduledEnqueueTime?: string;
  timeToLive?: string;
  applicationProperties?: Record<string, unknown>;
}

export interface OperationResult<T = never> {
  success: boolean;
  data?: T;
}

export interface BackgroundJob {
  id: string;
  name: string;
  status: "Queued" | "Running" | "Succeeded" | "Failed" | "Cancelled";
  progress: number;
  message?: string;
  createdAt: string;
  completedAt?: string;
}

export type TrackedEntityKind = "Queue" | "Topic" | "Subscription";
export type TrackedMessageSource = "Tap" | "Peek" | "DeadLetter";

export interface StartTracking {
  connectionName: string;
  nameFilter?: string;
  enableTopicTaps: boolean;
  includeDeadLetter: boolean;
  pollIntervalSeconds: number;
}

export interface TapStatus {
  topicName: string;
  subscriptionName: string;
  state: "Creating" | "Active" | "Unauthorized" | "Failed";
  error?: string;
}

export interface TrackingSession {
  id: string;
  connectionName: string;
  nameFilter?: string;
  enableTopicTaps: boolean;
  includeDeadLetter: boolean;
  pollIntervalSeconds: number;
  status: "Starting" | "Running" | "Stopped" | "Failed";
  error?: string;
  startedAt: string;
  stoppedAt?: string;
  lastPollAt?: string;
  watchedEntityCount: number;
  droppedCount: number;
  taps: TapStatus[];
}

export interface TrackedMessage {
  cursor: number;
  capturedAt: string;
  entityPath: string;
  entityKind: TrackedEntityKind;
  entityName: string;
  subscriptionName?: string;
  source: TrackedMessageSource;
  message: ServiceBusMessage;
}

export interface ActivityEvent {
  cursor: number;
  at: string;
  entityPath: string;
  entityKind: TrackedEntityKind;
  activeDelta: number;
  deadLetterDelta: number;
  activeCount: number;
  deadLetterCount: number;
}

export interface EntityActivity {
  entityPath: string;
  entityKind: TrackedEntityKind;
  entityName: string;
  subscriptionName?: string;
  lastActivityAt: string;
  activeCount: number;
  deadLetterCount: number;
  changeCount: number;
  capturedCount: number;
}

export interface TrackingUpdates {
  session: TrackingSession;
  messages: TrackedMessage[];
  events: ActivityEvent[];
  entities: EntityActivity[];
  nextCursor: number;
  hasMore: boolean;
}

export type ResourceSelection =
  | { kind: "connection"; connectionName: string }
  | { kind: "tracker"; connectionName: string }
  | { kind: "queue"; connectionName: string; name: string }
  | { kind: "topic"; connectionName: string; name: string }
  | {
      kind: "subscription";
      connectionName: string;
      topicName: string;
      name: string;
    };
