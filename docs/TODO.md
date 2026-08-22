# TODO

## Roadmap

### High Priority

- [ ] Review + commit importer fixes on `user/poojan/add-importer` (see docs/log/20260821-183000-book-importer-fix.md)
- [ ] Decide: auto-organize watcher-detected files (currently only tracked; organization runs on explicit import)

### Medium Priority

- [ ] Importer Phase 3: UI (dashboard, settings panel, live monitoring)
- [ ] Persist import status (in-memory only; lost on restart)
- [ ] Consider IHostedService for the import folder watcher

### Low Priority

- [ ] Clean up stale BookImport-* docs on main (never-built spec, reads as shipped)
- [ ] Dependency vulns flagged by NuGet audit (AutoMapper 12.0.1, System.Security.Cryptography.Xml, MailKit, Microsoft.AspNetCore.DataProtection)

## Open Issues

### Bugs

- [ ] Pre-existing test failure: ReadingHistoryServiceTests.CreatesForYesterdaySessions (fails on main too; date-boundary suspect)

### Enhancements
