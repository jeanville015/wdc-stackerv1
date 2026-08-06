# WDC Stacker — Scan & Placement Flowchart

This flowchart documents the **actual** code-verified behavior of the Scan and Placement (Assign) process, based on a line-by-line review of:
- `WDC_STACKER.CLIENT/src/components/StackerOperationControls.tsx` (UI)
- `WDC_STACKER.API/Aggregate/StackerAggregate.cs` (`ScanHolderJobAsync`, `AssignHolderAsync`, `MapGridViewBoxData`)

---

## A. Text Description (Step-by-Step)

### Part 1 — Scan (Validation)
1. **Start**: Operator types/scans an item ID into the input field.
2. If the field is empty → nothing happens (silent, no request sent).
3. If no valid login session exists → show "Login token is missing. Please sign in again."; stop.
4. Send scan request to the server.
5. **Server check 1 — Session valid?** If not → return "Invalid or expired token." → Placement stays disabled.
6. **Server check 2 — Item record found?** Look up the item's job record.
   - If the lookup service itself fails → return that service's error message.
   - If no record is found → return "HolderJob record was not found."
7. **Server check 3 — Operation matches the configured valid operation?** If not → return "Operation is not valid."
8. **Server check 4 — ParentHolder condition.** *(See note below — current code rejects when ParentHolder HAS a value, but the message says "ParentHolder has no value!")* → return "ParentHolder has no value!"
9. **Server check 5 — ShipTicket must be blank.** If a ShipTicket value already exists → return "ShipTicket has value!"
10. **Server check 6 — Find a suggested storage box:**
    - If a box with used space between 0% and 100% exists → suggest the box with the smallest used-space percentage (ties broken by lowest rack/layer/column).
    - Else if no partially-used box exists, pick an empty (0%) box.
    - Else if **all** boxes are full (100%): try to automatically expand to a new column, then row, then rack (if the configured limits allow it) and suggest that new box.
    - Else, if expansion limits are also exhausted → return "All Settings are maxed out!"
    - Else (no boxes exist / cannot resolve) → return "No suggested target box was found."
11. If a box was successfully suggested → return "Validation Pass!" with the suggested box marked; **Placement button becomes enabled**.
12. If any check (5–10) failed → Placement button stays **disabled**; error message shown to the operator.

> **UI note:** Steps 5–9 return a response with no box list at all. The screen still updates its box list to *empty* in this case, which can momentarily clear the previously displayed rack grid.

### Part 2 — Placement (Assign)
13. Operator clicks the **Assign** button (only clickable if step 11 succeeded on the current, unedited scan value).
14. **Client-side guards** (kept as a safety net, but not reachable through normal use since the button is disabled otherwise):
    - Empty item ID → "Holder is required."
    - Missing session → "Login token is missing..."
    - No suggested box in memory → "No suggested target box was found."
15. Send placement request to the server (item ID + the suggested box's location + a fixed process type).
16. **Server check 1 — Session valid?** If not → "Invalid or expired token."
17. **Server check 2 — Item record found again?** (Re-checked independently of the scan step.) If lookup fails or no record → matching error message.
18. **Server check 3 — Box label (`BinName`) must be exactly 5 characters.** If not → "BinName length is not eligible." *(Not checked during scan — new check.)*
19. **Server check 4 — Build code and product name must both be present.** If either is missing → "BuildCode or ProductName is missing." *(New check.)*
20. **Server check 5 — Build code + box label combination must match a configured code.** If it doesn't match → "BuildCode and BinName combination is not eligible." *(New check.)*
21. **Server check 6 — Is the item already placed in a box?** (Re-checked against the database at this exact moment — protects against a second operator placing the same item in between.) If already placed → "Holder is already assigned."
22. **Server check 7 — Process type valid (PWD or FGI)?** If not → "Invalid process." *(In practice this value is fixed by client configuration, so this is rarely user-triggerable.)*
23. **Save to database.** If the save fails unexpectedly → "Unable to Assign."
24. **Success** → "Holder assigned successfully." (existing box) or "Box created and holder assigned successfully." (new box); rack view updates; **End**.

---

## B. Mermaid Diagram

```mermaid
flowchart TD
    Start([Start: Operator scans item]) --> EmptyCheck{Item ID entered?}
    EmptyCheck -->|No| NoAction[No request sent]
    NoAction --> End1([End])

    EmptyCheck -->|Yes| TokenCheckClient{Login session present?}
    TokenCheckClient -->|No| ErrToken1["Login token is missing.<br/>Please sign in again."]
    ErrToken1 --> End1

    TokenCheckClient -->|Yes| ScanRequest[Send Scan request to server]
    ScanRequest --> ServerToken{Session valid on server?}
    ServerToken -->|No| ErrInvalidToken["Invalid or expired token."]
    ErrInvalidToken --> Disabled1[Placement stays disabled]
    Disabled1 --> End1

    ServerToken -->|Yes| LookupItem[Look up item job record via FEATS]
    LookupItem --> LookupOk{Lookup succeeded and record found?}
    LookupOk -->|Lookup failed| ErrLookup["Lookup service error message"]
    LookupOk -->|No record| ErrNotFound["HolderJob record was not found."]
    ErrLookup --> Disabled1
    ErrNotFound --> Disabled1

    LookupOk -->|Yes| OpCheck{Operation matches configured valid operation?}
    OpCheck -->|No| ErrOp["Operation is not valid"]
    ErrOp --> Disabled1

    OpCheck -->|Yes| ParentCheck{"ParentHolder has a value? (see bug note)"}
    ParentCheck -->|Yes, has value| ErrParent["'ParentHolder has no value!'<br/>(message contradicts condition — flagged bug)"]
    ErrParent --> Disabled1

    ParentCheck -->|No, blank| ShipCheck{ShipTicket already has a value?}
    ShipCheck -->|Yes| ErrShip["ShipTicket has value!"]
    ErrShip --> Disabled1

    ShipCheck -->|No, blank| BoxMap[Determine suggested storage box]
    BoxMap --> PartialBox{Partially-used box available 0%-100%?}
    PartialBox -->|Yes| SuggestPartial[Suggest smallest % used box]
    PartialBox -->|No| EmptyBox{Empty 0% box available?}
    EmptyBox -->|Yes| SuggestEmpty[Suggest empty box]
    EmptyBox -->|No| AllFull{All boxes at 100%?}
    AllFull -->|Yes| CanExpand{Can expand column/row/rack within limits?}
    CanExpand -->|Yes| SuggestNew[Auto-create and suggest new box]
    CanExpand -->|No| ErrMaxed["All Settings are maxed out!"]
    AllFull -->|No| ErrNoTarget["No suggested target box was found."]
    ErrMaxed --> Disabled1
    ErrNoTarget --> Disabled1

    SuggestPartial --> ScanSuccess["Validation Pass!<br/>Placement button enabled"]
    SuggestEmpty --> ScanSuccess
    SuggestNew --> ScanSuccess

    ScanSuccess --> ClickAssign([Operator clicks Assign])
    ClickAssign --> ClientGuards{"Client guards:<br/>item ID / session / suggested box present?<br/>(normally always true here)"}
    ClientGuards -->|Fails - edge case only| ErrClientGuard["Holder is required. /<br/>Login token missing /<br/>No suggested target box"]
    ErrClientGuard --> End1

    ClientGuards -->|Passes| AssignRequest[Send Placement request to server]
    AssignRequest --> AssignToken{Session valid?}
    AssignToken -->|No| ErrAssignToken["Invalid or expired token."]
    ErrAssignToken --> AssignFailed[Placement rejected, rack unchanged]

    AssignToken -->|Yes| LookupItem2[Re-look up item record independently]
    LookupItem2 --> LookupOk2{Lookup succeeded and record found?}
    LookupOk2 -->|No| ErrLookup2["Lookup error / HolderJob not found"]
    ErrLookup2 --> AssignFailed

    LookupOk2 -->|Yes| BinLenCheck{BinName exactly 5 characters?}
    BinLenCheck -->|No| ErrBinLen["BinName length is not eligible."]
    ErrBinLen --> AssignFailed

    BinLenCheck -->|Yes| FieldsCheck{BuildCode and ProductName both present?}
    FieldsCheck -->|No| ErrFields["BuildCode or ProductName is missing."]
    ErrFields --> AssignFailed

    FieldsCheck -->|Yes| ComboCheck{BuildCode + BinName combination valid?}
    ComboCheck -->|No| ErrCombo["BuildCode and BinName combination is not eligible."]
    ErrCombo --> AssignFailed

    ComboCheck -->|Yes| AlreadyAssignedCheck{Item already placed in a box?}
    AlreadyAssignedCheck -->|Yes| ErrAlready["Holder is already assigned."]
    ErrAlready --> AssignFailed

    AlreadyAssignedCheck -->|No| ProcessCheck{Process type is PWD or FGI?}
    ProcessCheck -->|No| ErrProcess["Invalid process."]
    ErrProcess --> AssignFailed

    ProcessCheck -->|Yes| SaveDb[Save assignment to database]
    SaveDb --> SaveOk{Save succeeded?}
    SaveOk -->|No| ErrSave["Unable to Assign."]
    ErrSave --> AssignFailed

    SaveOk -->|Yes| AssignSuccess["Holder assigned successfully. /<br/>Box created and holder assigned successfully.<br/>Rack view updates"]
    AssignSuccess --> End2([End])
    AssignFailed --> End2
```

---

## C. Notes / Discrepancies Found During Review

1. **Bug — ParentHolder message mismatch:** In `StackerAggregate.ScanHolderJobAsync`, the active code is:
   ```csharp
   // 2. ParentHolder must have value
   //if (string.IsNullOrWhiteSpace(parentHolder))
   if (!string.IsNullOrWhiteSpace(parentHolder))
   {
       Message = "ParentHolder has no value!"
   }
   ```
   The condition fires when `ParentHolder` **does** have a value, but the message says it has **no** value. Either the condition or the message text appears to be inverted from intent. **Needs confirmation from the dev team** on which is correct before finalizing test expectations.

2. **Scan does not fully validate what Assign will check.** A holder can pass scan ("Validation Pass!") and still fail at the Assign step due to `BinName` length, missing `BuildCode`/`ProductName`, or an invalid `BuildCode`+`BinName` combination — none of which are checked during scan.

3. **Client-side guards in `handleAssign` (empty holder / missing token / no suggested box) are not reachable through normal UI interaction**, because the Assign button is disabled until a scan succeeds, and editing the scan field immediately re-disables it. These guards only matter if the API is called directly or in a rare timing edge case (e.g., token expires between scan and click).

4. **A failed/blocked scan clears the rack grid view.** Any scan failure that returns before the box-mapping step (invalid session, record not found, invalid operation, ParentHolder/ShipTicket checks) does not include a box list in its response, so the UI updates the rack view to an **empty list** in those cases.

5. **Disassociate (removal) does more than a simple delete:** it also checks FEATS for an active "InSite hold" (`HoldReason`/`HoldComment` must both be blank) and performs a FEATS "Move Out" transaction *before* deleting the database assignment record. Both of these can independently cause the removal to fail.
