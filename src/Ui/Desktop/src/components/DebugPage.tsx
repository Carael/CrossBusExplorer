import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Bug, ChevronRight, ClipboardCopy, Search, Trash2 } from "lucide-react";
import { api } from "../api";
import {
  clearFrontendLogs,
  useFrontendLogs,
  type DiagnosticEntry,
  type DiagnosticLevel,
  type DiagnosticOrigin,
} from "../diagnostics";

const ORIGIN_LABELS: Record<DiagnosticOrigin, string> = {
  frontend: "Frontend",
  desktop: "Desktop",
  backend: "Backend",
};

export function DebugPage() {
  const queryClient = useQueryClient();
  const frontendLogs = useFrontendLogs();
  const [level, setLevel] = useState<DiagnosticLevel | "all">("all");
  const [origin, setOrigin] = useState<DiagnosticOrigin | "all">("all");
  const [search, setSearch] = useState("");
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [copied, setCopied] = useState(false);

  const backend = useQuery({
    queryKey: ["diagnostics", "backend"],
    queryFn: api.backendLogs,
    refetchInterval: 2000,
    staleTime: 0,
    retry: false,
  });
  const desktop = useQuery({
    queryKey: ["diagnostics", "desktop"],
    queryFn: api.desktopLogs,
    refetchInterval: 2000,
    staleTime: 0,
    retry: false,
  });

  const entries = useMemo(() => {
    const all: DiagnosticEntry[] = [
      ...frontendLogs,
      ...(desktop.data ?? []).map((entry) => ({
        id: `desktop-${entry.id}`,
        timestamp: entry.timestamp,
        level: entry.level,
        origin: "desktop" as const,
        source: entry.source,
        message: entry.message,
        details: entry.details ?? undefined,
      })),
      ...(backend.data ?? []).map((entry) => ({
        id: `backend-${entry.id}`,
        timestamp: Date.parse(entry.timestamp),
        level: entry.level,
        origin: "backend" as const,
        source: entry.source,
        message: entry.message,
        details: entry.details ?? undefined,
      })),
    ];
    return all.sort((left, right) => right.timestamp - left.timestamp);
  }, [frontendLogs, desktop.data, backend.data]);

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return entries.filter((entry) => {
      if (level !== "all" && entry.level !== level) return false;
      if (origin !== "all" && entry.origin !== origin) return false;
      if (!term) return true;
      return [entry.source, entry.message, entry.details ?? ""].some((value) => value.toLowerCase().includes(term));
    });
  }, [entries, level, origin, search]);

  const counts = useMemo(() => ({
    error: entries.filter((entry) => entry.level === "error").length,
    warning: entries.filter((entry) => entry.level === "warning").length,
  }), [entries]);

  async function clearAll() {
    clearFrontendLogs();
    await Promise.allSettled([api.clearBackendLogs(), api.clearDesktopLogs()]);
    await queryClient.invalidateQueries({ queryKey: ["diagnostics"] });
  }

  async function copyVisible() {
    const text = visible
      .map((entry) =>
        `[${new Date(entry.timestamp).toISOString()}] ${entry.level.toUpperCase()} ${ORIGIN_LABELS[entry.origin]} · ${entry.source}\n${entry.message}${entry.details ? `\n${entry.details}` : ""}`)
      .join("\n\n");
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    } catch (error) {
      console.error("Could not copy diagnostics to the clipboard.", error);
    }
  }

  function toggle(id: string) {
    setExpanded((current) => {
      const next = new Set(current);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  }

  return (
    <div className="content-page wide-page">
      <header className="page-header">
        <div>
          <div className="breadcrumbs">Debug</div>
          <div className="title-line">
            <div className="resource-icon"><Bug size={22} /></div>
            <div>
              <h1>Debug</h1>
              <p>Errors and warnings from the interface, the desktop shell, and the C# backend.</p>
            </div>
          </div>
        </div>
        <div className="header-actions">
          <button className="button secondary" onClick={copyVisible} disabled={visible.length === 0}>
            <ClipboardCopy size={16} /> {copied ? "Copied" : "Copy"}
          </button>
          <button className="button danger-ghost" onClick={clearAll} disabled={entries.length === 0}>
            <Trash2 size={16} /> Clear
          </button>
        </div>
      </header>

      {backend.error && (
        <div className="notice error">The backend log could not be loaded: {backend.error.message}</div>
      )}

      <div className="debug-toolbar">
        <div className="segmented" role="group" aria-label="Level">
          {(["all", "error", "warning", "info"] as const).map((value) => (
            <button key={value} className={level === value ? "active" : ""} onClick={() => setLevel(value)}>
              {value === "all" ? "All" : value[0].toUpperCase() + value.slice(1)}
              {value === "error" && counts.error > 0 && <span className="count-badge danger">{counts.error}</span>}
              {value === "warning" && counts.warning > 0 && <span className="count-badge">{counts.warning}</span>}
            </button>
          ))}
        </div>
        <select className="debug-select" value={origin} onChange={(event) => setOrigin(event.target.value as DiagnosticOrigin | "all")} aria-label="Origin">
          <option value="all">All origins</option>
          {(Object.keys(ORIGIN_LABELS) as DiagnosticOrigin[]).map((value) => (
            <option key={value} value={value}>{ORIGIN_LABELS[value]}</option>
          ))}
        </select>
        <label className="search-box debug-search">
          <Search size={15} />
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search logs" aria-label="Search logs" />
        </label>
      </div>

      <div className="debug-list">
        {visible.length === 0 ? (
          <div className="debug-empty">{entries.length === 0 ? "No problems recorded." : "No entries match the filters."}</div>
        ) : visible.map((entry) => {
          const open = expanded.has(entry.id);
          return (
            <div key={entry.id} className={`debug-entry ${entry.level}`}>
              <button className="debug-entry-header" onClick={() => entry.details && toggle(entry.id)} aria-expanded={entry.details ? open : undefined} disabled={!entry.details}>
                <ChevronRight size={14} className={`debug-chevron ${open ? "open" : ""} ${entry.details ? "" : "hidden"}`} />
                <time>{new Date(entry.timestamp).toLocaleTimeString()}</time>
                <span className={`debug-level ${entry.level}`}>{entry.level}</span>
                <span className="debug-origin">{ORIGIN_LABELS[entry.origin]}</span>
                <span className="debug-source" title={entry.source}>{entry.source}</span>
                <span className="debug-message">{entry.message}</span>
              </button>
              {open && entry.details && <pre className="debug-details">{entry.details}</pre>}
            </div>
          );
        })}
      </div>
    </div>
  );
}
