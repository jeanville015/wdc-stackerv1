# WDC Stacker — FGI
## Test Plan

**Author:** QA Team
**Creation Date:** July 15, 2026
**Revision:** Reviewed against the current application to confirm every on-screen message and behavior described below.

---

## 1. INTRODUCTION

This document defines the Test Plan and Test Results for the **WDC Stacker FGI** application — a warehouse system used to organize incoming items ("holders") into storage boxes/ship boxes on a rack, and to withdraw batches of those holders against a withdrawal request. This plan is organized by user action: Login, Scanning an Item, Placing an Item, Removing an Item from the Rack, and the full **Job Withdrawal** module (request list, acknowledge, preview, holder verification, and withdrawal).

**Test Result Summary**

- **Tester(s):** QA Team
- **Date of Test:** July 15, 2026

| Test No. | Description | Accepted / Not Accepted |
|---|---|---|
| TS-01 | User login using company credentials | Pending |
| TS-02 | Scanning an item and receiving a suggested storage box | Pending |
| TS-03 | Assigning (placing) a scanned item into its suggested box/ship box | Pending |
| TS-04 | Disassociating (removing) an item from a box it was placed in | Pending |
| TS-05 | Job Withdrawal: request list, acknowledge, preview, and disassociation | Pending |

---

## 2. TEST PLAN PRE-REQUISITES

**Application & Environment Setup Summary**

- **Application Name:** WDC Stacker
- **Version:** 1.0
- **UAT Environment:**
  - Test environment hosted on a Windows server, separate from the live production system
  - Uses a test database and a test connection to the company's employee record and privilege system
  - Uses a test/UAT connection to the Active Directory login system
- **Production Environment:**
  - Live environment hosted on a Windows server
  - Connected to the live database and the live employee record and privilege system
  - Connected to the live Active Directory login system

**System Dependencies**

| Dependency | Impact | Test Status |
|---|---|---|
| Company Login System (Active Directory) | If unavailable, no user can log in and the entire application becomes inaccessible | Pending |
| Employee Privilege System (FEATS) | If unavailable, item scanning, placement, and removal checks cannot be completed since these rely on FEATS item lookups | Pending |
| Warehouse Database | If unavailable, item placement, scanning, and removal cannot be completed; the rack/box view cannot be loaded | Pending |

**Identification**

| Role | Description | Assigned Tester/User |
|---|---|---|
| Warehouse Operator | Standard user who logs in, scans items, places them into boxes, and removes items from boxes on the Home page | Tester A |
| QA Lead | Reviews test execution and signs off results | Tester B |

---

## 3. TEST CASE 1 — Login and Session

- **Test Name:** User Login (Successful and Failed Attempts)
- **Feature:** Users sign in with their company username and password on the Login page. Once signed in, the system remembers the user for the rest of the session and uses that to authorize every action taken in the app.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Enter a valid company username and correct password, then sign in | Login succeeds with a "Login successful" confirmation; the user is taken to the Home page, and the Configuration tab is shown or hidden based on the user's permission level | Pending | Pending | Happy path |
| 2 | Enter a valid username with an **incorrect** password | Login is rejected with "Invalid username or password." | Pending | Pending | Negative — wrong password |
| 3 | Submit with the username and/or password **left blank** | Login is rejected with "Incorrect login details"; sign-in is not attempted | Pending | Pending | Negative — incomplete credentials |
| 4 | Enter a username that includes a domain prefix or suffix, e.g. `DOMAIN\username` or `username@domain.com` | Login is rejected with "Incorrect login details" — **note:** confirm with development whether usernames with a domain prefix/suffix should be accepted, since the system appears to reject them outright instead of stripping the domain part as intended | Pending | Pending | Negative — confirm intended behavior with dev |
| 5 | Attempt to log in while the company login system is temporarily unavailable | Login fails with "Authentication service error."; no crash or blank screen | Pending | Pending | Negative — login service outage |
| 6 | Attempt to log in with a disabled or locked company account | Login is rejected with "Invalid username or password." (the system does not show a separate message for disabled/locked accounts) | Pending | Pending | Negative — disabled account |
| 7 | Log in as a user who belongs to the admin group | The Configuration tab/page is available to this user | Pending | Pending | Positive — admin access |
| 8 | Log in as a user who does **not** belong to the admin group | The Configuration tab/page is hidden or blocked for this user | Pending | Pending | Negative — non-admin access |
| 9 | Try to use any part of the app (scanning, placing, removing, or job withdrawal) without being logged in, or after being logged out | The action is blocked with a message such as "Login token is missing." or "Invalid or expired token." | Pending | Pending | Negative — missing/invalid session |
| 10 | Stay logged in past the normal session length, then try to use the app | The action is blocked with "Invalid or expired token."; the user should be returned to the Login page | Pending | Pending | Negative — expired session |

---

## 4. TEST CASE 2 — Scanning an Item and Receiving a Suggested Storage Box

- **Test Name:** Item Scan (Successful and Failed Attempts)
- **Feature:** On the Home page, an operator scans or types an item's ID. The system checks the item, in order, against several rules (part number/product name, bin name, holds, quantity, and process status), and if all rules pass, suggests a storage box and ship box for it. If any rule fails, it shows the reason why the item cannot proceed.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Scan a valid, unplaced item that passes every rule and has an available box | A box and ship box are suggested; a "Validation Pass!" message is shown; the Place button becomes enabled | Pending | Pending | Happy path |
| 2 | Leave the item field empty and press Enter/Scan | Nothing happens; no request is sent | Pending | Pending | Negative — client-side guard |
| 3 | Scan while not logged in | A "Login token is missing. Please sign in again." message appears; no request is sent | Pending | Pending | Negative — client-side guard |
| 4 | Attempt to scan without providing an item ID at all | The system rejects the attempt with "Holder is required." | Pending | Pending | Negative — server-side guard |
| 5 | Scan with a session the system no longer recognizes as valid | The scan is rejected with "Invalid or expired token."; Place button stays disabled | Pending | Pending | Negative — invalid session |
| 6 | Scan an item ID that does not exist in the system | Item is rejected with "HolderJob record was not found."; no box is suggested | Pending | Pending | Negative — unknown item |
| 7 | Scan while the lookup system used to verify the item is unavailable | The item is rejected with the error message returned by that system; no box is suggested | Pending | Pending | Negative — lookup system outage |
| 8 | Scan an item that is not currently at the correct processing stage for this station | Item is rejected with "Operation is not valid" | Pending | Pending | Negative — wrong stage |
| 9 | Scan an item that currently has a Parent Holder value on file | **No effect currently.** This check is intentionally turned off for now; the item proceeds as if it had no Parent Holder. Confirm with development when this should be turned back on before go-live | Pending | Pending | Currently disabled — confirm re-enable date with dev |
| 10 | Scan an item that has already been marked with a Ship Ticket | **No effect currently.** This check is also intentionally turned off for now, for the same reason | Pending | Pending | Currently disabled — confirm re-enable date with dev |
| 11 | Scan an item with a missing Part Number (or missing both Part Number and Product Name) | The item is automatically moved out and placed on hold with the reason "NO PART NUMBER." On success, the message reads: "PartNumber is missing. Holder has been moved out to RBF2 and held with reason TAP: NO PART NUMBER." | Pending | Pending | Negative — missing part number |
| 12 | Same as above, but the automatic move-out step fails | The message reads: "MoveOut to 735617 RBF2 failed:" followed by the reason it failed; no hold is attempted | Pending | Pending | Negative — move-out failure |
| 13 | Same as #11, but the move-out succeeds and placing the hold afterward fails | The message reads: "MoveOut to RBF2 succeeded, but Hold failed:" followed by the reason, plus a note that the item is now moved out but not held | Pending | Pending | Negative — partial failure, item left un-held |
| 14 | Scan an item that has a Part Number but is missing its Product Name | Item is rejected with "ProductName is missing." (no automatic move-out happens in this case — only the combined missing-both case in step 11 does that) | Pending | Pending | Negative — missing product name only |
| 15 | Scan an item whose Bin Name is not exactly 5 characters | The item is automatically moved out. On success, the message reads: "BinName must be exactly 5 characters. Current BinName:" followed by the value, and confirms it was moved out | Pending | Pending | Negative — invalid bin name |
| 16 | Same as above, but the automatic move-out fails | The message reads: "MoveOut to 735617 RBF2 failed:" followed by the reason | Pending | Pending | Negative — move-out failure |
| 17 | Scan an item that currently has an active in-site hold reason or comment on file | The item is rejected with a message showing "Holder has FEATS hold." along with the hold reason and comment | Pending | Pending | Negative — in-site hold |
| 18 | Scan an item that comes back flagged with a hold or slider issue from the automated handling check | The item is automatically moved out. On success, the message states the hold/slider issue found and confirms the item was moved out | Pending | Pending | Negative — hold/slider issue |
| 19 | Scan an item while the automated handling check itself is unavailable | The message reports that the automated handling check failed, along with the reason | Pending | Pending | Negative — handling-system outage |
| 20 | Scan an item whose recorded quantities (good qty, loaded qty, slider count) do not all match | Item is rejected with "Holder QTY is invalid" | Pending | Pending | Negative — quantity mismatch |
| 21 | Scan an item that is not yet marked as "in process," and the system's attempt to move it in fails | The message reads: "MoveIn failed:" followed by the reason | Pending | Pending | Negative — move-in failure |
| 22 | Scan an item whose experiment information cannot be resolved to a pen number | The message shows the underlying lookup error, or "Unable to query ExperimentDefinition." | Pending | Pending | Negative — pen number lookup failure |
| 23 | Scan an item when no matching box already exists and the storage area has reached its configured limits (racks, layers, boxes, items per box, or ship boxes) | Item is rejected with "No compatible FGI target is available and all settings are maxed out." | Pending | Pending | Negative — storage full |
| 24 | Scan an item that unexpectedly reaches box suggestion with no Part Number or Product Name on record (safety-net check) | Item is rejected with "PartNum and ProductName are required for FGI targeting." | Pending | Pending | Negative — safety-net check, should not normally occur |

---

## 5. TEST CASE 3 — Assigning (Placing) a Scanned Item Into Its Suggested Box

- **Test Name:** Item Placement (Successful and Failed Attempts)
- **Feature:** After a successful scan, the operator clicks Assign/Place to confirm putting the item into the suggested box and ship box. The system re-checks several details independently at this step (part number, product name, quantity, and pen number), which can still fail even after a successful scan, and requires a ship box to be selected.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | After a successful scan, click Assign/Place with both box and ship box selected | Item is placed successfully with "Holder assigned to ShipBox successfully."; the rack view updates | Pending | Pending | Happy path |
| 2 | Place an item into a box/ship box that doesn't exist yet (a newly suggested target) | The box and ship box are automatically created and the item is placed; same success message as #1 | Pending | Pending | Edge case — new box/ship box creation |
| 3 | Attempt to place without an item ID or without a box selected | The system rejects the attempt with "Holder is required." or "BoxNo is required." | Pending | Pending | Negative — server-side guard |
| 4 | Attempt to place without a ship box selected | The system rejects the attempt with "ShipBoxName is required for FGI." — **note:** an older, slightly different wording of this same message also exists elsewhere in the system and does not currently appear to the user; confirm with development that only one consistent message is needed | Pending | Pending | Negative — duplicate validation with inconsistent wording |
| 5 | Successfully scan an item, then have the login session expire before clicking Assign/Place | Placement is rejected with "Invalid or expired token." | Pending | Pending | Negative — session expired between scan and place |
| 6 | Successfully scan an item, then have it disappear from the source system before clicking Assign/Place | Placement is rejected with "HolderJob record was not found." | Pending | Pending | Negative — item disappeared between scan and place |
| 7 | Successfully scan an item whose part number or product name fails the additional checks performed only at placement time (not during scanning) | Placement is rejected with "PartNumber or ProductName is missing." | Pending | Pending | Negative — re-validated at placement, not just scan |
| 8 | Successfully scan an item whose recorded quantities no longer match at placement time | Placement is rejected with "Holder QTY is invalid" | Pending | Pending | Negative — quantity mismatch |
| 9 | Successfully scan an item whose experiment/pen number information cannot be resolved at placement time | Placement is rejected with the underlying lookup error, or "Unable to query ExperimentDefinition." | Pending | Pending | Negative — pen number lookup failure |
| 10 | Successfully scan an item, then have another operator place that same item first, then click Assign/Place | Placement is rejected with "Holder is already assigned."; no change to the rack | Pending | Pending | Negative — race condition between two operators |
| 11 | Placement fails due to a business rule being violated in the warehouse database | Placement fails with a specific error message describing the rule that was violated | Pending | Pending | Negative — business-rule rejection |
| 12 | Placement fails due to an unexpected problem saving to the warehouse database | Placement fails with "Unable to Assign."; rack view remains unchanged | Pending | Pending | Negative — downstream system failure |

---

## 6. TEST CASE 4 — Disassociating (Removing) an Item from a Box It Was Placed In

- **Test Name:** Item Removal (Successful and Failed Attempts)
- **Feature:** An operator can remove a single item from the box it was previously placed in. Holders with status 'HOLD' are displayed with their shipbox colored red on the rack view and are available for disassociation. The disassociation process for held holders includes: retrieving the hold comment and reason, releasing the holder, moving it out to RBF2, and re-applying the hold with the saved comment and reason. For non-held holders, the process is a standard move-out followed by removal.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Remove an item that is currently placed in a box, has no active hold, and completes the move-out step successfully | Removal succeeds with "Holder disassociated successfully."; the box shows as empty on the rack view | Pending | Pending | Happy path |
| 2 | Remove an item that currently has status 'HOLD' with a hold comment and reason on file | The system retrieves the hold comment and reason, releases the holder, moves it out to RBF2, and re-applies the hold with the saved comment and reason; removal succeeds with confirmation | Pending | Pending | Happy path — held holder disassociation |
| 3 | Attempt removal without being properly logged in | Removal is rejected with "Bearer token is required." or "Invalid or expired token." | Pending | Pending | Negative — missing/invalid session |
| 4 | Attempt to remove an item that no longer exists in the system's item lookup | Removal is rejected with "HolderJob record was not found." or the underlying lookup error | Pending | Pending | Negative — unknown item / lookup outage |
| 5 | Attempt to remove an item where the move-out step with the tracking system fails | Removal is rejected with a message that begins "The SQL assignment was deleted, but..." followed by the move-out error — **note:** this wording appears to say the item's placement was already removed, but it actually was not in this situation; flag to development for confirmation | Pending | Pending | Negative — possible bug in message wording |
| 6 | Attempt to remove an item whose placement record no longer exists, or is no longer in a removable state (after the move-out step succeeded) | Removal is rejected with "The holder was not found or its status is not RELEASE." | Pending | Pending | Negative — status guard on removal |
| 7 | Attempt removal while the warehouse database is temporarily unavailable | Removal fails with a clear error message; the rack view is not left in a broken or partial state | Pending | Pending | Negative — database failure |
| 8 | Confirm what information is returned after a removal | The response confirms success/failure, the message shown to the user, and the updated list of boxes for the rack view | Pending | Pending | Contract check |
| 9 | **Feature unavailable:** The held holder disassociation flow (release → moveout to RBF2 → re-apply hold) is not currently implemented | This feature is marked as unavailable pending implementation | Pending | Pending | **Feature gap — not yet implemented** |

---

## 7. TEST CASE 5 — Job Withdrawal Module

- **Test Name:** Job Withdrawal (Request List, Acknowledge, Preview, Holder Verification, Withdrawal)
- **Feature:** The Job Withdrawal tab lets an operator select an open withdrawal request, acknowledge it, review a preview of candidate items (oldest first, skipping any on hold), browse the Rack → Box → Ship Box → Holder hierarchy for that location, then verify a Shipping ID and each included item before withdrawing (permanently removing) them from the system. **This entire module was missing from the original test plan and is covered in full below.**

### 7.1 Request List

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Open the Job Withdrawal tab while logged in | Request cards load, newest first, showing Grade, Part Number, Total Qty, LEC, Pen Number, Requestor, and Date | Pending | Pending | Happy path |
| 2 | Open the tab with no one logged in | "Login token is missing." is shown; no request is sent | Pending | Pending | Negative — client-side guard |
| 3 | Open the tab from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 4 | Open the tab with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 5 | Expand a request card (▼) | Card shows Shift, Model, Category, Head Type, Remarks, Acknowledged By, Actual Output, and Status | Pending | Pending | UI detail check |
| 6 | No withdrawal requests exist | The list shows "No withdrawal requests were found." | Pending | Pending | Empty state |

### 7.2 Selecting a Request and Loading the Rack

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 7 | Select a request card that has an LEC (location) on file | The request details are shown; the Rack → Box → Ship Box view loads for that location | Pending | Pending | Happy path |
| 8 | Select a request whose LEC is blank | The request details are shown; the Rack → Box → Ship Box view loads across all locations (filtered only by Part Number, Grade, and Pen Number if provided) | Pending | Pending | Happy path — LEC is optional |
| 9 | Select a request whose location has no matching rack layout | "No rack mapping was found for the selected LEC." is shown | Pending | Pending | Negative — empty layout |
| 10 | Quickly select request A, then request B, before A's rack finishes loading | Only request B's rack is shown; A's late-arriving data is discarded | Pending | Pending | Race-condition guard |
| 11 | Click a mapped box on the rack | A box details window opens, showing the box's Grade (Bin Name), Part Number, Pen Number, and its ship boxes | Pending | Pending | Happy path |
| 12 | Click a mapped ship box inside the box details window | A holders window opens, listing Holder, Product Name, Factory, LEC, Status, and Qty for that ship box | Pending | Pending | Happy path |
| 13 | Open a ship box with zero items | The list shows "No holder records were found." | Pending | Pending | Empty state |

### 7.3 Acknowledge

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 15 | Click **ACKNOWLEDGE** on a request that has not been acknowledged yet | The request is marked as acknowledged by the current user; a "Withdrawal request acknowledged successfully." confirmation appears; the button is replaced by **WITHDRAW** | Pending | Pending | Happy path |
| 16 | Click **ACKNOWLEDGE** on a request that is already acknowledged, or that can no longer be updated | "The request was not found or was already acknowledged." is shown | Pending | Pending | Negative — already acknowledged |
| 17 | Attempt to acknowledge without a valid request selected | "RequestId is required." is shown | Pending | Pending | Negative — server-side guard |
| 18 | Acknowledge from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 19 | Acknowledge with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 20 | **UI note:** once acknowledged, the action button becomes **WITHDRAW**; if the request is already closed, it becomes **VIEW** instead. Confirm the ACKNOWLEDGE button itself is also disabled for closed requests, even if never acknowledged | Button label and enabled/disabled state match the request's acknowledged/closed status as described | Pending | Pending | UI state check |

### 7.4 Withdrawal Preview

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 21 | Click **WITHDRAW** on an acknowledged request | A preview loads candidate items for that location/part/pen number, oldest first, stopping once the requested total quantity is reached (with a small tolerance allowance) | Pending | Pending | Happy path |
| 22 | Preview a request that has no Total quantity on file | "The selected request does not contain a TOTAL." is shown; no preview is loaded | Pending | Pending | Negative — missing total |
| 23 | Preview the same request again shortly after the first preview | The previously loaded preview is reused, with a note that it was "loaded from cache" | Pending | Pending | Cache-hit path |
| 24 | Preview again after enough time has passed, or after anything on the rack has changed | A fresh preview is calculated and loaded successfully | Pending | Pending | Cache-miss / refresh path |
| 25 | Preview with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 26 | Preview from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 27 | Preview with a missing or negative total quantity | "TOTAL is required and cannot be negative." is shown | Pending | Pending | Negative — server-side guard |
| 28 | Preview while the hold-check lookup for a candidate item fails | A clear error naming the item and the reason the hold check could not be completed is shown | Pending | Pending | Negative — lookup outage during preview |
| 29 | Preview where a candidate item currently has an active hold reason or comment on file | The item is excluded from the total and marked "IN-SITE HOLD"; the next oldest available item is used to make up the quantity instead | Pending | Pending | Automatic backfill on hold |
| 30 | **Confirmed gap:** the automated-handling hold check is not currently active during withdrawal preview | Only the standard in-site hold is enforced; an item with an automated-handling hold/slider issue can still be included and later withdrawn. Confirm whether this is acceptable before go-live | Pending | Pending | **Confirmed gap — flag to development** |
| 31 | Re-check an item that was previously found to be on hold, after that hold has since been cleared | The earlier "on hold" result may still be shown until the system is restarted; the item is not automatically re-checked | Pending | Pending | **Confirmed gap — flag to development** |
| 32 | Preview where the requested total is reached before all candidate items are reviewed | The remaining items are simply left out of the total and are not shown as "skipped by hold," since they were never checked | Pending | Pending | Boundary check |

### 7.5 Verify Holders Window

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 33 | The verification window opens after a successful preview | Shows the selected request's summary, Total vs. Total Qty, Shipping ID and Holder verification fields, and the Included / Skipped-by-Hold item tables | Pending | Pending | Happy path |
| 34 | Enter a Shipping ID and click VERIFY | "Shipping ID verified." is shown — **note:** this only checks that something was entered; it does not check the Shipping ID against any outside record at this stage | Pending | Pending | Basic entry check only |
| 35 | Click VERIFY with the Shipping ID left blank | "Shipping ID is required." is shown; the Withdraw/Close button stays disabled | Pending | Pending | Negative — required field |
| 36 | Scan/type an included item and click VERIFY | That item is marked "VERIFIED"; the row is highlighted; the progress message updates to show how many of the included items have been verified so far | Pending | Pending | Happy path |
| 37 | Scan an item that is not in the Included list (for example, one that was skipped due to a hold, or does not exist) | "Holder not found in Included." is shown; progress does not change; the Withdraw/Close button stays disabled | Pending | Pending | Negative — item not included |
| 38 | Scan the same item twice | The first scan verifies it; the second scan has no further effect since it is already verified | Pending | Pending | Edge case — duplicate scan |
| 39 | The request has no included items at all (for example, everything was on hold) | The Withdraw/Close button can never be enabled for this request | Pending | Pending | **Edge case to confirm as intended** |
| 40 | All included items are verified and the Shipping ID is verified | The button at the bottom becomes enabled, labeled **WITHDRAW** (or **CLOSE** if the request is already closed) | Pending | Pending | Enablement check |
| 41 | Click **WITHDRAW** with everything verified | A confirmation window appears: "This will permanently remove the Holders from STACKER data. This action cannot be undone." along with the item count and total quantity | Pending | Pending | Happy path |
| 42 | Click **CLOSE** when the request is already closed | The window simply closes with no confirmation and no changes made — but the button only becomes clickable once the Shipping ID and every item are (re-)verified again in this session; confirm this is the intended experience for reviewing a closed request | Pending | Pending | **Confirmed UX quirk — flag to development/UX** |
| 43 | In the confirmation window, click CANCEL | The confirmation closes; nothing is withdrawn; previously entered/verified information is kept | Pending | Pending | Happy path |
| 44 | In the confirmation window, click **YES, WITHDRAW** | The button shows a loading indicator and "WITHDRAWING..."; on success, the window closes and a success message is shown | Pending | Pending | Happy path |

### 7.6 Completing the Withdrawal

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 45 | Confirm withdrawal with a verified Shipping ID and a valid list of included items | The items are permanently removed from the system; any box/ship box left completely empty as a result is also removed; the request's Actual Output is increased by the withdrawn quantity and its Status is set to Closed; a "Holders were removed from STACKER data successfully." confirmation is shown with counts of what was removed | Pending | Pending | Happy path |
| 46 | **Confirmed gap:** the Shipping ID is not actually checked against any outside shipping record — any non-blank value is currently accepted | Withdrawal succeeds even with a Shipping ID that does not correspond to a real shipment | Pending | Pending | **Confirmed gap — flag to development** |
| 47 | **Confirmed gap:** withdrawn items are not currently grouped under the Shipping ID in the shipping/tracking system | Items are removed from the warehouse system but are not linked to the Shipping ID anywhere else | Pending | Pending | **Confirmed gap — flag to development** |
| 48 | **Confirmed gap:** no "move out" step is currently sent to the tracking system as part of a withdrawal | This is different from the single-item removal flow in Section 6, which does perform a move-out step | Pending | Pending | **Confirmed gap — flag to development** |
| 49 | **Confirmed gap:** no confirmation email is currently sent after a successful withdrawal | No one is notified automatically when a withdrawal is completed | Pending | Pending | **Confirmed gap — flag to development** |
| 50 | Attempt withdrawal with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 51 | Attempt withdrawal from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 52 | Attempt withdrawal without a valid request selected | "RequestId is required." is shown | Pending | Pending | Negative — server-side guard |
| 53 | Attempt withdrawal with the Shipping ID blank | "ShippingId is required." is shown | Pending | Pending | Negative — server-side guard |
| 54 | Attempt withdrawal with no items included | "At least one included Holder is required." is shown | Pending | Pending | Negative — server-side guard |
| 55 | Attempt withdrawal with an excessively long list of items | "Too many included Holders." is shown | Pending | Pending | Negative — server-side guard |
| 56 | Attempt withdrawal with a blank item, or one with an unusually long ID | "Every Holder is required and cannot exceed 50 characters." is shown | Pending | Pending | Negative — server-side guard |
| 57 | Attempt withdrawal with the same item listed more than once | "The included Holder list contains duplicates." is shown | Pending | Pending | Negative — server-side guard |
| 58 | Attempt withdrawal for a request that no longer exists (for example, it was removed by someone else in the meantime) | "The withdrawal request no longer exists." is shown | Pending | Pending | Negative — stale request |
| 59 | Attempt withdrawal for a request whose total quantity is no longer valid | "The withdrawal request no longer has valid TOTAL values." is shown | Pending | Pending | Negative — stale request data |
| 60 | Attempt withdrawal where the items actually available no longer match what was verified (for example, someone else removed one first) | "The Holder rows changed before deletion. No STACKER data was removed." is shown; nothing is removed | Pending | Pending | Negative — concurrent modification |
| 61 | Any other unexpected system error occurs while completing the withdrawal | Nothing is removed; confirm the user sees a clear, generic error message rather than a broken screen | Pending | Pending | Negative — unhandled failure |
| 62 | Successful withdrawal empties a ship box or box entirely | The now-empty ship box/box is automatically removed; it no longer appears in the rack/ship box views | Pending | Pending | Cascade cleanup check |
| 63 | Successful withdrawal | The previously saved preview for that request is cleared, so a stale preview is never reused afterward | Pending | Pending | Cache-invalidation check |
| 64 | After a successful withdrawal, the screen refreshes | The rack view and the request list both reload; if either refresh fails on its own, a separate message is shown without contradicting the already-confirmed success of the withdrawal itself | Pending | Pending | UI refresh-failure isolation |

---

## 8. VALIDATION ERRORS / DEPENDENCY FAILURES (Cross-Cutting)

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Log in while the company login system is unreachable | "Authentication service error." is shown | Pending | Pending | See Section 3, Step 5 |
| 2 | Scan/Place/Remove/Preview while the item-tracking lookup system is unreachable | The relevant screen shows the error returned by that system; nothing fails silently | Pending | Pending | |
| 3 | Scan while the automated handling check system is unreachable | A message reports that the check failed, along with the reason | Pending | Pending | |
| 4 | Place/withdraw while the warehouse database is unreachable | "Unable to Assign." (placement) or a rolled-back withdrawal with a clear error message | Pending | Pending | |
| 5 | Re-scan an item that was previously found on hold and has since been released | The earlier "on hold" result may still be shown until the system is restarted | Pending | Pending | Same gap as Section 7.4, Step 31 |

---

## 9. APPENDIX

- Screenshots of the Home page rack view before and after placing an item: *(attach evidence)*
- Screenshots or logs of login attempts (successful and failed): *(attach evidence)*
- Screenshots of the Job Withdrawal tab: request list, box/ship box/holder windows, verify-holders window, and confirmation window: *(attach evidence)*
- Screenshots or notes on any error messages encountered during testing: *(attach evidence)*
- List of confirmed gaps to review with development before go-live:
  - The Parent Holder and Ship Ticket scan checks are currently turned off.
  - The automated-handling hold check during withdrawal preview is currently turned off (only the standard in-site hold is enforced).
  - Previously checked hold results are not automatically refreshed; they may stay outdated until the system restarts.
  - Shipping ID entry during withdrawal is not checked against any outside shipping record.
  - The withdrawal step does not currently group items under the Shipping ID or send a move-out request to the tracking system.
  - No confirmation email is currently sent after a successful withdrawal.
  - The single-item removal message wording implies the item's placement record was already removed when the move-out step fails, but in that case it was not; flag to development for confirmation.
  - The "Ship Box is required" message for placing an item exists in two slightly different wordings in the system; confirm with development that only one, consistent message is shown to the user.

---

## 10. CUSTOMER SURVEY

Please rate the following on a scale of 1 (Poor) to 5 (Excellent):

| Question | Response |
|---|---|
| How clear was this test plan? | |
| Are you satisfied with the functionality tested (login, scan, assign, disassociate, withdrawal)? | |
| How would you rate the application's performance during testing? | |
| How would you rate the usability of the Home page and Job Withdrawal tab? | |
| Additional comments or suggestions: | |
