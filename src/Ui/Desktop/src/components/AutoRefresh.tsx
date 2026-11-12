import { createContext, useCallback, useContext, useMemo, useState } from "react";
import type { QueryClient, QueryKey } from "@tanstack/react-query";

interface AutoRefreshState {
  intervals: Record<string, number>;
  setInterval: (connectionId: string, ms: number) => void;
}

const AutoRefreshContext = createContext<AutoRefreshState>({ intervals: {}, setInterval: () => {} });

export function AutoRefreshProvider({ children }: { children: React.ReactNode }) {
  const [intervals, setIntervals] = useState<Record<string, number>>({});
  const setInterval = useCallback((connectionId: string, ms: number) => {
    setIntervals((current) => ({ ...current, [connectionId]: ms }));
  }, []);
  const value = useMemo(() => ({ intervals, setInterval }), [intervals, setInterval]);
  return <AutoRefreshContext.Provider value={value}>{children}</AutoRefreshContext.Provider>;
}

export function useAutoRefresh(connectionId: string) {
  const { intervals, setInterval } = useContext(AutoRefreshContext);
  const ms = intervals[connectionId] ?? 0;
  const set = useCallback((value: number) => setInterval(connectionId, value), [connectionId, setInterval]);
  return [ms, set] as const;
}

export function useRefetchInterval(connectionId: string): number | false {
  const [ms] = useAutoRefresh(connectionId);
  return ms > 0 ? ms : false;
}

// Copies fresher counts into another cached query; equal timestamps stop the sync from bouncing back.
export function syncQueryData<T>(client: QueryClient, key: QueryKey, sourceUpdatedAt: number, update: (current: T) => T) {
  const state = client.getQueryState<T>(key);
  if (!state?.data || state.dataUpdatedAt >= sourceUpdatedAt) return;
  client.setQueryData<T>(key, update(state.data), { updatedAt: sourceUpdatedAt });
}
