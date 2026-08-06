# Job Withdrawal Flow Implementation

## Overview
This document tracks the implementation of the Job Withdrawal flow for the WDC STACKER system.

## Phases Implemented

### Phase 1: Verify ShipBox
- **Status**: Implemented
- **Location**: `StackerAggregate.VerifyShipBoxAsync()`, exposed via `StackerAggregate.VerifyFgiWithdrawalShipBoxAsync()`
- **Description**: Validates the entered Shipping ID against FEATS: (1) `Query(Holder)` must return a record with `HolderType == "SHPBOX"` (confirms the ShipBox exists), then (2) `Query(HolderJob)` must return a record with a non-empty `ChildJobCount` (confirms the ShipBox is empty/available). Also exposed as `GET /api/stacker/withdrawal/verify-shipbox`, called from `WithdrawalDisassociationModal.handleShippingIdVerify()` instead of the old client-only non-empty check.
- **Files**:
  - `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
  - `WDC_STACKER.API/Controllers/Stacker/StackerController.cs`
  - `WDC_STACKER.CLIENT.FGI/src/api/withdrawalApi.ts`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalDisassociationModal.tsx`

### Phase 2: Check Hold (FEATS + AHS) with FIFO Backfill
- **Status**: Implemented with modular disable options
- **Location**: `StackerAggregate.CheckHolderHoldsAsync()`
- **Description**: 
  - Checks FEATS InSite hold for each holder in FIFO order
  - Checks AHS hold for configured operations (currently disabled for testing)
  - Hold-excluded holders are replaced with next FIFO holders (backfill)
  - Added `WasReviewedForHold` flag to distinguish qty-excluded vs hold-skipped
- **Files**: 
  - `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
  - `WDC_STACKER.API/Services/FeatsService.cs`
  - `WDC_STACKER.API/Services/AhsService.cs`
- **Testing Modularity** (markers, not fixed line numbers):
  - FEATS IN-SITE HOLD CHECK: `//-- FEATS IN-SITE HOLD CHECK: START/END` block in `CheckHolderHoldsAsync()` - currently **enabled**
  - AHS HOLD CHECK: `#region AHS HOLD CHECK` in `CheckHolderHoldsAsync()` - currently **disabled** (fully commented out)

### Phase 3: ShippingId Contract
- **Status**: Implemented
- **Description**: Added ShippingId parameter to disassociation request and passed through the flow
- **Files**:
  - `WDC_STACKER.API/Models/Stacker/FGI/FgiWithdrawalRequestView.cs` - `ShippingId` added on `FgiWithdrawalDisassociationRequest` (same file), not on `FgiWithdrawalRequestView`
  - `WDC_STACKER.API/Controllers/Stacker/StackerController.cs` - Validates and forwards ShippingId
  - `WDC_STACKER.CLIENT.FGI/src/api/withdrawalApi.ts` - Added shippingId param
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalDisassociationModal.tsx` - Passes shippingId
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/JobWithdrawalPanel.tsx` - Threads shippingId

### Phase 4: AddJob() FEATS Transaction
- **Status**: Implemented (currently disabled for testing)
- **Location**: `StackerAggregate.AddJobForWithdrawalAsync()`
- **Description**: Groups verified withdrawal holders under the Shipping ID using FEATS AddJob transaction
- **Files**:
  - `WDC_STACKER.API/Aggregate/StackerAggregate.cs` - Added AddJobForWithdrawalAsync method
  - `WDC_STACKER.API/Services/FeatsService.cs` - Added AddJobAsync wrapper
- **Testing Modularity**: `#region ADD JOB TRANSACTION` in `DisassociateFgiWithdrawalRequestAsync()` - currently disabled (commented out)

### Phase 5: MoveOut() FEATS Transaction
- **Status**: Implemented (currently disabled for testing)
- **Location**: `StackerAggregate.DisassociateFgiWithdrawalRequestAsync()`
- **Description**: Moves out the Shipping ID (as holder) using FEATS MoveOut transaction
- **Files**: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
- **Testing Modularity**: `#region MOVE-OUT TRANSACTION` in `DisassociateFgiWithdrawalRequestAsync()` - currently disabled (commented out)

### Phase 6: SQL Update
- **Status**: Implemented
- **Location**: `StackerSqlService.DisassociateFgiWithdrawalAsync()`
- **Description**: 
  - Deletes HOLDER_ASSIGN rows for client-confirmed included holders (join directly on `@ExpectedHolders`, the client-provided list)
  - Captures deleted Qty per holder into `@DeletedAssignments` (added `QTY` column)
  - Sums deleted Qty into `@DeletedQtySum`
  - Updates `KITTING_REQUEST`: `ACTUALOUTPUT = ISNULL(ACTUALOUTPUT, 0) + @DeletedQtySum` and `STATUS = 'CLOSED'`
  - Integrity check ensures deleted row count matches `@ExpectedHolders` count (all confirmed holders still existed and qualified under PROCESS = 'FGI')
  - Cleans up empty ShipBoxes/Boxes after holder deletion
- **File**: `WDC_STACKER.API/Services/StackerSqlService.cs`

### Bug Fix: Disassociate Button Not Working After Hold-Aware Selection
- **Root Cause**: `DisassociateFgiWithdrawalAsync()` still contained a leftover naive FIFO recompute (`@ComputedIncluded` CTE, qty-only, no hold awareness) plus a stale-check (`THROW 51012`) comparing it against the client's hold-aware `@ExpectedHolders` list. Since the client skips on-hold holders and backfills from later FIFO candidates, its selection diverges from the server's naive qty-only recompute whenever any holder was hold-skipped — causing every disassociation to fail with "The Included in Total Qty list changed."
- **Fix**: Removed the entire `@ComputedIncluded` CTE and the `THROW 51012` stale-check. The DELETE statement's join and the post-delete integrity check (`THROW 51013`) now both use `@ExpectedHolders` (the client-confirmed list) directly, per the original "trust client-confirmed list" design decision.
- **File**: `WDC_STACKER.API/Services/StackerSqlService.cs`

### Closed Request UI State
- **Status**: Implemented (behavior differs from the original plan - see below)
- **Actual behavior**:
  - `SelectedWithdrawalRequestPanel.tsx`: when `Status === "CLOSED"`, the ACKNOWLEDGE button is disabled, and the action button for an acknowledged request is relabeled **"VIEW"** (not "CLOSED") and remains clickable so the preview can be re-opened read-only.
  - `WithdrawalDisassociationModal.tsx`: `isRequestClosed` relabels the footer button to **"CLOSE"** and routes its `onClick` to `onClose` instead of opening the confirmation dialog.
  - Note: the footer button's `disabled` state still uses `isDisassociateDisabled` (ShippingId verified + all holders verified), so on a CLOSED request the "CLOSE" button is disabled until those conditions are met. Closing is still possible via the header X / backdrop.
- **Files**:
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/SelectedWithdrawalRequestPanel.tsx`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalDisassociationModal.tsx`

### Phase 7: Email Notification
- **Status**: Implemented
- **Location**: `StackerSqlService.DisassociateFgiWithdrawalAsync()` (fired via `Task.Run` fire-and-forget after the transaction commits, not from `StackerAggregate`)
- **Description**: After the SQL update computes `@NewStatus`, the service re-fetches the updated request and calls `IEmailService.SendWithdrawalPartialEmailAsync` / `SendWithdrawalCompletedEmailAsync` / `SendWithdrawalClosedEmailAsync` based on the new status (PARTIAL/COMPLETED/CLOSED). Errors during email send are caught and logged only; they do not fail the disassociation.
- **Files**:
  - `WDC_STACKER.API/Services/StackerSqlService.cs`
  - `WDC_STACKER.API/Services/EmailService.cs`
  - `WDC_STACKER.API/Interfaces/IEmailService.cs`
  - `WDC_STACKER.API/Aggregate/StackerAggregate.cs` (no longer has a placeholder; comment now cross-references the SQL-layer email call)

## Additional Enhancements

### Preview Contract: PartNum + Grade Scoping
- **Status**: Implemented (not previously documented)
- **Description**: The disassociation preview is now scoped by SliderPartNumber and Grade in addition to LEC/PENNUM/TOTAL, so FIFO candidates come only from matching material.
- **Files**:
  - `WDC_STACKER.API/Controllers/Stacker/StackerController.cs` - `GET withdrawal/disassociation-preview` accepts `partNum` and `grade` query params
  - `WDC_STACKER.API/Aggregate/StackerAggregate.cs` - `GetFgiWithdrawalDisassociationPreviewAsync(lec, penNum, total, partNum, grade, token, clientKey)`
  - `WDC_STACKER.API/Services/StackerSqlService.cs` - preview + disassociate SQL resolve `@RequestPartNum` / `@RequestGrade`
  - `WDC_STACKER.CLIENT.FGI/src/api/withdrawalApi.ts` - `getFgiWithdrawalDisassociationPreviewApi` passes partNumber/grade
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/JobWithdrawalPanel.tsx` - threads `SliderPartNumber` and `Grade`

### Withdrawal Rack / ShipBox / Holder Drill-Down
- **Status**: Implemented (not previously documented)
- **Description**: The withdrawal page renders the LEC rack layout and supports drilling down rack -> box -> ship box -> holders, reusing the home page grid styles (`components/home/rackGridStyles`).
- **Files**:
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalRackPanel.tsx`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalShipBoxModal.tsx`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalHoldersModal.tsx`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalRequestTable.tsx`

### Request Model: ActualOutput + Status
- **Status**: Implemented (not previously documented)
- **Description**: `ACTUALOUTPUT` and `STATUS` are selected from `KITTING_REQUEST` and surfaced to the client so the panel can display them and gate the closed-request UI.
- **Files**:
  - `WDC_STACKER.API/Models/Stacker/FGI/FgiWithdrawalRequestView.cs` - `ActualOutput`, `Status`
  - `WDC_STACKER.API/Services/StackerSqlService.cs` - request query/mapping
  - `WDC_STACKER.CLIENT.FGI/src/types/withdrawal.ts`

### Holder Verification Scan Gate
- **Status**: Implemented (not previously documented)
- **Description**: Inside the disassociation modal, the operator must (1) verify the ShippingId and (2) scan every included holder before DISASSOCIATE enables. Each scan flips exactly one matching unverified row to `VERIFIED`; unknown holders raise a not-found state. A circular progress indicator tracks verified/included count.
- **File**: `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalDisassociationModal.tsx`

### Frontend Loading Animation
- **Status**: **NOT implemented** (documented previously but not present in code)
- **Current state**: There is no `.withdrawal-disassociation-loading-overlay` rule in `WDC_STACKER.CLIENT.FGI/src/index.css` and no full-screen overlay JSX in `WithdrawalDisassociationModal.tsx`. The only feedback during submission is the inline spinner plus "DISASSOCIATING..." label on the confirmation dialog's confirm button, and the "CHECKING..." label on the DISASSOCIATE button while the preview loads.
- **If needed**: add the overlay element in the modal and the matching CSS class.

### Excluded Table Filtering
- **Description**: Changed excluded table to only show holders that were reviewed for holds but skipped (not qty-excluded)
- **Changes**:
  - Added `WasReviewedForHold` flag to `FgiWithdrawalSourceRecordView`
  - Changed skipped records filter to `!record.IsIncluded && record.WasReviewedForHold`
  - Changed table title from "SKIPPED BY LIMIT" to "SKIPPED BY HOLD"
  - Updated note column to show hold status (IN-SITE HOLD / AHS HOLD) instead of qty cap messages
- **Files**:
  - `WDC_STACKER.API/Models/Stacker/FGI/FgiWithdrawalRequestView.cs`
  - `WDC_STACKER.CLIENT.FGI/src/types/withdrawal.ts`
  - `WDC_STACKER.CLIENT.FGI/src/components/withdrawal/WithdrawalDisassociationModal.tsx`

### Caching
- **Description**: Added two-level caching to avoid repeated hold checks and preview calculations
- **Holder-Level Cache**:
  - Caches individual holder hold check results
  - Key: Holder (uppercase)
  - Cache: `_holdCheckCache` (ConcurrentDictionary)
  - No expiration and never cleared: a holder released from hold keeps its cached `IN-SITE HOLD` result until the API restarts
- **Preview-Level Cache**:
  - Caches entire preview by (LEC, PENNUM, TOTAL, PARTNUM, GRADE)
  - Key: `{LEC}|{PENNUM}|{TOTAL}|{PARTNUM}|{GRADE}` (uppercase, `null` literal for missing parts)
  - Expiration: 10 minutes (`_cacheExpiration`)
  - Cache: `_previewCache` (static ConcurrentDictionary)
  - Invalidated via `_previewCache.Clear()` at the **start** of `DisassociateFgiWithdrawalRequestAsync()` (before the SQL commit), not only on success
- **File**: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`

### Build Fixes
- **AHS Service Reference**: Fixed constructor errors with `#if !NETCOREAPP` conditional compilation
- **File**: `WDC_STACKER.API/AhsServiceReference.cs`

## Current Flow (Testing Configuration)
1. Verify ShipBox (real FEATS validation: Holder existence + HolderType=SHPBOX + ChildJobCount)
2. ~~AddJob~~ (disabled for testing)
3. ~~MoveOut~~ (disabled for testing)
4. SQL update (delete holders + set ACTUALOUTPUT)
5. Email notification (Partial/Completed/Closed) sent from `StackerSqlService`

## To Re-enable Disabled Features
All toggles live in `WDC_STACKER.API/Aggregate/StackerAggregate.cs`; locate them by marker instead of line number.
- **FEATS IN-SITE HOLD CHECK**: `//-- FEATS IN-SITE HOLD CHECK: START/END` (already enabled)
- **AHS HOLD CHECK**: **Removed.** The `#region AHS HOLD CHECK` block called `AhsService.CheckHoldAsync`, which wrapped the outdated/unsupported AHS `CheckHold` SOAP operation and would not have compiled as written (`checkExists = ';True'` third argument vs. the method's 2-argument signature). Both the dead commented-out block in `StackerAggregate.CheckHolderHoldsAsync()` and the `CheckHoldAsync` method in `AhsService.cs` have been deleted. Use `AhsService.SliderCheck2Async` instead (already used in the Job Scanning hold check, `ScanHolderJobAsync`) if AHS-based hold checking is needed during Withdrawal.
- **ADD JOB TRANSACTION**: `#region ADD JOB TRANSACTION` - uncomment
- **MOVE-OUT TRANSACTION**: `#region MOVE-OUT TRANSACTION` - uncomment

## Running the Application

### Backend
```powershell
cd WDC_STACKER.API
dotnet run
```

### Frontend
```powershell
cd WDC_STACKER.CLIENT.FGI
npm run dev
```

## Remaining Work (Job Withdrawal)

1. **AddJob transaction — disabled for testing.**
   `#region ADD JOB TRANSACTION` in `StackerAggregate.DisassociateFgiWithdrawalRequestAsync()` is fully commented out.
   **Action:** Uncomment when ready to group withdrawal holders under the Shipping Id via FEATS.

2. **MoveOut transaction — disabled for testing.**
   `#region MOVE-OUT TRANSACTION` in the same method is fully commented out.
   **Action:** Uncomment when ready to move the ShipBox out via FEATS.

3. **No compensating rollback for FEATS AddJob/MoveOut partial failure.**
   Once AddJob/MoveOut are re-enabled (items 1-2), a SQL failure after a successful FEATS transaction (or a FEATS failure after a partially-applied step) leaves FEATS and the STACKER DB out of sync. There is no saga/compensation logic to undo a successful FEATS call if a later step fails.
   **Action:** Design compensating actions (e.g. re-MoveIn / ReleaseHolder) or a retry/reconciliation job before re-enabling items 1-2 in production.

4. **`_holdCheckCache` never expires.**
   No TTL, and it is not cleared on disassociation, so hold status can go stale for the lifetime of the process.
   **Action:** Add a TTL (mirroring `_previewCache`'s 10-minute expiry) or invalidate on disassociation.

5. **`_previewCache.Clear()` runs too early.**
   It is called at the very start of `DisassociateFgiWithdrawalRequestAsync()`, before VerifyShipBox/AddJob/MoveOut/SQL run — so a failed attempt still wipes a valid cached preview, forcing an unnecessary re-fetch.
   **Action:** Move the `.Clear()` call to after the SQL update succeeds.

6. **CLOSED modal footer button bug.**
   On a `CLOSED` request, `WithdrawalDisassociationModal.tsx` relabels the footer button to "CLOSE" (routing `onClick` to `onClose`), but its `disabled` state still uses `isDisassociateDisabled` (ShippingId + all holders verified). This means a read-only CLOSED view cannot be dismissed from the footer button until those conditions are met — only the header X / backdrop click works.
   **Action:** Give the CLOSED-state button its own `disabled` condition (e.g. always enabled) instead of reusing `isDisassociateDisabled`.

7. **Decide on AHS hold check for Withdrawal.**
   The old AHS hold-check block in `CheckHolderHoldsAsync` was dead/uncompilable code and has been removed entirely (not just disabled). Withdrawal currently only performs the FEATS in-site hold check. Job Scanning still performs a live AHS `SliderCheck2Async` check.
   **Action:** If Withdrawal needs AHS-based hold checking, implement it using `AhsService.SliderCheck2Async` (see `JOB_SCANNING_VALIDATIONS_CHANGES.md` for the existing Scan-time pattern).

---

## Documentation Review (Current vs. Documented) — Verified Against Code

Re-verified against the current `WDC_STACKER.API/Aggregate/StackerAggregate.cs` and `StackerController.cs`.

| Documented Item | Current Code State | Verdict |
|---|---|---|
| Phase 1 – VerifyShipBox | **Updated.** No longer stubbed: `VerifyShipBoxAsync()` now queries FEATS `Holder` (existence + HolderType=SHPBOX) and `HolderJob` (ChildJobCount) before allowing disassociation to proceed. Also reachable from the client via `GET /api/stacker/withdrawal/verify-shipbox`. | 🟢 Updated — real validation implemented |
| Phase 2 – FEATS in-site hold check | `CheckHolderHoldsAsync` (~line 1868) FEATS check block bounded by `//-- FEATS IN-SITE HOLD CHECK: START/END` — confirmed **enabled**. | ✅ Accurate |
| Phase 2 – AHS hold check | **Removed.** The commented-out `#region AHS HOLD CHECK` block and the dead `AhsService.CheckHoldAsync` method it referenced (outdated/unsupported AHS `CheckHold` SOAP op, no live callers) have both been deleted. Withdrawal now only performs the FEATS in-site hold check; Job Scanning remains the only flow with an active AHS check, via `SliderCheck2Async`. | ✅ Updated — dead code removed |
| Phase 4 – AddJob (disabled) | `#region ADD JOB TRANSACTION (comment out to disable)` (~lines 1999-2013) — confirmed fully commented out including the failure-return branch. | ✅ Accurate |
| Phase 5 – MoveOut (disabled) | `#region MOVE-OUT TRANSACTION (comment out to disable)` (~lines 2015-2041) — confirmed fully commented out. | ✅ Accurate |
| Phase 6 – SQL update | `StackerSqlService.DisassociateFgiWithdrawalAsync()` still performs the delete + `ACTUALOUTPUT`/`STATUS` update; called unconditionally after the bypassed/disabled phases (~lines 2043-2051). | ✅ Accurate |
| Phase 7 – Email notification | **Corrected.** Email is wired and working, but not where this doc originally placed it: `StackerSqlService.DisassociateFgiWithdrawalAsync()` fires `IEmailService.SendWithdrawalPartialEmailAsync/CompletedEmailAsync/ClosedEmailAsync` (fire-and-forget `Task.Run`) based on `@NewStatus` computed in the same SQL statement. The old placeholder comment block in `StackerAggregate.cs` has been removed and replaced with a cross-reference comment. | 🟢 Updated — feature confirmed implemented, doc location corrected |
| Preview cache / holder-cache behavior | `_previewCache` (static `ConcurrentDictionary`, 10-min expiry) and `_holdCheckCache` (no expiry) both confirmed present at class-field level (~lines 19-21) and used inside `CheckHolderHoldsAsync`/`GetFgiWithdrawalDisassociationPreviewAsync`. | ✅ Accurate |

**Outdated items found:** No functional drift — all "disabled/commented" phases documented here remain disabled/commented in the current codebase. The only correction is the newly-noted **asymmetry between Job Scanning's AHS check (enabled) and Job Withdrawal's AHS check (disabled)** — previously each doc only described its own module in isolation.

---

## Process Flow — Job Withdrawal

```
┌──────────────────────────────────────────────────────────────────────┐
│ OPERATOR: Opens Job Withdrawal tab → WithdrawalRequestTable loads    │
│ GET /api/stacker/withdrawal/requests → GetFgiWithdrawalRequestsAsync │
└───────────────────────────────┬──────────────────────────────────────┘
                                 ▼
        Operator selects a request → SelectedWithdrawalRequestPanel
                                 │
                 ┌───────────────┴────────────────┐
                 ▼                                 ▼
     PATCH .../acknowledge                  (already CLOSED?)
     AcknowledgeFgiWithdrawalRequestAsync    → button relabeled "VIEW"/"CLOSE",
     → SQL: SET AcknowledgeBy = user           read-only preview only
                 │
                 ▼
     Operator clicks "Disassociate" → JobWithdrawalPanel
                 │ GET .../withdrawal/disassociation-preview
                 │   (lec, penNum, total, partNum, grade)
                 ▼
     GetFgiWithdrawalDisassociationPreviewAsync
       → checks _previewCache (10-min TTL) first
       → SQL: FIFO candidate list scoped by LEC/PENNUM/TOTAL/PartNum/Grade
       → for each candidate (in FIFO order, up to qty needed):
             CheckHolderHoldsAsync(holder)
               ├─ cache hit? → return cached (false, "") or (true,msg,true,"IN-SITE HOLD")
               └─ FEATS InSite hold check — ENABLED → hold ⇒ excluded, backfill next FIFO
                    (AHS hold check removed — see "To Re-enable Disabled Features")
       → tags each record Included / Skipped-by-Hold (WasReviewedForHold) / Skipped-by-Qty
                 ▼
     WithdrawalDisassociationModal renders:
       - Included table (FIFO holders that will fulfill the request)
       - Skipped-by-Hold table (WasReviewedForHold && !IsIncluded)
                 ▼
     Operator enters Shipping ID → handleShippingIdVerify()
       (client-only non-empty check; no server call yet)
                 ▼
     Operator scans/types each included Holder → handleHolderVerify()
       (matches against already-loaded sourceRecords client-side;
        flips row to VERIFIED; does NOT re-call any hold-check API)
                 ▼
     All holders VERIFIED + ShippingId verified → "DISASSOCIATE" enabled
                 ▼
     Confirm dialog → POST .../withdrawal/requests/{id}/disassociate
       { shippingId, includedHolders }
                 ▼
     DisassociateFgiWithdrawalRequestAsync()
       1. _previewCache.Clear()  (runs BEFORE any of the steps below)
       2. VerifyShipBoxAsync(shippingId)  — FEATS Query(Holder) HolderType=SHPBOX + Query(HolderJob) ChildJobCount
       3. [DISABLED] AddJobForWithdrawalAsync  — #region ADD JOB TRANSACTION (commented out)
       4. [DISABLED] FEATS MoveOut(shippingId) — #region MOVE-OUT TRANSACTION (commented out)
       5. StackerSqlService.DisassociateFgiWithdrawalAsync()
            → DELETE HOLDER_ASSIGN rows matching @ExpectedHolders (client-confirmed list)
            → SUM deleted Qty → UPDATE KITTING_REQUEST SET ACTUALOUTPUT += sum, STATUS='CLOSED'
            → integrity check: deleted row count == @ExpectedHolders count
            → clean up now-empty ShipBoxes/Boxes
       6. Email notification fired (fire-and-forget) from StackerSqlService based on @NewStatus (PARTIAL/COMPLETED/CLOSED)
                 ▼
     Response → WithdrawalRequestTable refreshes; request now shows STATUS=CLOSED
```

### Rack / Layout Drill-Down (parallel, read-only path)
```
WithdrawalRackPanel ← GET .../withdrawal/layout (lec, penNum, partNum, grade)
   → GetFgiWithdrawalLayoutAsync → rack → WithdrawalShipBoxModal (box/ship-box)
       → WithdrawalHoldersModal (holders inside a ship box)
```
