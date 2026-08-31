import { spawn } from "node:child_process";
import { existsSync } from "node:fs";
import { homedir } from "node:os";
import { delimiter, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const desktopDirectory = resolve(scriptDirectory, "..");
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

console.log(`Using Cargo from ${join(cargoDirectory, cargoExecutable)}`);

const child = spawn(process.execPath, [tauriCli, ...process.argv.slice(2)], {
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
