import { useSyncExternalStore } from "react";

export type DiagnosticLevel = "error" | "warning" | "info";
export type DiagnosticOrigin = "frontend" | "desktop" | "backend";

export interface DiagnosticEntry {
  id: string;
  timestamp: number;
  level: DiagnosticLevel;
  origin: DiagnosticOrigin;
  source: string;
  message: string;
  details?: string;
}

const CAPACITY = 500;

let entries: DiagnosticEntry[] = [];
let counter = 0;
const listeners = new Set<() => void>();

function emit() {
  listeners.forEach((listener) => listener());
}

export function logFrontend(
  level: DiagnosticLevel,
  source: string,
  message: string,
  details?: string,
) {
  entries = [
    ...entries,
    {
      id: `frontend-${++counter}`,
      timestamp: Date.now(),
      level,
      origin: "frontend" as const,
      source,
      message,
      details,
    },
  ].slice(-CAPACITY);
  emit();
}

export function clearFrontendLogs() {
  entries = [];
  emit();
}

export function useFrontendLogs() {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => entries,
  );
}

export function describeError(error: unknown): { message: string; details?: string } {
  if (error instanceof Error) {
    return { message: error.message || error.name, details: error.stack };
  }
  if (typeof error === "string") {
    return { message: error };
  }
  try {
    return { message: JSON.stringify(error) };
  } catch {
    return { message: String(error) };
  }
}

function formatConsoleArguments(args: unknown[]) {
  return args
    .map((value) => {
      if (value instanceof Error) return value.stack ?? value.message;
      if (typeof value === "string") return value;
      try {
        return JSON.stringify(value);
      } catch {
        return String(value);
      }
    })
    .join(" ");
}

let installed = false;

export function installGlobalErrorHandlers() {
  if (installed) return;
  installed = true;

  window.addEventListener("error", (event) => {
    const { message, details } = describeError(event.error ?? event.message);
    const location = event.filename ? `${event.filename}:${event.lineno}:${event.colno}` : undefined;
    logFrontend("error", "window.onerror", message, [location, details].filter(Boolean).join("\n") || undefined);
  });

  window.addEventListener("unhandledrejection", (event) => {
    const { message, details } = describeError(event.reason);
    logFrontend("error", "unhandledrejection", message, details);
  });

  for (const level of ["error", "warn"] as const) {
    const original = console[level].bind(console);
    console[level] = (...args: unknown[]) => {
      original(...args);
      logFrontend(level === "warn" ? "warning" : "error", `console.${level}`, formatConsoleArguments(args));
    };
  }
}
