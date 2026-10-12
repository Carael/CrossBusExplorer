import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { App } from "./App";
import { AutoRefreshProvider } from "./components/AutoRefresh";
import { DialogProvider } from "./components/Dialogs";
import "./styles.css";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <DialogProvider>
        <AutoRefreshProvider>
          <App />
        </AutoRefreshProvider>
      </DialogProvider>
    </QueryClientProvider>
  </StrictMode>,
);
