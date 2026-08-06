# FGI Client/API Merge Plan — Phased by Module

Companion to `Version changes - august 8 6pm.md`. Goal: bring Remote's visual/UX improvements into Local **without losing** Local's Hold Check, CSV export, ShippingId verification, and withdrawal-status features. Each phase is scoped to be independently testable and small enough to review in one sitting. Do not start a phase until the previous one is merged and smoke-tested.

**Ground rule:** Before Phase 1, decide the target data model for "hold" detection (see Phase 0). Every later phase depends on this decision.

---

## Phase 0 — Data Model Reconciliation (no UI changes)
**Files:** `types/stacker.ts`, `types/withdrawal.ts`, `Models/Stacker/*`, `Services/StackerSqlService.cs` (query layer only)

1. Decide: keep Remote's `InSiteHoldHolders[] / InSiteHoldPositions[] / HasInSiteHold` model (recommended — it's position-aware and supports the new badge/matrix UI) instead of Local's `HasHeldHolder` boolean.
2. Port Remote's `GetFgiHolderLocationsAsync` query + `FgiHolderLocation` model into Local's `StackerSqlService`/`Models`, alongside (not replacing yet) the existing `HasHeldHolder`/`Lec` computation.
3. Update `ShipBoxView`, `FgiWithdrawalHolder`, `FgiWithdrawalBox` types to add the new in-site-hold fields **without removing** `Lec`/`HasHeldHolder`/`Status`/`Factory`/`ProductName` yet (additive change, both models coexist temporarily).
4. Keep `IEmailService` dependency in `StackerSqlService` — do not drop it.

**Test:** Existing endpoints still return old fields; new fields populate alongside. No client changes yet, so no visual regression possible.

---

## Phase 1 — App Shell & Navigation (low risk, high visual impact)
**Files:** `components/AppShell.tsx`, `components/LeftNav.tsx`, `assets/wd-logo.png`, `App.css` (shell/nav classes only)

1. Copy `assets/wd-logo.png` from Remote into Local.
2. Port `LeftNav.tsx` (collapsible nav, FontAwesome icons, toggle button) — keep Local's nav label ("Home") or adopt Remote's ("Job Processing") per user preference.
3. Port `AppShell.tsx` restyle (header markup + `stacker-app-shell`/`stacker-app-column`/`stacker-header-*` classes).
4. Copy only the shell/nav-related CSS blocks from Remote's `App.css`/`index.css` (do not bulk-copy the whole file yet — cherry-pick `.stacker-app-shell`, `.stacker-side-nav*`, `.stacker-header-*`).

**Test:** Login → verify header, logo, nav collapse/expand, and route navigation (`/`, `/config`) still work. No functional/API changes in this phase.

---

## Phase 2 — Job Scanning: Operation Controls Panel
**Files:** `components/StackerOperationControls.tsx`, `App.css`/`index.css` (`.operation-*` classes)

1. Port the two-channel feedback model (`validationFeedback` / `assignmentFeedback`) and `OperationNotice` component.
2. Add `rackReady` prop plumbing (wire it up in Phase 4 when HomePage changes) — safe to add now with a default of `true` so behavior is unchanged until Phase 4.
3. **Keep** Local's CSV Export button/section and `exportCsvApi` call — do not delete. Merge it into the restyled panel using the new `.operation-section` class pattern instead of the old inline-style block.
4. Port `.operation-*` CSS classes.

**Test:** Scan/Validate/Assign flow works identically; CSV export button still present and functional; visual style matches Remote's panel look.

---

## Phase 3 — Job Scanning: Rack & ShipBox Grid Visuals
**Files:** `components/home/rackGridStyles.ts`, `components/home/RackPanel.tsx`, `components/home/ShipBoxGridModal.tsx`, `components/home/BoxAssignmentsModal.tsx`, `index.css` (`.rack-*`, `.fgi-shipbox-*` classes), `package.json` (confirm `react-bootstrap` is actually resolvable — add as explicit dependency if it's only transitive today)

1. Add `rackOverviewGridStyle()` to `rackGridStyles.ts` (additive, keep `rackGridStyle` for now in case anything else references it).
2. Port `RackPanel.tsx` visual rewrite (ribbons, badges, icons). Wire `HasInSiteHold`/`InSiteHoldHolders` from Phase 0's additive fields instead of `HasHeldHolder`.
3. Port `ShipBoxGridModal.tsx` (react-bootstrap `Modal`, holder-matrix segments, legend). **Re-integrate** Local's `BoxAssignmentsModal` confirm/success dialog + `disassociateFgiHolder` call into the new flattened-props version — Remote's version left the "Disassociate" button non-functional; do not ship that regression.
4. Verify `react-bootstrap` renders correctly (install/pin if missing from `package.json`).

**Test:** Open a Box → ShipBox grid renders with new visuals → open holder assignments → disassociate a held holder end-to-end (confirm dialog → API call → success dialog → row removed). Compare against Local's current working disassociate flow before merging to confirm no functional loss.

---

## Phase 4 — Job Scanning: HomePage Data Flow
**Files:** `pages/HomePage.tsx`, `api/stackerApi.ts`, `Controllers/Stacker/StackerController.cs`, `Aggregate/StackerAggregate.cs`

1. Add `getFgiGridViewApi()` client function + `GET /api/stacker/fgi/grid-view` endpoint + aggregate method (`MapGridViewBoxData` reuse, per Remote).
2. Port `mergeSuggestedTargets`/`clearSuggestedTargets` helpers and auto-load-on-mount `useEffect`.
3. **Keep** Local's "Check Hold" button and `runHoldCheck()` call — re-attach it as an explicit action button in the restyled `job-batching-layout`, triggering a grid refetch via the new `getFgiGridViewApi` (instead of the old `getBoxesApi`) afterward.
4. Decide fate of `getBoxesApi` / `GET /api/stacker/boxes` and `exportCsvApi` / `GET /api/stacker/export/csv`: keep both server endpoints (CSV export still needs `GetAllHolderAssignmentsForCsvAsync`; Hold Check refresh can switch to the new grid-view endpoint, so `GetBoxes`/`/api/stacker/boxes` may become redundant — confirm no other caller before removing).
5. Wire `rackReady` (from Phase 2) to `!rackLoading && !rackError`.

**Test:** Full Job Scanning tab regression: page load auto-fetches grid, scan/validate merges suggested target without full reload, assign updates grid, Check Hold still works end-to-end, CSV export still downloads a file.

---

## Phase 5 — Withdrawal: Rack/ShipBox Visuals
**Files:** `components/withdrawal/WithdrawalRackPanel.tsx`, `components/withdrawal/WithdrawalShipBoxModal.tsx`, `components/withdrawal/WithdrawalHoldersModal.tsx`

1. Port `rackOverviewGridStyle` usage, in-site-hold badges (driven by `FgiWithdrawalHolder.IsInSiteHold`, additive field from Phase 0).
2. Port holder-matrix segment rendering into `WithdrawalShipBoxModal`.
3. Port `WithdrawalHoldersModal` simplification **carefully**: Remote drops `ProductName`/`Factory`/`LEC`/`Status` columns. Confirm with the user/business whether those columns are still needed before removing — if needed, keep them alongside the new `IsInSiteHold` row highlighting.
4. Thread `maxItemPerShipBox` prop from `useCapacityConfig` down through `JobWithdrawalPanel` → `WithdrawalRackPanel` → `WithdrawalShipBoxModal`.

**Test:** Open a withdrawal request → rack renders with new visuals → open a Box → ShipBox holder matrix renders → holders modal shows correct in-site-hold highlighting and (if kept) Product/Factory/LEC/Status columns.

---

## Phase 6 — Withdrawal: API Field Renames (`partNum` → `sliderPartNumber`, drop `actualOutput`/`ShippingId`)
**Files:** `api/withdrawalApi.ts`, `types/withdrawal.ts`, `Controllers/Stacker/StackerController.cs`, `Services/StackerSqlService.cs` (`GetFgiWithdrawalDisassociationPreviewAsync`, `GetFgiWithdrawalLayoutAsync`)

> This phase is functional/breaking, isolate it from visual phases so a regression is easy to bisect.

1. Rename `partNum`→`sliderPartNumber` consistently across `getFgiWithdrawalLayoutApi`, `getFgiWithdrawalDisassociationPreviewApi`, controller actions, and SQL params.
2. Re-enable the `Grade` filter in `GetFgiWithdrawalDisassociationPreviewAsync` (Local currently has it commented out) — confirm with business logic owner this is intended before enabling.
3. Decide: drop `ActualOutput`-based `RemainingQty` subtraction (Remote's simpler `Total`-only approach), or keep Local's remaining-qty behavior. **This changes how many holders get selected for disassociation** — needs explicit sign-off, not a silent merge.
4. Remove `ShippingId` from `FgiWithdrawalDisassociationRequest`/`verifyFgiWithdrawalShipBoxApi` **only if** the business no longer requires ShippingId verification. Otherwise, keep Local's verification step and layer the field-renames on top of it instead of deleting it.

**Test:** Run a disassociation end-to-end with known FIFO holder data; verify the same holders are selected as before the rename (regression-test the SQL math specifically, since #2/#3 change selection counts).

---

## Phase 7 — Withdrawal: Request List & Selected Panel UI
**Files:** `components/withdrawal/WithdrawalRequestTable.tsx`, `components/withdrawal/SelectedWithdrawalRequestPanel.tsx`, `utils/withdrawalStatus.ts`, `App.css` (`.withdrawal-request-table`/`.withdrawal-request-card*`)

1. Do **not** delete `utils/withdrawalStatus.ts` — it has no Remote equivalent and is a real UX feature (status pill).
2. Choose one request-list UI: keep Local's expandable card list (recommended, more information-dense) styled with Remote's new table classes as an alternative "compact" view, OR fully adopt Remote's simple table and re-add a status column to it. This is a product decision — flag to the user before implementing.
3. Port Remote's flattened field-grid layout in `SelectedWithdrawalRequestPanel` but retain the status pill and closed/"VIEW" state logic from Local.
4. Rename `onWithdraw`→`onDisassociate` prop names for consistency with Phase 6/8.

**Test:** Withdrawal tab: select various requests (open/partial/completed/closed) and confirm status pill, VIEW-vs-WITHDRAW button state, and expand/collapse (if kept) all behave as they did in Local before the merge.

---

## Phase 8 — Withdrawal: Disassociation Modal
**Files:** `components/withdrawal/WithdrawalDisassociationModal.tsx`, `components/withdrawal/JobWithdrawalPanel.tsx`

1. Apply the `onWithdraw`→`onDisassociate` rename and drop/keep `ShippingId` per the Phase 6 decision.
2. Port `isBlockedHolderStatus()` (`INSITEHOLD`/`AHSHOLD` detection) and `maximumTotalQty`/`targetTotal` props into `RecordsTable`.
3. Re-run Prettier/formatter on this file after merging — the Remote diff for this file is ~40% pure blank-line/formatting noise; do a clean reformat pass so the real logic diff is reviewable.
4. Update `JobWithdrawalPanel.tsx` call sites accordingly; decide whether to keep Local's post-disassociation full requests re-fetch (safer, slightly slower) or Remote's rack-only refresh (faster, relies on optimistic UI).

**Test:** Full disassociation flow with a request that has blocked (in-site/AHS hold) holders — confirm they're excluded/flagged correctly, and the modal's totals match expectations.

---

## Phase 9 — Cleanup & Dev Tooling
**Files:** `qaNavigation.tsx`, `qaShipBoxModal.tsx`, `qa-navigation.html`, `qa-shipbox-modal.html`, dead code removal

1. Optionally port Remote's QA harness pages (useful for isolated visual regression testing without a backend) — low risk, additive, not part of production bundle.
2. Remove any now-dead code paths (e.g., old `rackGridStyle`/`getCornerHighlightStyle` helpers if nothing references them after Phase 3/5; old `HasHeldHolder`/`Lec` fields from Phase 0 once nothing reads them).
3. Final full regression pass across both tabs (Job Scanning + Job Withdrawal) plus Config page (untouched, but confirm no shared CSS class collisions from the App.css/index.css merges).

---

## Suggested Order Recap

1. Phase 0 — data model (backend-only, no visible change)
2. Phase 1 — app shell/nav (cosmetic, isolated)
3. Phase 2 — operation controls panel (cosmetic + feedback UX)
4. Phase 3 — rack/ShipBox visuals (cosmetic, re-attach disassociate)
5. Phase 4 — HomePage data flow (functional, re-attach Hold Check)
6. Phase 5 — withdrawal rack/ShipBox visuals (cosmetic)
7. Phase 6 — withdrawal API field renames (functional/breaking — needs sign-off)
8. Phase 7 — withdrawal list/selected-panel UI (product decision needed)
9. Phase 8 — withdrawal disassociation modal (functional, depends on Phase 6)
10. Phase 9 — cleanup + QA tooling

Each phase should be its own commit/PR so a regression can be bisected to a single module.
