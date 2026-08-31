import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, KeyRound, Network, Pencil, RadioTower, Trash2, Wifi } from "lucide-react";
import { api } from "../api";
import type { Connection } from "../types";

export function ConnectionOverview({ connection, onEdit, onDeleted }: { connection: Connection; onEdit: () => void; onDeleted: () => void }) {
  const queryClient = useQueryClient();
  const test = useMutation({ mutationFn: () => api.testConnection(connection.name) });
  const remove = useMutation({
    mutationFn: () => api.deleteConnection(connection.name),
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ["connections"] }); onDeleted(); },
  });

  function confirmDelete() {
    if (window.confirm(`Delete the ${connection.name} connection profile?`)) {
      remove.mutate();
    }
  }

  return (
    <div className="content-page">
      <header className="page-header">
        <div>
          <div className="breadcrumbs">Connections / {connection.name}</div>
          <div className="title-line">
            <div className="resource-icon"><RadioTower size={22} /></div>
            <div>
              <h1>{connection.name}</h1>
              <p>{connection.fullyQualifiedName}</p>
            </div>
          </div>
        </div>
        <div className="header-actions">
          <button className="button secondary" onClick={onEdit}><Pencil size={16} /> Edit</button>
          <button className="button secondary" onClick={() => test.mutate()} disabled={test.isPending}>
            <Wifi size={16} /> {test.isPending ? "Testing…" : "Test connection"}
          </button>
          <button className="button danger-ghost" onClick={confirmDelete} disabled={remove.isPending}>
            <Trash2 size={16} /> Delete
          </button>
        </div>
      </header>

      {test.data && (
        <div className="notice success">
          <CheckCircle2 size={17} />
          <span>{test.data.message}</span>
        </div>
      )}
      {(test.error || remove.error) && (
        <div className="notice error">{(test.error ?? remove.error)?.message}</div>
      )}

      <div className="overview-grid">
        <section className="card identity-card">
          <div className="card-heading">
            <div><span className="eyebrow">Profile</span><h2>Connection details</h2></div>
          </div>
          <dl className="definition-list">
            <div><dt><Network size={15} /> Namespace</dt><dd>{connection.fullyQualifiedName}</dd></div>
            <div><dt><KeyRound size={15} /> Authentication</dt><dd>{formatAuthentication(connection.authenticationType)}</dd></div>
            <div><dt>Transport</dt><dd>{connection.transportType === "AmqpTcp" ? "AMQP over TCP" : "AMQP over WebSockets"}</dd></div>
            <div><dt>Folder</dt><dd>{connection.folder || "Default"}</dd></div>
            {connection.tenantId && <div><dt>Tenant</dt><dd>{connection.tenantId}</dd></div>}
            {connection.clientId && <div><dt>Client</dt><dd>{connection.clientId}</dd></div>}
          </dl>
        </section>

        <section className="card guidance-card">
          <span className="eyebrow">Next step</span>
          <h2>Browse the namespace</h2>
          <p>Expand Queues or Topics in the explorer. Resources are loaded only when needed to keep startup fast.</p>
          {connection.authenticationType === "AzureCli" && (
            <div className="command-hint"><code>az login</code><span>Run this first if the test cannot find an authenticated account.</span></div>
          )}
        </section>
      </div>
    </div>
  );
}

function formatAuthentication(value: Connection["authenticationType"]) {
  return ({
    AzureCli: "Azure CLI",
    ConnectionString: "Connection string",
    DefaultAzureCredential: "Default Azure credential",
    WorkloadIdentity: "Workload identity",
    InteractiveBrowser: "Interactive browser",
  } as const)[value];
}
