# 0001 — Importer service hosting and DI lifetimes

**Status:** Accepted (2026-08-21)

## Context

The `user/poojan/add-importer` branch added the Kavita Importer (watch an import
folder, detect media formats, organize files into the library) but was left
mid-debug and did not compile. Three structural questions had to be settled to
make it work:

1. **Where does the controller live?** The first draft placed `ImportController`
   in `Kavita.API/Controllers/`, but `Kavita.API` cannot reference
   `Kavita.Services` (the dependency runs the other way: Services → API), so the
   controller could never resolve the import services. Every other controller in
   the solution lives in `Kavita.Server/Controllers/`.
2. **What lifetime do the import services get?** They were registered `Scoped`,
   but `KavitaImporterService` owns a `FileSystemWatcher` and an in-memory
   import-status dictionary. A scoped instance dies with the HTTP request, so
   `POST /monitoring/start` would start a watcher that is torn down moments
   later and statistics would always read empty.
3. **One source tree or two?** The service sources existed byte-for-byte in both
   `Kavita.Services/Import/` and `Kavita.API/Controllers/Import/Import/`,
   declaring the same types in the same namespaces across two assemblies.

Alternatives considered and rejected:

- *Keep services scoped, hold state in a static* — hides the lifetime problem
  instead of fixing it, and makes testing worse.
- *Make Kavita.API reference Kavita.Services* — creates a project-reference
  cycle; impossible.
- *IHostedService for the watcher* — cleaner long-term shape for background
  monitoring, but a larger refactor than needed to make the feature function;
  noted as future work in the work log.

## Decision

- Delete the duplicated `Kavita.API/Controllers/Import/Import/` tree; the
  single source of truth is `Kavita.Services/Import/`.
- Move `ImportController` + DTOs to `Kavita.Server/Controllers/`
  (namespace `Kavita.Server.Controllers`), matching every other controller.
- Register the four import services as **singletons**. To avoid captive
  dependencies, `IDirectoryService` and `IFileSystem` (both stateless) are also
  promoted to singletons.
- `KavitaImporterService` self-initializes lazily (`InitializeCoreAsync`) from
  both `ImportAsync` and `StartMonitoringAsync`, so no startup hook is required.

## Consequences

- Monitoring state, tracked files, and statistics survive across requests; the
  feature works via the REST API alone (verified end-to-end).
- `IDirectoryService` is now shared process-wide. It is stateless (immutable
  path properties over `IFileSystem`), so this is safe, but any future mutable
  state added to it would become a concurrency concern.
- The importer still holds import status only in memory — a restart loses the
  tracked-file list (not the organized files). Persistence is future work.
