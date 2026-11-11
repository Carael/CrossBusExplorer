use std::collections::VecDeque;
use std::sync::Mutex;
use std::time::{Duration, SystemTime, UNIX_EPOCH};

use serde::{Deserialize, Serialize};
use tauri::{Manager, RunEvent, State};
use tauri_plugin_shell::process::{CommandChild, CommandEvent};
use tauri_plugin_shell::ShellExt;
use tokio::sync::Notify;
use uuid::Uuid;

struct BackendState {
    runtime: Mutex<BackendRuntime>,
    ready: Notify,
    client: reqwest::Client,
    diagnostics: Mutex<VecDeque<DiagnosticEntry>>,
}

const DIAGNOSTICS_CAPACITY: usize = 1000;

#[derive(Clone, Serialize)]
#[serde(rename_all = "camelCase")]
struct DiagnosticEntry {
    id: u64,
    timestamp: u64,
    level: String,
    source: String,
    message: String,
    details: Option<String>,
}

struct BackendRuntime {
    base_url: Option<String>,
    token: String,
    child: Option<CommandChild>,
    startup_error: Option<String>,
    next_diagnostic_id: u64,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct BackendRequest {
    method: String,
    path: String,
    body: Option<String>,
}

#[derive(Serialize)]
struct BackendResponse {
    status: u16,
    body: Option<String>,
}

#[derive(Deserialize)]
struct ReadyPayload {
    addresses: Vec<String>,
}

fn record(app_state: &BackendState, level: &str, source: &str, message: impl Into<String>, details: Option<String>) {
    let id = match app_state.runtime.lock() {
        Ok(mut runtime) => {
            runtime.next_diagnostic_id += 1;
            runtime.next_diagnostic_id
        }
        Err(_) => return,
    };
    let timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|value| value.as_millis() as u64)
        .unwrap_or_default();

    if let Ok(mut entries) = app_state.diagnostics.lock() {
        entries.push_back(DiagnosticEntry {
            id,
            timestamp,
            level: level.to_string(),
            source: source.to_string(),
            message: message.into(),
            details,
        });
        while entries.len() > DIAGNOSTICS_CAPACITY {
            entries.pop_front();
        }
    }
}

#[tauri::command]
fn get_diagnostics(state: State<'_, BackendState>) -> Vec<DiagnosticEntry> {
    state
        .diagnostics
        .lock()
        .map(|entries| entries.iter().cloned().collect())
        .unwrap_or_default()
}

#[tauri::command]
fn clear_diagnostics(state: State<'_, BackendState>) {
    if let Ok(mut entries) = state.diagnostics.lock() {
        entries.clear();
    }
}

#[tauri::command]
async fn backend_request(
    state: State<'_, BackendState>,
    request: BackendRequest,
) -> Result<BackendResponse, String> {
    let description = format!("{} {}", request.method, request.path);
    let result = send_backend_request(&state, request).await;
    if let Err(error) = &result {
        record(&state, "error", "bridge", format!("{description}: {error}"), None);
    }
    result
}

async fn send_backend_request(
    state: &BackendState,
    request: BackendRequest,
) -> Result<BackendResponse, String> {
    validate_path(&request.path)?;

    let base_url = wait_for_backend(state).await?;
    let token = state
        .runtime
        .lock()
        .map_err(|_| "Backend state lock was poisoned.".to_string())?
        .token
        .clone();
    let method = reqwest::Method::from_bytes(request.method.as_bytes())
        .map_err(|_| "Unsupported HTTP method.".to_string())?;

    let mut outgoing = state
        .client
        .request(method, format!("{base_url}{}", request.path))
        .bearer_auth(token)
        .header("Content-Type", "application/json");

    if let Some(body) = request.body {
        outgoing = outgoing.body(body);
    }

    let response = outgoing
        .send()
        .await
        .map_err(|error| format!("{error:?}"))?;
    let status = response.status().as_u16();
    let body = response.text().await.map_err(|error| error.to_string())?;

    Ok(BackendResponse {
        status,
        body: if body.is_empty() { None } else { Some(body) },
    })
}

fn validate_path(path: &str) -> Result<(), String> {
    let allowed = path == "/health" || path.starts_with("/api/v1/");
    if !allowed || path.contains("..") || path.contains("://") {
        return Err("The requested backend path is not allowed.".to_string());
    }
    Ok(())
}

async fn wait_for_backend(state: &BackendState) -> Result<String, String> {
    let wait = async {
        loop {
            {
                let runtime = state
                    .runtime
                    .lock()
                    .map_err(|_| "Backend state lock was poisoned.".to_string())?;
                if let Some(base_url) = &runtime.base_url {
                    return Ok(base_url.clone());
                }
                if let Some(error) = &runtime.startup_error {
                    return Err(error.clone());
                }
            }
            state.ready.notified().await;
        }
    };

    tokio::time::timeout(Duration::from_secs(30), wait)
        .await
        .map_err(|_| "The C# backend did not become ready within 30 seconds.".to_string())?
}

fn start_backend(app: &tauri::App) -> Result<(), Box<dyn std::error::Error>> {
    let data_directory = app.path().app_data_dir()?;
    std::fs::create_dir_all(&data_directory)?;

    let state = app.state::<BackendState>();
    let token = state
        .runtime
        .lock()
        .map_err(|_| "Backend state lock was poisoned.")?
        .token
        .clone();

    let mut command = app
        .shell()
        .sidecar("crossbus-host")?
        .env("CROSSBUS_DATA_DIR", data_directory)
        .env("CROSSBUS_API_TOKEN", token)
        .env("CROSSBUS_API_URLS", "http://127.0.0.1:0");

    if let Some(path) = backend_path() {
        command = command.env("PATH", path);
    }

    let (mut events, child) = command.spawn()?;

    state
        .runtime
        .lock()
        .map_err(|_| "Backend state lock was poisoned.")?
        .child = Some(child);

    let handle = app.handle().clone();
    tauri::async_runtime::spawn(async move {
        while let Some(event) = events.recv().await {
            match event {
                CommandEvent::Stderr(bytes) => {
                    let line = String::from_utf8_lossy(&bytes);
                    record(&handle.state::<BackendState>(), "error", "backend-stderr", line.trim_end(), None);
                }
                CommandEvent::Stdout(bytes) => {
                    let line = String::from_utf8_lossy(&bytes);
                    if !line.trim_start().starts_with("CROSSBUS_READY ") {
                        record(&handle.state::<BackendState>(), "info", "backend-stdout", line.trim_end(), None);
                    }
                    if let Some(payload) = line.trim().strip_prefix("CROSSBUS_READY ") {
                        match serde_json::from_str::<ReadyPayload>(payload) {
                            Ok(payload) if !payload.addresses.is_empty() => {
                                let state = handle.state::<BackendState>();
                                if let Ok(mut runtime) = state.runtime.lock() {
                                    runtime.base_url = payload.addresses.first().cloned();
                                }
                                state.ready.notify_waiters();
                            }
                            Ok(_) => set_startup_error(&handle, "Backend returned no address."),
                            Err(error) => set_startup_error(
                                &handle,
                                format!("Could not parse backend readiness: {error}"),
                            ),
                        }
                    }
                }
                CommandEvent::Error(error) => set_startup_error(&handle, error),
                CommandEvent::Terminated(payload) => {
                    let state = handle.state::<BackendState>();
                    record(
                        &state,
                        "error",
                        "backend",
                        format!("Backend process terminated (code {:?}, signal {:?}).", payload.code, payload.signal),
                        None,
                    );
                    let was_ready = state
                        .runtime
                        .lock()
                        .map(|runtime| runtime.base_url.is_some())
                        .unwrap_or(false);
                    if !was_ready {
                        set_startup_error(
                            &handle,
                            format!("Backend exited during startup: {payload:?}"),
                        );
                    }
                }
                _ => {}
            }
        }
    });

    Ok(())
}

fn backend_path() -> Option<std::ffi::OsString> {
    #[cfg_attr(not(any(target_os = "macos", target_os = "linux")), allow(unused_mut))]
    let mut paths: Vec<std::path::PathBuf> = std::env::var_os("PATH")
        .map(|value| std::env::split_paths(&value).collect())
        .unwrap_or_default();

    #[cfg(target_os = "macos")]
    for path in [
        std::path::PathBuf::from("/opt/homebrew/bin"),
        std::path::PathBuf::from("/usr/local/bin"),
    ] {
        if !paths.contains(&path) {
            paths.push(path);
        }
    }

    #[cfg(target_os = "linux")]
    for path in [
        std::path::PathBuf::from("/usr/local/bin"),
        std::path::PathBuf::from("/snap/bin"),
    ] {
        if !paths.contains(&path) {
            paths.push(path);
        }
    }
    std::env::join_paths(paths).ok()
}

fn set_startup_error(app: &tauri::AppHandle, error: impl Into<String>) {
    let error = error.into();
    let state = app.state::<BackendState>();
    record(&state, "error", "backend", error.clone(), None);
    if let Ok(mut runtime) = state.runtime.lock() {
        runtime.startup_error = Some(error);
    }
    state.ready.notify_waiters();
}

pub fn run() {
    let state = BackendState {
        runtime: Mutex::new(BackendRuntime {
            base_url: None,
            token: Uuid::new_v4().to_string(),
            child: None,
            startup_error: None,
            next_diagnostic_id: 0,
        }),
        ready: Notify::new(),
        client: reqwest::Client::new(),
        diagnostics: Mutex::new(VecDeque::new()),
    };

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .manage(state)
        .invoke_handler(tauri::generate_handler![backend_request, get_diagnostics, clear_diagnostics])
        .setup(|app| {
            start_backend(app)?;
            Ok(())
        })
        .build(tauri::generate_context!())
        .expect("failed to build Cross Bus Explorer")
        .run(|app, event| {
            if matches!(event, RunEvent::ExitRequested { .. } | RunEvent::Exit) {
                let state = app.state::<BackendState>();
                if let Ok(mut runtime) = state.runtime.lock() {
                    if let Some(child) = runtime.child.take() {
                        let _ = child.kill();
                    }
                };
            }
        });
}
