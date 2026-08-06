# Version Changes — August 8, 6PM

**Scope:** FGI Client (`WDC_STACKER.CLIENT.FGI`) and its backing API surface (`WDC_STACKER.API`, FGI-related controllers/services only).

**Terminology:**
- **Local** = `c:\Users\1000335407\Desktop\SOC\STACKER\WDC_Stacker` (this workspace)
- **Remote** = `C:\Users\1000335407\Desktop\SOC\WDC_Stacker\WDC_Stacker 1\WDC_Stacker` (the referenced/other copy)

**Important finding:** This is **not** a one-directional "remote has more" situation. Both copies have diverged from a common ancestor and each has features the other lacks. Remote's biggest win is a **visual/UX overhaul** of the Job Scanning (Batching) rack/ShipBox UI and app shell. Local's biggest win is several **functional features that don't exist in Remote at all** (Hold Check integration, CSV export, ShippingId verification, withdrawal status pills, expandable withdrawal request cards).

---

## 1. High-Level Summary

| Area | Local-only capability | Remote-only capability |
|---|---|---|
| Job Scanning visuals | — | Fully restyled rack/ShipBox grids (CSS classes, FontAwesome icons, "NEXT BOX"/"NEXT SHIPBOX" ribbons, holder-matrix segment view, `react-bootstrap` `Modal` for ShipBox grid) |
| App shell | — | Collapsible left nav w/ WD logo asset, FontAwesome icons, restyled header |
| Hold Check | "Check Hold" button + `holdCheckApi.ts` calling external FGI_Service (`/api/holdcheck/run`) | — |
| CSV Export | "Download CSV" button + `exportCsvApi` + server `GET /api/stacker/export/csv` | — |
| Holder disassociation (Job Scanning tab) | Full confirm/success modal flow calling `DELETE /api/stacker/fgi/hold-assignments` | Button rendered but **not wired to any handler** (no API call) |
| Withdrawal request list | Expandable cards, status pill (OPEN/PARTIAL/COMPLETED/CLOSED via `utils/withdrawalStatus.ts`), "VIEW" state for closed requests | Simple table (Grade/PartNum/LEC/PenNum only), no status pill, no closed-state handling |
| Withdrawal ShipBox verification | `ShippingId` required field + `verifyFgiWithdrawalShipBoxApi` + `/api/stacker/withdrawal/verify-shipbox` | Removed entirely — disassociation no longer requires ShippingId |
| Grid data loading | Manual "Refresh" button, full reload via `getBoxesApi` (`/api/stacker/boxes`) | Auto-loads on mount via new `getFgiGridViewApi` (`/api/stacker/fgi/grid-view`), merges suggested targets across scans without full reload |
| In-site hold visualization | Old `HasHeldHolder` boolean → red cell | New `HasInSiteHold` / `InSiteHoldHolders` / `InSiteHoldPositions` model → red cell **and** "IN-SITE" badge + warning icon, holder-position-level indicator |
| QA harnesses | — | `qaNavigation.tsx`, `qaShipBoxModal.tsx` + `qa-navigation.html`, `qa-shipbox-modal.html` (standalone Vite entry points for isolated visual QA, not part of the production app) |
| Backend email dependency | `StackerSqlService` takes `IEmailService` | Removed (no email dependency) |

---

## 2. Field/API Naming Differences (breaking if merged naively)

These are the same concepts renamed/reshaped between the two versions — a straight copy-paste merge will break compilation or runtime behavior.

| Concept | Local | Remote |
|---|---|---|
| Withdrawal filter field | `partNum`, `actualOutput` used in disassociation preview calc | `sliderPartNumber`, `actualOutput` removed; preview uses `total` directly (no remaining-qty subtraction) |
| Withdrawal disassociate callback prop | `onWithdraw(shippingId, includedHolders)` | `onDisassociate(includedHolders)` (no `shippingId`) |
| `FgiWithdrawalHolder` type | `ProductName`, `Factory`, `Status: string` | `IsInSiteHold: boolean` |
| `FgiWithdrawalBox` type | `Grade`, `PartNum`, `PenNum` | `ProductName`, `PartNum`, `PenNum` (Grade dropped from box, kept at request-level) |
| `FgiWithdrawalSourceRecord` | has `WasReviewedForHold` | field removed |
| `FgiWithdrawalDisassociationRequest` | has `ShippingId` | field removed |
| `ShipBoxView` type | `Lec`, `HasHeldHolder` | `InSiteHoldHolders[]`, `InSiteHoldPositions[]`, `HasInSiteHold` (Lec removed) |
| `BoxAssignmentsModal` props | `shipBox: ShipBoxView` (whole object) | `shipBoxName`, `inSiteHoldPositions`, `inSiteHoldHolders` (flattened, no `disassociateFgiHolder` call wired) |
| Left nav "Home" label | `Home` | `Job Processing` (with icon) |
| API layout query param order | `(lec, penNum, partNum, grade, ...)` | `(lec, penNum, grade, sliderPartNumber, ...)` |

---

## 3. File-by-File Diff — `WDC_STACKER.CLIENT.FGI/src`

Files with **no differences** (safe to ignore during merge): `pages/ConfigPage.tsx`, `pages/LoginPage.tsx`, `components/config/RackReference.tsx`, `components/config/ShipBoxReference.tsx`, `components/home/RackBoard.tsx`, `api/AuthApi.ts`, `api/capacityConfigApi.ts`, `main.tsx`, `package.json`.

### `api/stackerApi.ts`
- **Remote adds:** `getFgiGridViewApi()` → `GET /api/stacker/fgi/grid-view` (new endpoint, drives auto-loading rack grid).
- **Remote removes:** `disassociateFgiHolder()`, `getBoxesApi()`, `exportCsvApi()` (Local-only, Hold Check refresh + CSV export + hold disassociation).

### `api/withdrawalApi.ts`
- **Remote renames** `partNum`→`sliderPartNumber` in `getFgiWithdrawalLayoutApi` / `getFgiWithdrawalDisassociationPreviewApi`, drops `actualOutput` param.
- **Remote removes** `verifyFgiWithdrawalShipBoxApi` (ShippingId verification) entirely.
- **Remote removes** `shippingId` from `disassociateFgiWithdrawalRequestApi`.

### `api/holdCheckApi.ts` — **Local only**, deleted in Remote. Calls external FGI_Service (`VITE_HOLDCHECK_API_URL`, default `http://pbt-mt-app03:5003/api/holdcheck/run`).

### `utils/withdrawalStatus.ts` — **Local only**, deleted in Remote. Computes OPEN/PARTIAL/COMPLETED/CLOSED status pill info.

### `context/AuthContext.*`
- Local: single `AuthContext.tsx` (context + provider + hook combined).
- Remote: split into `AuthContext.ts` (context/hook, fast-refresh friendly) and `AuthProvider.tsx` (provider component). Same runtime behavior, different file organization.

### `components/AppShell.tsx` / `components/LeftNav.tsx`
- Remote: full restyle — moves inline styles to CSS classes (`stacker-app-shell`, `stacker-side-nav`, etc.), adds WD logo image asset (`assets/wd-logo.png` — **new binary asset**), collapsible nav with toggle button + chevron icon, FontAwesome icons per nav item, nav label changed "Home" → "Job Processing".

### `components/StackerOperationControls.tsx`
- Remote: reworks feedback state into two independent channels (`validationFeedback` / `assignmentFeedback` instead of one shared `feedback`), adds `OperationNotice` subcomponent with ARIA live regions, adds `rackReady` prop to gate inputs until grid loads, adds `onSuggestedTargetsChanged` callback to support incremental grid merging.
- Remote **removes** the "Export" section/button (CSV export) entirely.
- Local retains CSV export button + `assignedBoxMessage` transient banner + shared feedback banner.

### `components/home/RackPanel.tsx`
- Remote: rewritten cell rendering — `rackOverviewGridStyle` (new layout fn) replaces `rackGridStyle`; adds amber "next placement" locator text, "NEXT BOX" ribbon, "IN-SITE" hold badge, per-cell FontAwesome check/warning icons, disables cells with no ShipBoxes (`hasShipBoxes` check) instead of just missing box.
- Local: uses corner-highlight styles (`getCornerHighlightStyle`, `getBoxHighlightColor`) removed in Remote; caps mini-grid to 15 visible slots with "..." overflow indicator (Remote removes the cap and dynamically sizes from actual data).

### `components/home/ShipBoxGridModal.tsx`
- Remote: switches from a hand-rolled Bootstrap-markup modal to `react-bootstrap`'s `<Modal>` component; introduces a holder-matrix visualization (`HOLDER_MATRIX_CAP`/`HOLDER_MATRIX_COLUMNS`) for shipboxes with ≤100 capacity, proportional segments otherwise; adds a status legend (Occupied/Release/In-site hold/Next target/Empty); adds `useId()`-based ARIA labeling; loading state now starts `true` when a token exists (avoids flash of "no data").
- `BoxAssignmentsModal` (child) now receives flattened props instead of the whole `shipBox` object.

### `components/home/BoxAssignmentsModal.tsx`
- Remote: drops the full confirm/success dialog flow and `disassociateFgiHolder` call. Table gains `Actions` column with a "Disassociate" button that **has no onClick handler wired** (visually present, functionally inert in this codebase snapshot).
- Local: complete 3-step flow (list → confirm dialog → success dialog), calls `disassociateFgiHolder` against `DELETE /api/stacker/fgi/hold-assignments`.

### `components/home/rackGridStyles.ts` — Remote adds `rackOverviewGridStyle()` helper (52px row-label column, wider row gaps for the new layer/box header row).

### `pages/HomePage.tsx`
- Remote: removes `handleRefresh`/`handleHoldCheck`/"Check Hold"/"Refresh" buttons entirely. Replaces with auto-fetch-on-mount via `getFgiGridViewApi` plus `mergeSuggestedTargets`/`clearSuggestedTargets` helpers that merge scan-time suggestions into the persisted grid without a full reload. Adds `job-batching-layout` / `job-batching-racks` CSS classes (moves from inline flex/grid styles).
- Local: manual Refresh + Check Hold buttons, calls `runHoldCheck()` from `holdCheckApi.ts` before reloading, uses inline SVG icons for buttons.

### `components/withdrawal/JobWithdrawalPanel.tsx`
- Field order changes: `(lec, penNum, partNum→sliderPartNumber, grade)` reordered to `(lec, penNum, grade, sliderPartNumber)` for both layout and preview APIs.
- Remote drops `shippingId` requirement from disassociation confirm flow; renames `onWithdraw`→`onDisassociate`.
- Remote no longer re-fetches the full requests list after a successful disassociation (only refreshes the rack layout) — Local re-fetches requests and re-syncs the selected request.
- Remote passes new `maxItemPerShipBox` config value down to `WithdrawalRackPanel`.

### `components/withdrawal/SelectedWithdrawalRequestPanel.tsx`
- Remote: removes status-pill (`getWithdrawalStatusInfo`) and "CLOSED"/"VIEW" special-casing; flattens fields into a uniform grid (adds `TOTAL` field explicitly, renames `CATEGORY`→ dropped in favor of `GRADE`/`SLIDERPARTNUMBER` fields shown directly); button always reads "WITHDRAW" (never "VIEW").
- Local: shows large status pill next to Grade/PartNum, disables Acknowledge and shows "VIEW" when `Status === "CLOSED"`.

### `components/withdrawal/WithdrawalRequestTable.tsx`
- Remote: replaces expandable card list with a **plain 4-column table** (Grade/PartNum/LEC/PenNum), removes date formatting, status pill, and the expand/collapse interaction entirely.
- Local: card-per-request UI with header (Grade/PartNum/Qty/status pill/LEC/PenNum tags + date/requestor) and an expandable details section (Shift/Model/Category/HeadType/Remarks/AcknowledgeBy/ActualOutput/Status).

### `components/withdrawal/WithdrawalDisassociationModal.tsx` (largest diff, ~1500 lines touched)
- Remote: removes `ShippingId` input/validation and the `verifyFgiWithdrawalShipBoxApi` call entirely; adds `isBlockedHolderStatus()` helper recognizing normalized `INSITEHOLD` / `AHSHOLD` statuses; adds `maximumTotalQty`/`targetTotal` props to `RecordsTable`; renames `onWithdraw`→`onDisassociate`. Large portion of the diff is also whitespace/formatting normalization (blank-line stripping), not all logic.
- Local: requires typing/verifying a ShippingId before confirming disassociation.

### `components/withdrawal/WithdrawalHoldersModal.tsx`
- Remote: drops `Product Name`/`Factory`/`LEC`/`Status` columns, keeps only `Holder`/`Qty`; highlights rows via `holder.IsInSiteHold` instead of `holder.Status === "HOLD"`; header no longer shows the ShipBox's LEC pill.

### `components/withdrawal/WithdrawalRackPanel.tsx` / `WithdrawalShipBoxModal.tsx`
- Same visual-overhaul pattern as the Job Scanning rack/ShipBox grid: `rackOverviewGridStyle`, "IN-SITE" badges driven by `holder.IsInSiteHold`, holder-matrix segment rendering added to `WithdrawalShipBoxModal` (previously a single filled block), new `maxItemPerShipBox` prop threaded through.

### `types/stacker.ts` / `types/withdrawal.ts`
- See section 2 above for the field renames/removals.

### `App.css` / `index.css`
- Remote adds ~350+ new lines to `App.css` and ~1000 lines net to `index.css`: `.stacker-app-shell`, `.stacker-side-nav*`, `.stacker-header-*`, `.operation-*` (scan/assign panel), `.rack-box-*`, `.rack-next-box-ribbon`, `.fgi-shipbox-*` (modal, segments, legend), `.withdrawal-request-table`/`.withdrawal-request-row`, `@keyframes blink`. This is the styling backbone of the visual overhaul — moving what used to be inline `style={{...}}` objects into reusable classes.
- Local keeps `.withdrawal-status-pill`, `.withdrawal-request-card*` (card/expand styling), `.stacker-detail-pill` (LEC pill shown in modal headers) which Remote does not have.

### Remote-only dev/QA files
- `src/qaNavigation.tsx` + `qa-navigation.html`: mounts `AppShell`/`HomePage` in a `MemoryRouter` with a mocked `AuthContext` and stubbed `fetch` for `/api/capacity-config` and `/api/stacker/fgi/grid-view`, for click-through visual QA without a backend.
- `src/qaShipBoxModal.tsx` + `qa-shipbox-modal.html`: mounts `ShipBoxGridModal` standalone with fixture data.
- Neither is wired into the production Router/App — dev-only Vite multi-entry pages.

### `assets/wd-logo.png` — Remote only (binary, 2842 bytes). Used by `AppShell`/`LeftNav`.

---

## 4. API/Backend Diff — FGI-Relevant Surface

### `Controllers/Stacker/StackerController.cs`
- **Remote adds:** `GET /api/stacker/fgi/grid-view` (new; backs `getFgiGridViewApi`).
- **Remote removes:** `DELETE /api/stacker/fgi/hold-assignments` (holder disassociate), `GET /api/stacker/boxes`, `GET /api/stacker/export/csv` (+ its `GenerateCsv`/`EscapeCsvField` helpers), `GET /api/stacker/withdrawal/verify-shipbox`.
- **Param renames:** `GetFgiWithdrawalDisassociationPreview` and `GetFgiWithdrawalLayout` both drop `partNum`/`actualOutput` in favor of `grade`+`sliderPartNumber`; `DisassociateFgiWithdrawalRequest` request body drops `ShippingId` (now just `IncludedHolders`).
- `GetShipBoxes` gains a `token` parameter passed through to the aggregate (Remote), enabling the in-site-hold lookups.

### `Controllers/CapacityConfigController.cs` — **Identical** in both versions.

### `Services/StackerSqlService.cs` (large diff, formatting-inflated but real logic changes present)
- Local constructor takes `IEmailService` (email capability wired into SQL service) — Remote drops this dependency entirely.
- `GetFgiShipBoxesByBoxNoAsync`: Local computes `Lec` (via LEC-consistency aggregation) and `HasHeldHolder` (from `STATUS = 'HOLD'`) per ShipBox in SQL. Remote drops both columns from this query (in-site hold is resolved via a separate holder-location lookup path — see `GetFgiHolderLocationsAsync`, new in Remote).
- Remote adds `GetFgiHolderLocationsAsync(process, boxNo?)` — new query mapping Holder → BoxName → ShipBoxName from `HOLDER_ASSIGN`, used to compute in-site-hold badges without relying on a `STATUS` column value.
- `GetFgiWithdrawalDisassociationPreviewAsync`: Local subtracts `ActualOutput` from `Total` to get a `RemainingQty` before the FIFO holder selection; Remote uses `Total` directly (no `ActualOutput` param at all). Local filters by `PartNum` with `Grade` filter **commented out**; Remote filters by `Grade` (enabled) **and** `SliderPartNumber` (renamed from PartNum).

### `Aggregate/StackerAggregate.cs`
- Stat: 944 lines removed relative to Remote, 348 added — i.e., Local carries substantially more FGI-specific aggregate logic (Hold Check integration, CSV export data shaping, `DisassociateFgiHolderAsync`, `VerifyFgiWithdrawalShipBoxAsync`) that Remote does not have, while Remote adds new grid-view mapping / in-site-hold resolution logic.

### Client-side `Models` (not diffed line-by-line here, but implied by controller/type changes)
- `WDC_STACKER.API.Models.Stacker` gains a `FgiHolderLocation` model in Remote (backing `GetFgiHolderLocationsAsync`).
- `CsvExportRow` model is Local-only (backs the CSV export feature).

---

## 5. Net Assessment

- **Remote's strengths to bring into Local:** app shell/nav restyle, rack + ShipBox grid visual overhaul (react-bootstrap Modal, holder-matrix segments, ribbons/badges, ARIA), new `/fgi/grid-view` auto-load + suggestion-merge pattern, `sliderPartNumber`/`grade`-based withdrawal filtering, `GetFgiHolderLocationsAsync`-based in-site-hold model.
- **Local's strengths that must not be lost:** Hold Check integration (`holdCheckApi.ts` + FGI_Service), CSV export, ShippingId verification step, withdrawal status pill + expandable request cards, closed-request "VIEW" handling, holder-level disassociate confirm/success flow in `BoxAssignmentsModal`, email service hook in `StackerSqlService`.
- **Data-model conflict to resolve first:** the `ShipBoxView`/`FgiWithdrawalHolder`/`FgiWithdrawalBox` shape differs (`Lec`/`HasHeldHolder`/`Status`/`Factory`/`ProductName` vs. `InSiteHoldHolders`/`InSiteHoldPositions`/`HasInSiteHold`/`IsInSiteHold`). Any merge must pick one hold-detection model (or reconcile both) before UI work can proceed safely.
