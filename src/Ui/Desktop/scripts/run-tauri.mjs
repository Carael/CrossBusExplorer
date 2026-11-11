import { execFileSync, spawn } from "node:child_process";
import { existsSync } from "node:fs";
import { homedir } from "node:os";
import { delimiter, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const desktopDirectory = resolve(scriptDirectory, "..");

function resolveVersionFromTag() {
  const envTag =
    process.env.RELEASE_VERSION ||
    (process.env.GITHUB_REF_TYPE === "tag" ? process.env.GITHUB_REF_NAME : "");

  let rawTag = envTag?.trim() ?? "";
  if (!rawTag) {
    try {
      rawTag = execFileSync("git", ["describe", "--tags", "--abbrev=0"], {
        cwd: desktopDirectory,
        encoding: "utf8",
        stdio: ["ignore", "pipe", "ignore"],
      }).trim();
    } catch {
      return null;
    }
  }

  const normalized = rawTag.replace(/^v/i, "");
  return /^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$/.test(normalized) ? normalized : null;
}
const cargoExecutable = process.platform === "win32" ? "cargo.exe" : "cargo";
const currentPathDirectories = (process.env.PATH ?? "").split(delimiter).filter(Boolean);
const cargoHome = process.env.CARGO_HOME ?? join(homedir(), ".cargo");
const platformDirectories = process.platform === "darwin"
  ? ["/opt/homebrew/opt/rustup/bin", "/opt/homebrew/bin", "/usr/local/bin"]
  : process.platform === "linux"
    ? ["/usr/local/bin", "/snap/bin"]
    : [];
const candidateDirectories = [
  join(cargoHome, "bin"),
  ...platformDirectories,
  ...currentPathDirectories,
];
const cargoDirectory = candidateDirectories.find((directory) =>
  existsSync(join(directory, cargoExecutable)),
);

if (!cargoDirectory) {
  console.error(
    "Cargo was not found. Install the stable Rust toolchain from https://rustup.rs and restart your terminal.",
  );
  process.exit(1);
}

const pathDirectories = [...new Set([cargoDirectory, ...currentPathDirectories])];
const tauriCli = join(desktopDirectory, "node_modules", "@tauri-apps", "cli", "tauri.js");
const rawArgs = process.argv.slice(2);
const subcommand = rawArgs[0];
const hasConfigFlag = rawArgs.includes("--config") || rawArgs.includes("-c");
const tagVersion = resolveVersionFromTag();
const tauriArgs =
  tagVersion && (subcommand === "build" || subcommand === "dev") && !hasConfigFlag
    ? [subcommand, "--config", JSON.stringify({ version: tagVersion }), ...rawArgs.slice(1)]
    : rawArgs;

console.log(`Using Cargo from ${join(cargoDirectory, cargoExecutable)}`);
if (tagVersion) {
  console.log(`Using version ${tagVersion} resolved from git tag`);
}

const child = spawn(process.execPath, [tauriCli, ...tauriArgs], {
  cwd: desktopDirectory,
  env: { ...process.env, PATH: pathDirectories.join(delimiter) },
  stdio: "inherit",
});

child.on("error", (error) => {
  console.error(`Unable to start Tauri: ${error.message}`);
  process.exitCode = 1;
});

child.on("exit", (code, signal) => {
  if (signal) {
    console.error(`Tauri stopped by signal ${signal}.`);
    process.exitCode = 1;
    return;
  }

  process.exitCode = code ?? 1;
});
