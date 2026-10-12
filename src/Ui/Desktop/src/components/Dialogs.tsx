import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { X } from "lucide-react";

interface ConfirmOptions {
  title: string;
  message?: string;
  confirmLabel?: string;
  danger?: boolean;
}

interface PromptOptions {
  title: string;
  message?: string;
  defaultValue?: string;
  placeholder?: string;
  confirmLabel?: string;
}

interface DialogApi {
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  prompt: (options: PromptOptions) => Promise<string | null>;
}

type PendingDialog =
  | ({ kind: "confirm" } & ConfirmOptions)
  | ({ kind: "prompt" } & PromptOptions);

const DialogContext = createContext<DialogApi | undefined>(undefined);

// The webview used by Tauri ignores window.confirm/prompt, so dialogs are rendered in-app.
export function DialogProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<PendingDialog>();
  const [value, setValue] = useState("");
  const resolve = useRef<((result: boolean | string | null) => void) | undefined>(undefined);

  const close = useCallback((result: boolean | string | null) => {
    setPending(undefined);
    setValue("");
    const callback = resolve.current;
    resolve.current = undefined;
    callback?.(result);
  }, []);

  const api = useMemo<DialogApi>(() => ({
    confirm: (options) => new Promise<boolean>((done) => {
      resolve.current = (result) => done(result === true);
      setValue("");
      setPending({ kind: "confirm", ...options });
    }),
    prompt: (options) => new Promise<string | null>((done) => {
      resolve.current = (result) => done(typeof result === "string" ? result : null);
      setValue(options.defaultValue ?? "");
      setPending({ kind: "prompt", ...options });
    }),
  }), []);

  useEffect(() => {
    if (!pending) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") close(pending?.kind === "prompt" ? null : false);
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [pending, close]);

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!pending) return;
    if (pending.kind === "prompt") {
      const trimmed = value.trim();
      if (!trimmed) return;
      close(trimmed);
      return;
    }
    close(true);
  }

  const cancelResult = pending?.kind === "prompt" ? null : false;

  return (
    <DialogContext.Provider value={api}>
      {children}
      {pending && (
        <div className="dialog-backdrop" role="presentation" onMouseDown={() => close(cancelResult)}>
          <section
            className="dialog compact-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="app-dialog-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <header className="dialog-header">
              <div>
                <h2 id="app-dialog-title">{pending.title}</h2>
              </div>
              <button className="icon-button" onClick={() => close(cancelResult)} aria-label="Close dialog">
                <X size={19} />
              </button>
            </header>

            <form onSubmit={submit}>
              {pending.message && <p className="field-help">{pending.message}</p>}
              {pending.kind === "prompt" && (
                <label>
                  <input
                    autoFocus
                    value={value}
                    placeholder={pending.placeholder}
                    onChange={(event) => setValue(event.target.value)}
                  />
                </label>
              )}
              <div className="dialog-actions">
                <button type="button" className="button secondary" onClick={() => close(cancelResult)}>
                  Cancel
                </button>
                <button
                  type="submit"
                  className={`button ${pending.kind === "confirm" && pending.danger ? "danger" : "primary"}`}
                  disabled={pending.kind === "prompt" && !value.trim()}
                >
                  {pending.confirmLabel ?? (pending.kind === "prompt" ? "Save" : "Confirm")}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </DialogContext.Provider>
  );
}

export function useDialogs(): DialogApi {
  const context = useContext(DialogContext);
  if (!context) throw new Error("useDialogs must be used within a DialogProvider.");
  return context;
}
