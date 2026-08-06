# Job Scanning Validations Implementation Changes

## Overview
This document lists all changes made to implement job scanning validations for the WDC Stacker application, specifically focusing on PartNum, BinName, and Hold validations.

---

## 1. MoveOut + Hold for Missing PartNum Validation

### File: `WDC_STACKER.API/Services/FeatsService.cs`
**Change:** Added `HoldHolderAsync` wrapper method
**Purpose:** Wrap the FEATS SOAP service's `HoldHolder` operation to apply holds to holders with specified reason codes and comments
**Details:**
- Mirrors the existing `MoveOutAsync` pattern
- Includes logging, domain-prefixed username handling, and error handling
- Returns `(bool Success, string Message)` tuple
- Lines: 262-283

### File: `WDC_STACKER.API/Services/FeatsService.cs`
**Change:** Added `MoveInAsync` wrapper method
**Purpose:** Wrap the FEATS SOAP service's `MoveIn` operation to move holders into process
**Details:**
- Mirrors the existing `MoveOutAsync` pattern
- Includes logging, domain-prefixed username handling, and error handling
- Returns `(bool Success, string Message)` tuple
- Lines: 227-248

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Modified PartNum validation to MoveOut then apply Hold
**Purpose:** When PartNumber is missing for FGI holders, move them to RBF2 and apply a hold with reason TAP and comment "NO PART NUMBER"
**Details:**
- Calls `_featsService.MoveOutAsync` with resource "735617 RBF2"
- If MoveOut succeeds, calls `_featsService.HoldHolderAsync` with holdReasonCode "TAP" and comment "NO PART NUMBER"
- Lines: 668-732

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Added compensating logic for partial failure
**Purpose:** Handle the case where MoveOut succeeds but Hold fails
**Details:**
- If Hold fails after successful MoveOut, logs a warning
- Returns error message indicating holder is moved out but not held
- Lines: 703-721

---

## 2. MoveOut for Invalid BinName Validation

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Added BinName length validation
**Purpose:** Ensure BinName is exactly 5 characters for FGI holders; if not, MoveOut to RBF2
**Details:**
- Checks if `binName?.Length != 5`
- Calls `_featsService.MoveOutAsync` with resource "735617 RBF2"
- Returns error message with current BinName value
- Lines: 747-781

---

## 3. AHS Service Integration for Hold Validation

### File: `AhsServiceReference.cs` (root level)
**Change:** Created new AHS service reference file
**Purpose:** Manually generated SOAP service reference for AHS (AutoHolding) web service based on WSDL
**Details:**
- Contains `AutoHoldingSoap` interface with `SliderCheck2` and `CheckHold` operations
- Contains `AutoHoldingSoapClient` for making SOAP calls
- Uses BasicHttpBinding with no security
- Namespace: `AhsServiceReference`

### File: `AhsService.wsdl` (root level)
**Change:** Downloaded AHS WSDL file
**Purpose:** Reference documentation for AHS web service
**Details:**
- Downloaded from `http://pbt-mt-ahsapp01:1010/AutoHolding.asmx?wsdl`
- Moved to root level to match FeatsService pattern

### File: `WDC_STACKER.API/Services/AhsService.cs`
**Change:** Created AhsService wrapper and updated SliderCheck2Async return type
**Purpose:** Provide a clean wrapper around AHS SOAP client with logging and error handling
**Details:**
- `SliderCheck2Async`: Checks if holder has slider issues, returns raw response string. **This is the only AHS check used going forward** — it supersedes `CheckHoldAsync` for hold validation.
- ~~`CheckHoldAsync`: Checks if holder is on hold~~ — **Removed.** This method called the AHS `CheckHold` SOAP operation, which is outdated/out of support. It had no live callers (its only reference was inside the commented-out `#region AHS HOLD CHECK` block in `StackerAggregate.CheckHolderHoldsAsync()`, which itself would not have compiled as written). Both the method in `AhsService.cs` and the dead commented-out block in `StackerAggregate.cs` have been deleted. `SliderCheck2Async` already covers hold detection (via `EXISTS`/`ONHOLD` responses) and is the supported replacement.
- Configurable service URL via appsettings
- Returns raw response string instead of boolean for detailed validation
- Lines: 1-45 (after `CheckHoldAsync` removal)

### File: `WDC_STACKER.API/Program.cs`
**Change:** Registered AhsService in dependency injection
**Purpose:** Make AhsService available for injection into other services
**Details:**
- Added `builder.Services.AddScoped<AhsService>();`
- Line: 36

### File: `WDC_STACKER.API/appsettings.json`
**Change:** Added AHS service URL configuration
**Purpose:** Configure AHS service endpoint URL
**Details:**
- Added `"AHS": { "ServiceUrl": "http://pbt-mt-ahsapp01:1010/AutoHolding.asmx" }`
- Lines: 12-14

### File: `WDC_STACKER.API/Models/CapacityConfig.cs`
**Change:** Added HoldValidationOperations list property
**Purpose:** Configure list of operations to validate against AHS SliderCheck2
**Details:**
- Added `public List<string> HoldValidationOperations { get; set; } = new();`
- Line: 19

### File: `WDC_STACKER.API/CapacityConfig.WDC_STACKER.CLIENT.FGI.json`
**Change:** Added HoldValidationOperations configuration
**Purpose:** Configure operations for AHS hold validation
**Details:**
- Added `"HoldValidationOperations": ["735570 SLIDER OUTPUT CHECK", "735575 PWD OPERATION", "735500 SLIDER COUNTER/LABEL", "735613 BACKFLUSH2", "735630 FGI"]`
- Lines: 12-17

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Injected AhsService into StackerAggregate
**Purpose:** Enable StackerAggregate to call AHS service for hold validation
**Details:**
- Added `_ahsService` field
- Added `AhsService` parameter to constructor
- Lines: 12, 17

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Added HoldReason and HoldComment to FEATS query fieldNames
**Purpose:** Retrieve hold information from FEATS for first-step validation
**Details:**
- Added "HoldReason" and "HoldComment" to fieldNames list
- Lines: 554-555

### File: `WDC_STACKER.API/Aggregate/StackerAggregate.cs`
**Change:** Implemented two-step Hold validation
**Purpose:** Validate holder is free of holds using both FEATS and AHS checks
**Details:**
- **Step 1 (FEATS)**: Check if HoldReason and HoldComment are null. If not null, holder has FEATS hold.
- **Step 2 (AHS)**: Loop through all operations in config.HoldValidationOperations and call SliderCheck2Async for each.
  - If any operation returns "EXISTS" or "ONHOLD", fail validation and MoveOut to RBF2.
  - If all operations return "PASSED", validation passes.
  - If no operations configured, uses current operation as fallback.
- Lines: 617-618, 783-877

---

## 4. Code Structure Consistency

### File: `AhsServiceReference.cs` (moved)
**Change:** Moved from `WDC_STACKER.API/` to root level
**Purpose:** Match FeatsService reference file location pattern
**Details:**
- Now located at root level next to `FeatsServiceReference.cs`

### File: `AhsService.wsdl` (moved)
**Change:** Moved from `WDC_STACKER.API/` to root level
**Purpose:** Match TxnService.wsdl location pattern
**Details:**
- Now located at root level next to `TxnService.wsdl`

---

## Summary of Validation Flow

When scanning a holder for FGI:

1. **Operation Check**: Must match `config.ValidOperation`
2. **PartNum/ProductName Check**:
   - If PartNum missing → MoveOut to RBF2 + Hold with reason TAP
   - If ProductName missing → Error (no MoveOut)
3. **BinName Check**: If not exactly 5 characters → MoveOut to RBF2
4. **Two-step Hold Validation**:
   - Step 1 (FEATS): Check if HoldReason and HoldComment are null
   - Step 2 (AHS): Loop through all HoldValidationOperations and check SliderCheck2
5. **Holder QTY Check**: Must be valid
6. **InProcess Validation (Last)**: If InProcess != "True", perform MoveIn then continue

All MoveOut operations use resource "735617 RBF2" as specified by the user.

---

## Remaining Work (Job Scanning / Batching)

1. **ParentHolder validation — disabled.**
   Requirement: Holder must have no parent (ParentHolder should be empty). Check exists but is fully commented out in `StackerAggregate.cs` (`ScanHolderJobAsync`, ~lines 701-714) behind "disable for now while on development/QA, enable once on UAT/PROD". `parentHolder` is still fetched from FEATS (~line 591/650) so the value is available — only the `if (!string.IsNullOrWhiteSpace(parentHolder))` failure branch needs uncommenting.
   **Action:** Uncomment when ready for UAT/PROD.

2. **ShipTicket validation — disabled.**
   Requirement: ShipTicket must be empty. Same pattern as ParentHolder: field already queried/fetched, only the failure branch is commented out in `ScanHolderJobAsync` (~lines 716-729).
   **Action:** Uncomment when ready for UAT/PROD.

3. **Scan-vs-Withdrawal AHS hold-check asymmetry.**
   Job Scanning performs a live AHS `SliderCheck2Async` loop over `config.HoldValidationOperations` (~lines 910-1025). Job Withdrawal's equivalent AHS check was dead/uncompilable code and has since been **removed** (see `JOB_WITHDRAWAL_CHANGES.md`), leaving Withdrawal with only the FEATS in-site hold check. If AHS-based hold checking is required for Withdrawal too, it needs to be built using `AhsService.SliderCheck2Async` (the same method Scanning already uses).
   **Action:** Decide whether Withdrawal needs an AHS hold check; if yes, implement using `SliderCheck2Async`.

4. **Assign does not re-verify holds.**
   `AssignHolderAsync` performs no independent hold re-check — it relies entirely on the hold/validation state established during the preceding `Scan` call. If the UI ever allows "Assign" without a fresh "Scan" for the same holder (e.g. stale cached scan result reused), holds are **not** re-verified at Assign time.
   **Action:** Confirm this is acceptable given the UI flow, or add a lightweight re-check in `AssignHolderAsync`.

---

## Documentation Review (Current vs. Documented) — Verified Against Code

This section reconciles this document against the current state of `WDC_STACKER.API/Aggregate/StackerAggregate.cs`.

| Documented Item | Current Code State | Verdict |
|---|---|---|
| MoveOut + Hold on missing PartNum (§1) | Implemented, **enabled**, matches flow (`ScanHolderJobAsync` ~lines 731-831) | ✅ Accurate |
| MoveOut on invalid BinName (§2) | Implemented, **enabled** (~lines 833-882) | ✅ Accurate |
| Two-step Hold Validation — FEATS + AHS (§3) | FEATS in-site hold check (HoldReason/HoldComment) is **enabled** (~lines 885-908). AHS `SliderCheck2` loop is also **enabled** in `ScanHolderJobAsync` (~lines 910-1025). Withdrawal's equivalent AHS check has since been **deleted entirely** (dead/uncompilable code, no live callers) rather than left disabled — see `JOB_WITHDRAWAL_CHANGES.md` and Remaining Work §3 above. Scanning and Withdrawal remain asymmetric on AHS hold checking. | � Updated — AHS asymmetry now tracked as Remaining Work item |
| Holder QTY check | Implemented (~lines 1027-1040), unchanged | ✅ Accurate |
| InProcess + MoveIn | Implemented and enabled | ✅ Accurate (previously mislabeled under "Pending"; now removed from Remaining Work since it is done) |
| ParentHolder / ShipTicket checks | Still commented out, confirmed present in code at current line ranges (see Remaining Work §1-2) | ✅ Accurate |

**Outdated items found:** Line-number references throughout this file (e.g., `Lines: 668-732`, `747-781`, `783-877`) were written against an earlier revision of `StackerAggregate.cs` and have since drifted by tens of lines due to intervening edits. All line numbers above have been corrected/approximated (`~line`) against the current file. Treat exact line numbers as approximate and prefer marker/method-name lookups for future edits.

---

## Process Flow — Job Scanning

```
┌────────────────────────────────────────────────────────────────────────┐
│ OPERATOR: Enter/scan Holder ID (StackerOperationControls.tsx)          │
└───────────────────────────────┬────────────────────────────────────────┘
                                 │ POST /api/stacker/scan
                                 ▼
                    StackerController.Scan()
                                 │
                                 ▼
                StackerAggregate.ScanHolderJobAsync()
                                 │
        ┌────────────────────────┴─────────────────────────┐
        │ 0. Already-assigned check (GetHolderAssignLocationAsync)
        │    → if found: return "already assigned" + highlight box (no FEATS call)
        └────────────────────────┬─────────────────────────┘
                                 ▼
                 FEATS HolderJob query (ExecuteFeatsQueryAsync)
                                 │
                                 ▼
        1. Operation == config.ValidOperation? ──NO──> FAIL "Operation is not valid"
                                 │YES
                                 ▼
        2. ParentHolder empty? [DISABLED / commented out — always skipped]
                                 │
                                 ▼
        3. ShipTicket empty?   [DISABLED / commented out — always skipped]
                                 │
                                 ▼
        4. PartNumber & ProductName present?
              │NO (PartNumber) ──> MoveOut→"735617 RBF2" ──fail──> return error
              │                       │success
              │                       ▼
              │                 HoldHolder(reason=TAP, comment="NO PART NUMBER")
              │                       │fail──> return "moved out but not held" (partial-failure)
              │                       │success
              │                       ▼
              │                 return FAIL "Missing PartNumber" (holder now on hold at RBF2)
              │NO (ProductName only) ──> return FAIL (no MoveOut/Hold side effect)
                                 │YES (both present)
                                 ▼
        5. BinName.Length == 5? ──NO──> MoveOut→"735617 RBF2" ──> return FAIL
                                 │YES
                                 ▼
        6. Hold check — Step 1: FEATS HoldReason/HoldComment both null? ──NO──> return FAIL "Holder has FEATS hold"
                                 │YES
                                 ▼
           Hold check — Step 2: loop config.HoldValidationOperations →
                          AHS SliderCheck2Async(holder, op) for each
                            │ any EXISTS/ONHOLD ──> MoveOut→"735617 RBF2" ──> return FAIL
                            │ AHS call itself fails ──> return FAIL (no MoveOut)
                                 │all PASSED
                                 ▼
        7. Holder QTY valid (TryGetValidatedHolderQty)? ──NO──> return FAIL "Holder QTY is invalid"
                                 │YES
                                 ▼
        8. InProcess == "True"? ──NO──> MoveInAsync ──fail──> return FAIL "MoveIn failed"
                                 │YES (or MoveIn succeeded)
                                 ▼
        9. [FGI only] Resolve PenNum (ResolveFgiPenNumAsync) → suggest Box/ShipBox
           (grouping by PartNum+PenNum+ProductName / LEC)
                                 ▼
              SUCCESS → Message "Validation Pass!" + GridViewBoxes + suggested target
                                 │
                                 ▼
        UI: StackerOperationControls pre-selects suggested Box/ShipBox → enables "Assign"
```

---

## Process Flow — Job Batching (Assign)

```
┌───────────────────────────────────────────────────────────────────┐
│ OPERATOR: Confirms/overrides Box + ShipBox, clicks "Assign"        │
└───────────────────────────────┬─────────────────────────────────────┘
                                 │ POST /api/stacker/assign
                                 ▼
                StackerController.Assign()
                                 ▼
             StackerAggregate.AssignHolderAsync()
                                 │
        (No independent hold re-check here — relies on the hold/validation
         state already established during the preceding Scan call. If the
         UI allows Assign without a fresh Scan for the same holder, holds
         are NOT re-verified at Assign time.)
                                 ▼
        Validate Box/ShipBox exist + compatible (IsCompatibleFgiBox) →
        auto-create next Box/ShipBox if needed (TryCreateNextFgiBox)
                                 ▼
        StackerSqlService.InsertFgiAssignmentAsync()
           → INSERT into HOLDER_ASSIGN / BOX / SHIPBOX tables
                                 ▼
        Return updated GridViewBoxes → RackBoard/RackPanel/ShipBoxGridModal
        re-render with new holder placement
```
