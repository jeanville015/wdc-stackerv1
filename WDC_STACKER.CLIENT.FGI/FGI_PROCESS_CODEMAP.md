# FGI Stacker — Process Codemap & Implementation Checklist

Scope: `WDC_STACKER.CLIENT.FGI` (React/TS client) and its backing `WDC_STACKER.API` endpoints/services that the FGI client depends on (identified via the `X-Stacker-Client: WDC_STACKER.CLIENT.FGI` header / `isFgi` branches in the API).

Legend: ✅ Implemented · 🟡 Partially implemented · ❌ Not implemented

---

## 1. Architecture Map

```
WDC_STACKER.CLIENT.FGI (React + Vite + TS)
├─ src/pages/LoginPage.tsx            → Login screen
├─ src/context/AuthContext.tsx        → session (sessionStorage) + user state
├─ src/api/AuthApi.ts                 → POST /api/auth/login
├─ src/components/ProtectedRoute.tsx  → route guard (+ config-access guard)
├─ src/components/AppShell.tsx        → shell layout wrapping LeftNav + <Outlet/>
├─ src/components/LeftNav.tsx         → nav; shows "Configuration" link only if canAccessConfiguration
├─ src/pages/HomePage.tsx             → Tabs: "JOB SCANNING - BATCHING" | "JOB WITHDRAWAL"
│   ├─ src/components/StackerOperationControls.tsx  → Scan + Assign controls
│   ├─ src/components/home/RackBoard.tsx            → Rack display (loops RACK_COUNT)
│   │   ├─ src/components/home/RackPanel.tsx        → Box/position display per rack
│   │   ├─ src/components/home/ShipBoxGridModal.tsx → ShipBox grid + selection
│   │   └─ src/components/home/BoxAssignmentsModal.tsx → holders inside a box
│   └─ src/components/withdrawal/JobWithdrawalPanel.tsx → Withdrawal workflow root
│       ├─ WithdrawalRequestTable.tsx
│       ├─ SelectedWithdrawalRequestPanel.tsx (Acknowledge / Disassociate buttons)
│       ├─ WithdrawalDisassociationModal.tsx (Shipping ID + Holder verify + Allocate)
│       ├─ WithdrawalRackPanel.tsx / WithdrawalShipBoxModal.tsx / WithdrawalHoldersModal.tsx
│       └─ src/api/withdrawalApi.ts
├─ src/pages/ConfigPage.tsx           → Config-only page (RackReference/ShipBoxReference)
└─ src/api/stackerApi.ts              → scanApi / assignApi

WDC_STACKER.API (ASP.NET Core)
├─ Controllers/AuthController.cs               → POST /api/auth/login
├─ Aggregate/AuthProjectionAggregate.cs         → AD auth + token issue + CanAccessConfigurationAsync
├─ Services/ActiveDirectoryService.cs           → AD credential validation
├─ Controllers/Stacker/StackerController.cs     → /api/stacker/* endpoints (scan, assign, withdrawal/*)
├─ Aggregate/StackerAggregate.cs                → business rules: ScanHolderJobAsync, AssignHolderAsync,
│                                                  MapGridViewBoxData, withdrawal orchestration
├─ Services/StackerSqlService.cs                → SQL Server persistence (HOLDER_ASSIGN, BOX, SHIPBOX, KITTING_REQUEST)
├─ Services/FeatsService.cs                     → FEATS SOAP wrapper (Query, MoveIn, MoveOut, HoldHolder available)
└─ Services/CapacityConfigService.cs            → CapacityConfig.json (ValidOperation, rack/box/ship-box sizing)
```

---

## 2. Login Module

| Plan Item | Code Location | Status |
|---|---|---|
| Login form / submit | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.CLIENT.FGI/src/pages/LoginPage.tsx:97-120` | ✅ |
| `POST /api/auth/login` | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Controllers/AuthController.cs:23-38` | ✅ |
| AD credential check | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Services/ActiveDirectoryService.cs:19-63` | ✅ |
| Invalid account → error message | `AuthController` returns `Unauthorized` on `!result.Success`; `LoginPage.tsx:112-116` displays `result.Message` / thrown error | ✅ |
| Login with User Group → Config tab enabled | `CanAccessConfigurationAsync` queries FEATS `UsersByPrivilege` for `TAP_FAB3ADMIN` and matches employee name — `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Aggregate/AuthProjectionAggregate.cs:64` and `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Aggregate/StackerAggregate.cs:30-48` | ✅ |
| Login without group → Config tab hidden | `LeftNav.tsx:105-109` conditionally renders nav link; `ProtectedRoute.tsx:19-21` redirects `/config` if `!user.canAccessConfiguration`; `App.tsx:28-35` wraps `/config` with `requireConfigurationAccess` | ✅ |
| Session persistence | `AuthContext.tsx:15-19,21-24` (sessionStorage) | ✅ |

**Notes:** "Incomplete" credential validation only checks non-empty client-side (`LoginPage.tsx:101-104`) and non-empty server-side (`AuthController.cs:26-30`) — no additional format validation (e.g. domain\username pattern) is enforced.

---

## 3. Job Scanning

Plan: Enter Holder → Validation (HolderJob query) → Batching.

| Sub-check | Code Location | Status |
|---|---|---|
| Scan input + call `POST /api/stacker/scan` | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.CLIENT.FGI/src/components/StackerOperationControls.tsx:91-138` → `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Controllers/Stacker/StackerController.cs:34-58` | ✅ |
| FEATS `HolderJob` query | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Aggregate/StackerAggregate.cs:580-619` | ✅ |
| Correct Operation check ("Not in Correct Operation") | `StackerAggregate.cs:678-698` — compares `Operation` to `CapacityConfig.ValidOperation`; message is `"Operation is not valid"` (not the exact "Not in Correct Operation" text) | 🟡 (logic present, message text differs, no MoveOut side-effect) |
| ParentHolder must be empty | Check exists but is **commented out** (`StackerAggregate.cs:~701-714`), guarded by "disable for now while on development/QA, enable once on UAT/PROD". Field is still fetched/queried. | ❌ (disabled by design, see `JOB_SCANNING_VALIDATIONS_CHANGES.md`) |
| ShipTicket must be empty | Check exists but is **commented out** (`StackerAggregate.cs:~716-729`), same disable comment as ParentHolder. | ❌ (disabled by design) |
| Holds check ("Holder is on hold" + MoveOut to RBF2) | **Now implemented directly in `ScanHolderJobAsync`.** Step 1: FEATS `HoldReason`/`HoldComment` must both be null (`StackerAggregate.cs:~885-908`). Step 2: loops `config.HoldValidationOperations` calling AHS `SliderCheck2Async`; any `EXISTS`/`ONHOLD` response triggers `MoveOutAsync` to `"735617 RBF2"` then fails (`StackerAggregate.cs:~910-1025`). | ✅ |
| Correct BinName check + MoveOut to RBF2 | Implemented for FGI: validates `binName?.Length != 5`, and calls `_featsService.MoveOutAsync(resource: "735617 RBF2")` on failure (`StackerAggregate.cs:~833-882`). | ✅ |
| Has PartNum check ("Holder has no Part Number") | Checks for empty/whitespace `PartNumber`; on failure now performs MoveOut to RBF2 **and** applies a Hold (see next row) rather than just returning a message (`StackerAggregate.cs:~731-831`) | ✅ (upgraded from 🟡) |
| Apply Hold (Reason='TAP', Comment='NO PART NUMBER') on missing part number | `FeatsService.HoldHolderAsync` wrapper implemented (`@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Services/FeatsService.cs:262-283`) and is called from `ScanHolderJobAsync` immediately after a successful MoveOut for missing PartNumber, with compensating-failure handling if Hold fails post-MoveOut (`StackerAggregate.cs:~774-803`) | ✅ |
| MoveOut to RBF2 (on failed checks) | `FeatsService.MoveOutAsync` is now called from three places inside the scan validation path: missing PartNumber, invalid BinName, and AHS hold/slider failure (`StackerAggregate.cs:~745, 845, 969`) | ✅ |
| InProcess check + MoveIn if not | Implemented as the last validation step: if `InProcess != "True"`, calls `FeatsService.MoveInAsync` (`@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Services/FeatsService.cs:227-248`) from `StackerAggregate.cs:~1042-1083`; fails scan if MoveIn fails | ✅ |
| "Validation Pass!" → continue to Batching | `StackerAggregate.cs:~1085+` returns `Message = "Validation Pass!"`; `StackerOperationControls.tsx:127` shows it and pre-selects suggested Box/ShipBox | ✅ |

**Summary (updated):** Operation match, PartNumber/ProductName + Hold/MoveOut, BinName correctness + MoveOut, two-step Hold validation (FEATS + AHS) + MoveOut, Holder-Qty validity, InProcess + MoveIn, and box-suggestion logic are all **implemented and enabled** for FGI scanning as of this review. Only ParentHolder and ShipTicket checks remain intentionally disabled pending UAT/PROD readiness. See `JOB_SCANNING_VALIDATIONS_CHANGES.md` for full detail, line-accurate references, and a process-flow diagram.

---

## 4. Job Batching

Plan: Enter Holder → Holds validation → Grouping-restriction validation → Select Rack/BlackBox/ShipBox → Enable Assign.

| Sub-check | Code Location | Status |
|---|---|---|
| Suggested Box/ShipBox auto-selection after successful scan | `StackerOperationControls.tsx:108-127` (`findSuggestedTarget`) | ✅ |
| Grouping restriction (same PartNum + PenNum + ProductName grouped into same Box; LEC-based ShipBox grouping) | `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Aggregate/StackerAggregate.cs:295-309` (`IsCompatibleFgiBox`), `:311-330` (`TrySelectSuggestedFgiTarget`), `:443-517` (`TryCreateNextFgiBox`) | ✅ |
| Enable "Assign" only when Box + ShipBox selected | `StackerOperationControls.tsx:73` (`canAssign`), `:343-346` disables button | ✅ |
| Assign Box Success → save Holder to DB in assigned box | `POST /api/stacker/assign` → `AssignHolderAsync` → `_stackerSqlService.InsertFgiAssignmentAsync` (`StackerAggregate.cs:754-1046`, `:1020-1042`) | ✅ |
| Holds validation before batching | Not independently re-checked inside `AssignHolderAsync`; relies on the Hold/AHS checks already enforced during the preceding `ScanHolderJobAsync` call for the same holder (no dedicated hold re-check at Assign time) | 🟡 (covered upstream at Scan, not re-verified at Assign) |
| Rack Display (correct # racks + box positions) | `RackBoard.tsx:47,61-83` renders `config.RACK_COUNT` racks, filters boxes per rack by `RackNum` | ✅ |
| Box Display (positions of filled holders) | `RackPanel.tsx` renders per-box grid using `LayerRowNum`/`LayerColNum`/`BoxListPercentage` (fetched via `GetFgiBoxListCountAndPercentageAsync`) | ✅ |
| ShipBox Display (moved-in/assigned/batched holders) | `ShipBoxGridModal.tsx` + `GET /api/stacker/boxes/{boxName}/shipboxes/{shipBoxName}/assignments` → `GetShipBoxAssignmentsAsync` | ✅ |

**Summary (updated):** Core batching/assignment and visual rack/box/ship-box displays are implemented. The plan's explicit "Holds validation" step prior to batching has no dedicated re-check at Assign time, but is effectively covered because the same holder must already pass the Hold/AHS checks during the preceding Scan call (unlike the prior revision of this doc, Holds are no longer a gap in Job Scanning itself).

---

## 5. Job Withdrawal

Plan: View Requests → Acknowledge → Disassociate → Enter ShipBox → Verify Valid ShipBox(NEW) → Fulfill by inventory → Scan Holders (check holds) → Allocate → AddJob() (NEW) → MoveOut/Ship → Update table → Send email.

| Sub-step | Code Location | Status |
|---|---|---|
| View Requests | `WithdrawalRequestTable.tsx` ← `getFgiWithdrawalRequestsApi` ← `GET /api/stacker/withdrawal/requests` → `StackerSqlService.GetFgiWithdrawalRequestsAsync` (`@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Services/StackerSqlService.cs:262-324`) | ✅ |
| Click Acknowledge → update `AcknowledgeBy` = current user | `SelectedWithdrawalRequestPanel.tsx` → `acknowledgeFgiWithdrawalRequestApi` → `PATCH .../acknowledge` → `AcknowledgeFgiWithdrawalRequestAsync` (`StackerAggregate.cs:1446-1478`) → SQL `UPDATE ... KITTING_REQUEST` (`StackerSqlService.cs:556-574`) | ✅ |
| Click Disassociate → build disassociation preview (FIFO candidate list + hold status) | `JobWithdrawalPanel.tsx:198-257` → `getFgiWithdrawalDisassociationPreviewApi` → `GET .../disassociation-preview` → `GetFgiWithdrawalDisassociationPreviewAsync` (`StackerAggregate.cs:1378-1427`) which also runs `CheckHolderInSiteHoldAsync` per included holder, tagging `HOLD PASS` / `IN-SITE HOLD` | ✅ |
| Enter ShipBox (validation) | `WithdrawalDisassociationModal.tsx:281,346-356` — "Shipping ID" input; validation only checks the field is **non-empty client-side**; no server-side lookup/verification of the shipping/ship-box ID against inventory or FEATS | 🟡 |
| List of available holders to fulfill request (List Success) | `RecordsTable` in `WithdrawalDisassociationModal.tsx:66-234` renders Included/Skipped FIFO records from the preview payload | ✅ |
| Scan each holder to verify (Scan Success) — check for holds | `handleHolderVerify` (`WithdrawalDisassociationModal.tsx:358-432`) only checks the typed holder text against the **already-loaded** `sourceRecords` list client-side; it does **not** call any scan/hold-check API at verification time (hold status was pre-computed once during preview load, not re-validated per scan) | 🟡 |
| Click Allocate | Button flow is `openDisassociationConfirmation` → confirm modal → `handleConfirmedDisassociation` → `onDisassociate(holders)` (`WithdrawalDisassociationModal.tsx:452-489`); UI label is a confirm dialog, not literally "Allocate" | 🟡 |
| Processing MoveOut/Ship ShipBox | `disassociateFgiWithdrawalRequestApi` → `POST .../disassociate` → `DisassociateFgiWithdrawalRequestAsync` (`StackerAggregate.cs:~1972-2058`). Order: (1) clears `_previewCache`, (2) `VerifyShipBox` (bypassed stub), (3) `AddJobForWithdrawalAsync` FEATS call — **implemented but disabled** via `#region ADD JOB TRANSACTION (comment out to disable)`, (4) FEATS `MoveOutAsync` — **implemented but disabled** via `#region MOVE-OUT TRANSACTION (comment out to disable)`, (5) SQL delete via `StackerSqlService.DisassociateFgiWithdrawalAsync`. Both FEATS calls are coded and ready but intentionally commented out for testing (unlike the Batching "Disassociate" action's `DisassociateHolderAsync`, which does call `_featsService.MoveOutAsync` unconditionally) | 🟡 (implemented, currently disabled by design — see `JOB_WITHDRAWAL_CHANGES.md`) |
| Update tables | `HOLDER_ASSIGN` rows are deleted transactionally with box/ship-box FIFO recheck (`StackerSqlService.cs:1444-1762+`) | ✅ (DB only) |
| Send email | A `//-- SEND EMAIL: START (placeholder) / END --` TODO marker exists in `DisassociateFgiWithdrawalRequestAsync` (`StackerAggregate.cs:~2053-2056`), and an `IEmailService` interface exists at `@c:/Users/1000335407/Desktop/SOC/STACKER/WDC_Stacker/WDC_STACKER.API/Interfaces/IEmailService.cs`, but no implementation is wired into the FGI withdrawal flow — no actual SMTP send occurs | ❌ (scaffolding present, not wired) |
| Rack/ShipBox layout view for selected request | `WithdrawalRackPanel.tsx` ← `getFgiWithdrawalLayoutApi` ← `GET .../withdrawal/layout` → `GetFgiWithdrawalLayoutAsync` (`StackerAggregate.cs:1480-1486`) | ✅ |

**Summary (updated):** Acknowledge, request listing, FIFO preview with **live FEATS in-site hold tagging** (the AHS hold check has been removed entirely — dead code, see `JOB_WITHDRAWAL_CHANGES.md`), and DB-only disassociation are implemented. Ship-box ID is not server-validated (stub), per-holder scan does not re-verify holds live, and the FEATS `AddJob`/`MoveOut` SOAP calls plus email send are all coded but intentionally disabled/placeholder pending sign-off (see `JOB_WITHDRAWAL_CHANGES.md` process flow and re-enable instructions).

---

## 6. Consolidated Checklist

### ✅ Implemented
- Login (AD auth, error messaging, token/session)
- Config-tab gating via FEATS `TAP_FAB3ADMIN` privilege group
- Holder scan → FEATS `HolderJob` query → Operation-match validation
- PartNumber/ProductName/Qty presence validation (FGI)
- Suggested Box/ShipBox auto-targeting with grouping (PartNum+PenNum+ProductName / LEC)
- Assign holder → persist to `HOLDER_ASSIGN`/`BOX`/`SHIPBOX` tables
- Rack, Box, and ShipBox visual displays (grid, percentage fill, positions)
- Withdrawal: view requests, acknowledge, FIFO disassociation preview w/ InSite hold tagging, rack/ship-box layout view
- Withdrawal: disassociation DB transaction (FIFO recompute + delete)

### 🟡 Partially Implemented
- Operation-mismatch message text differs from plan ("Operation is not valid" vs "Not in Correct Operation") and has no MoveOut side effect
- Withdrawal "Enter ShipBox" is a client-only non-empty check, no backend validation
- Withdrawal per-holder "scan to verify" is client-side list matching only, not a live hold re-check
- Withdrawal "Allocate" exists as a confirm-then-disassociate action, not a distinct allocation step
- Holds validation at Job Batching (Assign) relies on the preceding Scan call rather than an independent re-check

### ✅ Implemented (updated — previously listed as Not Implemented)
- Holds check during Job Scanning (FEATS HoldReason/HoldComment + AHS `SliderCheck2` loop) + MoveOut to RBF2
- BinName correctness check during Job Scanning (FGI) + MoveOut to RBF2
- Apply Hold (Reason=`TAP`, Comment=`NO PART NUMBER`) on missing part number, with compensating-failure handling
- InProcess check + conditional MoveIn during Job Scanning

### 🟡 Implemented but Intentionally Disabled
- FEATS `AddJob` transaction during Job Withdrawal disassociation (`#region ADD JOB TRANSACTION`, commented out)
- FEATS `MoveOut`/Ship SOAP call during Job Withdrawal disassociation (`#region MOVE-OUT TRANSACTION`, commented out)
- ParentHolder / ShipTicket checks during Job Scanning (commented out pending UAT/PROD)
- Holds validation step during Job Batching (prior to Assign) — relies on the preceding Scan call instead of an independent re-check

### ❌ Not Implemented
- Email notification after withdrawal completion (`IEmailService` interface exists but has no implementation wired to FGI withdrawal; only a TODO comment marker is present)
- Server-side ShipBox/ShippingId validation (still a client-only non-empty check; `VerifyShipBox` backend stub is bypassed)
- Live per-holder hold re-verification at withdrawal "scan to verify" step (client-side list match only, hold status is from the once-computed preview)

---

## 7. Key Files Reference

| Area | Frontend | Backend |
|---|---|---|
| Login | `src/pages/LoginPage.tsx`, `src/context/AuthContext.tsx`, `src/api/AuthApi.ts` | `Controllers/AuthController.cs`, `Aggregate/AuthProjectionAggregate.cs`, `Services/ActiveDirectoryService.cs` |
| Scanning/Batching | `src/components/StackerOperationControls.tsx`, `src/components/home/RackBoard.tsx`, `RackPanel.tsx`, `ShipBoxGridModal.tsx`, `BoxAssignmentsModal.tsx`, `src/api/stackerApi.ts` | `Controllers/Stacker/StackerController.cs`, `Aggregate/StackerAggregate.cs` (`ScanHolderJobAsync`, `AssignHolderAsync`, `MapGridViewBoxData`), `Services/StackerSqlService.cs`, `Services/FeatsService.cs` |
| Withdrawal | `src/components/withdrawal/*.tsx`, `src/api/withdrawalApi.ts` | `StackerController.cs` (`withdrawal/*` routes), `StackerAggregate.cs` (`GetFgiWithdrawalRequestsAsync`, `AcknowledgeFgiWithdrawalRequestAsync`, `GetFgiWithdrawalDisassociationPreviewAsync`, `DisassociateFgiWithdrawalRequestAsync`, `GetFgiWithdrawalLayoutAsync`), `StackerSqlService.cs` |
| Config | `src/pages/ConfigPage.tsx`, `src/components/config/*.tsx` | `Services/CapacityConfigService.cs` |
