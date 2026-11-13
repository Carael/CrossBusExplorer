import { useSyncExternalStore } from "react";

const STORAGE_KEY = "crossbus-favorites";

export type FavoriteEntityKind = "queue" | "topic";

function loadFavorites(): Set<string> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return new Set();
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return new Set(parsed.filter((item): item is string => typeof item === "string"));
    }
  } catch {
    // Ignore storage read failures.
  }
  return new Set();
}

let favorites = loadFavorites();
const listeners = new Set<() => void>();

function saveAndEmit(next: Set<string>) {
  favorites = next;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...favorites]));
  } catch {
    // Ignore storage write failures.
  }
  listeners.forEach((listener) => listener());
}

export function favoriteKey(kind: FavoriteEntityKind, connectionId: string, name: string): string {
  return `${kind}:${connectionId}:${name}`;
}

export function toggleFavorite(kind: FavoriteEntityKind, connectionId: string, name: string): void {
  const key = favoriteKey(kind, connectionId, name);
  const next = new Set(favorites);
  if (next.has(key)) {
    next.delete(key);
  } else {
    next.add(key);
  }
  saveAndEmit(next);
}

export function useFavorites(): ReadonlySet<string> {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => favorites,
  );
}
