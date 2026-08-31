import { useEffect, useState, type FormEvent } from "react";
import { X } from "lucide-react";
import type { AuthenticationType, Connection, SaveConnection } from "../types";

interface Props {
  open: boolean;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSave: (connection: SaveConnection) => void;
  initial?: Connection;
}

const emptyForm: SaveConnection = {
  name: "",
  authenticationType: "AzureCli",
  fullyQualifiedName: "",
  connectionString: "",
  transportType: "AmqpTcp",
  tenantId: "",
  clientId: "",
  tokenFilePath: "",
  folder: "",
};

const authenticationOptions: Array<{
  value: AuthenticationType;
  label: string;
  description: string;
}> = [
  {
    value: "AzureCli",
    label: "Azure CLI",
    description: "Use the account already authenticated with az login.",
  },
  {
    value: "ConnectionString",
    label: "Connection string",
    description: "Legacy SAS authentication. The secret stays in the local backend.",
  },
  {
    value: "DefaultAzureCredential",
    label: "Default Azure credential",
    description: "Use the configured Azure SDK credential chain.",
  },
  {
    value: "WorkloadIdentity",
    label: "Workload identity",
    description: "Use a tenant, client and federated token file.",
  },
  {
    value: "InteractiveBrowser",
    label: "Interactive browser",
    description: "Authenticate using the system browser.",
  },
];

export function ConnectionDialog({ open, busy, error, onClose, onSave, initial }: Props) {
  const [form, setForm] = useState<SaveConnection>(emptyForm);

  useEffect(() => {
    if (open) setForm(initial ? {
      name: initial.name,
      authenticationType: initial.authenticationType,
      fullyQualifiedName: initial.fullyQualifiedName,
      connectionString: "",
      transportType: initial.transportType,
      tenantId: initial.tenantId ?? "",
      clientId: initial.clientId ?? "",
      tokenFilePath: initial.tokenFilePath ?? "",
      folder: initial.folder,
    } : emptyForm);
  }, [open, initial]);

  if (!open) return null;

  const selected = authenticationOptions.find(
    (option) => option.value === form.authenticationType,
  )!;
  const passwordless = form.authenticationType !== "ConnectionString";
  const workload = form.authenticationType === "WorkloadIdentity";

  function submit(event: FormEvent) {
    event.preventDefault();
    onSave(form);
  }

  return (
    <div className="dialog-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="dialog connection-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="connection-dialog-title"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <header className="dialog-header">
          <div>
            <span className="eyebrow">Connection profile</span>
            <h2 id="connection-dialog-title">{initial ? "Edit connection profile" : "Add a Service Bus namespace"}</h2>
          </div>
          <button className="icon-button" onClick={onClose} aria-label="Close dialog">
            <X size={19} />
          </button>
        </header>

        <form onSubmit={submit}>
          <div className="auth-grid" role="radiogroup" aria-label="Authentication method">
            {authenticationOptions.map((option) => (
              <label
                className={`auth-option ${form.authenticationType === option.value ? "selected" : ""}`}
                key={option.value}
              >
                <input
                  type="radio"
                  name="authenticationType"
                  value={option.value}
                  checked={form.authenticationType === option.value}
                  onChange={() =>
                    setForm((current) => ({
                      ...current,
                      authenticationType: option.value,
                    }))
                  }
                />
                <strong>{option.label}</strong>
              </label>
            ))}
          </div>
          <p className="field-help">{selected.description}</p>

          <div className="form-grid">
            <label>
              <span>Display name</span>
              <input
                autoFocus
                required
                disabled={!!initial}
                value={form.name}
                onChange={(event) => setForm({ ...form, name: event.target.value })}
                placeholder="Payments - production"
              />
            </label>

            {passwordless ? (
              <label className="wide">
                <span>Fully qualified namespace</span>
                <input
                  required={!initial?.hasStoredSecret}
                  value={form.fullyQualifiedName}
                  onChange={(event) =>
                    setForm({ ...form, fullyQualifiedName: event.target.value })
                  }
                  placeholder="payments.servicebus.windows.net"
                />
              </label>
            ) : (
              <label className="wide">
                <span>Connection string</span>
                <textarea
                  required
                  rows={4}
                  value={form.connectionString}
                  onChange={(event) =>
                    setForm({ ...form, connectionString: event.target.value })
                  }
                  placeholder="Endpoint=sb://…"
                />
                {initial?.hasStoredSecret && <em className="input-hint">Leave blank to keep the stored connection string.</em>}
              </label>
            )}

            {passwordless && (
              <label>
                <span>Tenant ID {!workload && <em>optional</em>}</span>
                <input
                  required={workload}
                  value={form.tenantId}
                  onChange={(event) => setForm({ ...form, tenantId: event.target.value })}
                  placeholder="Use active tenant"
                />
              </label>
            )}

            {(workload || form.authenticationType === "InteractiveBrowser") && (
              <label>
                <span>Client ID {workload ? "" : <em>optional</em>}</span>
                <input
                  required={workload}
                  value={form.clientId}
                  onChange={(event) => setForm({ ...form, clientId: event.target.value })}
                />
              </label>
            )}

            {workload && (
              <label className="wide">
                <span>Federated token file</span>
                <input
                  required
                  value={form.tokenFilePath}
                  onChange={(event) =>
                    setForm({ ...form, tokenFilePath: event.target.value })
                  }
                  placeholder="/var/run/secrets/azure/tokens/azure-identity-token"
                />
              </label>
            )}

            <label>
              <span>Transport</span>
              <select
                value={form.transportType}
                onChange={(event) =>
                  setForm({
                    ...form,
                    transportType: event.target.value as SaveConnection["transportType"],
                  })
                }
              >
                <option value="AmqpTcp">AMQP over TCP</option>
                <option value="AmqpWebSockets">AMQP over WebSockets</option>
              </select>
            </label>

            <label>
              <span>Folder <em>optional</em></span>
              <input
                value={form.folder}
                onChange={(event) => setForm({ ...form, folder: event.target.value })}
                placeholder="Production"
              />
            </label>
          </div>

          {error && <div className="inline-error">{error}</div>}

          <footer className="dialog-actions">
            <button type="button" className="button secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="button primary" disabled={busy}>
              {busy ? "Saving…" : "Save connection"}
            </button>
          </footer>
        </form>
      </section>
    </div>
  );
}
