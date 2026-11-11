import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { App } from "./App";
import { AutoRefreshProvider } from "./components/AutoRefresh";
import { DialogProvider } from "./components/Dialogs";
import { describeError, installGlobalErrorHandlers, logFrontend } from "./diagnostics";
import "./styles.css";

installGlobalErrorHandlers();

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const reportRenderError = (source: string) => (
  error: unknown,
  info: { componentStack?: string },
) => {
  const { message, details } = describeError(error);
  logFrontend("error", source, message, [details, info.componentStack].filter(Boolean).join("\n"));
};

createRoot(document.getElementById("root")!, {
  onUncaughtError: reportRenderError("react.uncaught"),
  onCaughtError: reportRenderError("react.caught"),
}).render(
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
