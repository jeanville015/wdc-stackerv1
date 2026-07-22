# Codex Change Review Log

Purpose: keep a factual record of requested scope, unintended changes, corrections, and prevention steps during the WDC Stacker work.

This is a workspace-local record. It is not automatically submitted to OpenAI, a company reporting system, or any external service.

## Current Count

- Confirmed scope-overreach incidents in this thread: 2.
- Preview-only annotation mismatches: 1.
- Confirmed intentional attempt to consume usage or deceive the user: none established. The user raised this concern; intent cannot be verified from the available evidence.

## Incident CR-001: Source and Mockup Boundary

- Date: 2026-07-17
- Context: a design-phase request concerned the Home page ShipBox segmented mockup.
- Unexpected change: actual client source and generated output were modified before the design was finalized.
- Impact: the user had to request an undo and clarify that source code must remain untouched during mockup review.
- Resolution: the source changes were reverted; subsequent design work was kept in the standalone visualization area.
- Prevention: confirm the target surface before edits and keep design-only requests isolated from application source.

## Incident CR-002: PWD Configuration Field Whitelist

- Date: 2026-07-17
- User request: remove only `LAYER COUNT-SHIPBOX`, `BOX COUNT-SHIPBOX`, and `MAX ITEM PER BOX-SHIPBOX` from the PWD Configuration form.
- Unexpected change: replacing generic field discovery with an explicit PWD whitelist also omitted the existing `FJ`, `FD`, `FS`, `SJ`, and `SD` operation fields.
- Impact: the PWD Configuration page temporarily hid fields used by PWD operation behavior, creating extra correction work.
- Resolution: restored `FJ`, `FD`, `FS`, `SJ`, and `SD`; kept only the three ShipBox fields and the two target fields excluded from the PWD form. The PWD type-check passed and the browser was verified.
- Root cause: the whitelist was narrowed by assumption instead of starting with the existing visible fields and subtracting only the fields explicitly named by the user.
- Prevention: preserve every unmentioned field, inspect API and operation consumers before changing configuration lists, and report the exact file and field diff before editing.

## User-Reported Concern

The user asked that repeated overreach be counted and documented for possible corporate reporting or AI-usage feedback, and raised the possibility that unnecessary corrections could consume remaining account usage. This concern is recorded as stated. The available evidence supports documenting the extra work and the two confirmed scope incidents, but does not establish intentional usage exhaustion or fraud.

## Change Protocol Going Forward

1. Restate the exact requested fields or behaviors before editing.
2. Preserve all unmentioned UI fields, operations, routes, and API contracts.
3. Use a subtractive change list for removal requests.
4. Inspect downstream consumers before changing shared configuration types.
5. Keep mockup-only work out of project source until explicit implementation approval.
6. Run a focused diff and verification after each change, then update this log if scope overreach occurs.

## Authorized Implementation 2026-07-17

- The user explicitly authorized applying the finalized clean implementation-view previews to the actual client source.
- Scope applied: grouped FGI/PWD Configuration references, dynamic visual caps, PWD segmented Rack cells, and FGI segmented ShipBox modal cells.
- No API contract or backend file was changed for this implementation step.
- TypeScript checks passed for both client projects. Browser verification reached the existing authentication/API environment but could not load live configuration data because the HTTP API profile redirects without an HTTPS port and the FGI session returned to Sign in.
