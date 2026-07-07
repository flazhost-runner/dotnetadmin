# Storage & switching backends

DotNetAdmin uses a pluggable storage adapter (`IStorageService`) with drivers
`local` / `oss` / `s3`, mirroring NodeAdmin. The database stores the object **key**;
the render URL is built at request time by the active driver, so switching backends is
a **config-only change (+ restart)** — no code or view edits.

- **local** → files under `Storage:BasePath`, served at the stable prefix `/storage/<key>`
  (static middleware registered only when driver=local, in `Program.cs`).
- **oss / s3** → absolute presigned URL with a limited TTL; no local serving.

Full guide (drivers table, switch steps, migration with `aws s3 sync` / `ossutil`,
deployment caveats): see the **"Storage & switching backends"** section in
[`../README.md`](../README.md). Config reference: [`../.env.example`](../.env.example).

Implementation: `src/Core/Storage/` (`IStorageService`, `LocalStorageService`,
`S3StorageService`, `OssStorageService`); DI selection in
`src/Core/Extensions/ServiceCollectionExtensions.cs`; local static mount in `Program.cs`.
