import { execFileSync } from "node:child_process";
import { copyFileSync, mkdirSync, rmSync } from "node:fs";
import { arch, platform } from "node:process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const desktopDirectory = resolve(scriptDirectory, "..");
const repositoryDirectory = resolve(desktopDirectory, "../../..");
const configuration = process.argv[2] === "Debug" ? "Debug" : "Release";

const targets = {
  "darwin-arm64": { rid: "osx-arm64", triple: "aarch64-apple-darwin", executable: "CrossBusExplorer.Host" },
  "darwin-x64": { rid: "osx-x64", triple: "x86_64-apple-darwin", executable: "CrossBusExplorer.Host" },
  "linux-arm64": { rid: "linux-arm64", triple: "aarch64-unknown-linux-gnu", executable: "CrossBusExplorer.Host" },
  "linux-x64": { rid: "linux-x64", triple: "x86_64-unknown-linux-gnu", executable: "CrossBusExplorer.Host" },
  "win32-arm64": { rid: "win-arm64", triple: "aarch64-pc-windows-msvc", executable: "CrossBusExplorer.Host.exe" },
  "win32-x64": { rid: "win-x64", triple: "x86_64-pc-windows-msvc", executable: "CrossBusExplorer.Host.exe" },
};

const target = targets[`${platform}-${arch}`];
if (!target) throw new Error(`Unsupported sidecar target: ${platform}-${arch}`);

const binariesDirectory = join(desktopDirectory, "src-tauri", "binaries");
const publishDirectory = join(binariesDirectory, "publish", target.rid);
const destination = join(
  binariesDirectory,
  `crossbus-host-${target.triple}${platform === "win32" ? ".exe" : ""}`,
);

mkdirSync(publishDirectory, { recursive: true });
rmSync(destination, { force: true });

execFileSync(
  "dotnet",
  [
    "publish",
    join(repositoryDirectory, "src", "Api", "Host", "Host.csproj"),
    "--configuration",
    configuration,
    "--runtime",
    target.rid,
    "--self-contained",
    "true",
    "--output",
    publishDirectory,
    "-p:PublishSingleFile=true",
    `-p:PublishTrimmed=${configuration === "Release" ? "true" : "false"}`,
    "-p:TrimMode=partial",
    "-p:JsonSerializerIsReflectionEnabledByDefault=true",
    "--nologo",
  ],
  { stdio: "inherit", cwd: repositoryDirectory },
);

copyFileSync(join(publishDirectory, target.executable), destination);
console.log(`Prepared ${destination}`);
