# WDC-FGI Stacker
## Test Plan

**Author:** Brian Xavier Hipolito
**Creation Date:** August 3, 2026

---

## Table of Contents

- Introduction
- Test Result Summary
- Test Plan Pre-requisites
- Test Case 1: User Login and Session Authentication
- Test Case 2: Job Scanning and Suggested Box
- Test Case 3: Job Batching / Assignment
- Test Case 4: Job Withdrawal
- Test Case 5: Individual Holder Disassociation
- Test Case 6: Job Unship
- Appendix
- Customer Survey

**Test Result Summary**

Fill up the necessary column after conducting the test following the test plan.

**Tester(s):** Melanie Marfa
**Date of Test:**

---

## Introduction

This document defines the Test Plan for the **WDC Stacker FGI** application — a warehouse system used to organize incoming items ("holders") into storage boxes/ship boxes on a rack, and to withdraw batches of those holders against a withdrawal request.

---

## Test Plan Pre-requisites

### Application & Environment Setup Summary

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

### System Dependencies

| Dependency | Impact | Test Status |
|---|---|---|
| Company Login System (Active Directory) | If unavailable, no user can log in and the entire application becomes inaccessible | Pending |
| Employee Privilege System (FEATS) | If unavailable, item scanning, placement, and removal checks cannot be completed since these rely on FEATS item lookups | Pending |
| Warehouse Database | If unavailable, item placement, scanning, and removal cannot be completed; the rack/box view cannot be loaded | Pending |

### Identification

Identify who to take up which role / user category - user id to be used during the testing phase

| Role | Description | Assigned Tester/User |
|---|---|---|
| Warehouse Operator | Standard user who logs in, scans items, places them into boxes, and removes items from boxes on the Home page | |
| QA Lead | Reviews test execution and signs off results | |

---

## Test Case 1

**Test Name:** User Login and Session Authentication
**Feature:** Users sign in with their company username and password on the Login page. Once verified, the system checks whether the user also has permission to access the Settings page.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Enter a valid company username and correct password, then sign in | Login succeeds with a "Login successful" confirmation; the user is taken to the Home page, and the Configuration tab is shown or hidden based on the user's permission level | Pending | Pending | Happy path |
| 2 | Enter a valid username with an **incorrect** password | Login is rejected with "Invalid username or password." | Pending | Pending | Negative — wrong password |
| 3 | Submit with the username and/or password **left blank** | Login is rejected with "Incorrect login details"; sign-in is not attempted | Pending | Pending | Negative — incomplete credentials |
| 4 | Enter a username that includes a domain prefix or suffix, e.g. `DOMAIN\username` or `username@domain.com` | Login is rejected with "Incorrect login details" | Pending | Pending | Negative — domain prefix/suffix |
| 5 | Attempt to log in while the company login system is temporarily unavailable | Login fails with "Authentication service error."; no crash or blank screen | Pending | Pending | Negative — login service outage |
| 6 | Attempt to log in with a disabled or locked company account | Login is rejected with "Invalid username or password." | Pending | Pending | Negative — disabled account |
| 7 | Log in as a user who belongs to the admin group | The Configuration tab/page is available to this user | Pending | Pending | Positive — admin access |
| 8 | Log in as a user who does **not** belong to the admin group | The Configuration tab/page is hidden or blocked for this user | Pending | Pending | Negative — non-admin access |
| 9 | Try to use any part of the app (scanning, placing, removing, or job withdrawal) without being logged in, or after being logged out | The action is blocked with a message such as "Login token is missing." or "Invalid or expired token." | Pending | Pending | Negative — missing/invalid session |
| 10 | Stay logged in past the normal session length, then try to use the app | The action is blocked with "Invalid or expired token."; the user should be returned to the Login page | Pending | Pending | Negative — expired session |

---

## Test Case 2

**Test Name:** Job Scanning and Suggested Box
**Feature:** On the Home page, an operator scans or types an item's ID. The system checks the item, in order, against several rules, and if all rules pass, suggests a storage box for it. If any rule fails, it shows the reason why the item cannot proceed and the Place button stays disabled. This section reflects a verified, line-by-line review of the actual checking logic.

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
| 9 | Scan an item that currently has a Parent Holder value on file | **No effect currently.** This check is intentionally turned off for now; the item proceeds as if it had no Parent Holder | Pending | Pending | Currently disabled |
| 10 | Scan an item that has already been marked with a Ship Ticket | **No effect currently.** This check is also intentionally turned off for now | Pending | Pending | Currently disabled |
| 11 | Scan an item with a missing Part Number (or missing both Part Number and Product Name) | The item is automatically moved out and placed on hold with the reason "NO PART NUMBER." On success, the message reads: "PartNumber is missing. Holder has been moved out to RBF2 and held with reason TAP: NO PART NUMBER." | Pending | Pending | Negative — missing part number |
| 12 | Same as above, but the automatic move-out step fails | The message reads: "MoveOut to 735617 RBF2 failed:" followed by the reason it failed; no hold is attempted | Pending | Pending | Negative — move-out failure |
| 13 | Same as #11, but the move-out succeeds and placing the hold afterward fails | The message reads: "MoveOut to RBF2 succeeded, but Hold failed:" followed by the reason, plus a note that the item is now moved out but not held | Pending | Pending | Negative — partial failure |
| 14 | Scan an item that has a Part Number but is missing its Product Name | Item is rejected with "ProductName is missing." | Pending | Pending | Negative — missing product name only |
| 15 | Scan an item whose Bin Name is not exactly 5 characters | The item is automatically moved out. On success, the message reads: "BinName must be exactly 5 characters. Current BinName:" followed by the value, and confirms it was moved out | Pending | Pending | Negative — invalid bin name |
| 16 | Same as above, but the automatic move-out fails | The message reads: "MoveOut to 735617 RBF2 failed:" followed by the reason | Pending | Pending | Negative — move-out failure |
| 17 | Scan an item that currently has an active in-site hold reason or comment on file | The item is rejected with a message showing "Holder has FEATS hold." along with the hold reason and comment | Pending | Pending | Negative — in-site hold |
| 18 | Scan an item that comes back flagged with a hold or slider issue from the automated handling check | The item is automatically moved out. On success, the message states the hold/slider issue found and confirms the item was moved out | Pending | Pending | Negative — hold/slider issue |
| 19 | Scan an item while the automated handling check itself is unavailable | The message reports that the automated handling check failed, along with the reason | Pending | Pending | Negative — handling-system outage |
| 20 | Scan an item whose recorded quantities (good qty, loaded qty, slider count) do not all match | Item is rejected with "Holder QTY is invalid" | Pending | Pending | Negative — quantity mismatch |
| 21 | Scan an item that is not yet marked as "in process," and the system's attempt to move it in fails | The message reads: "MoveIn failed:" followed by the reason | Pending | Pending | Negative — move-in failure |
| 22 | Scan an item whose experiment information cannot be resolved to a pen number | The message shows the underlying lookup error, or "Unable to query ExperimentDefinition." | Pending | Pending | Negative — pen number lookup failure |
| 23 | Scan an item when no matching box already exists and the storage area has reached its configured limits | Item is rejected with "No compatible FGI target is available and all settings are maxed out." | Pending | Pending | Negative — storage full |
| 24 | Scan an item that unexpectedly reaches box suggestion with no Part Number or Product Name on record (safety-net check) | Item is rejected with "PartNum and ProductName are required for FGI targeting." | Pending | Pending | Negative — safety-net check |

---

## Test Case 3

**Test Name:** Job Batching / Assignment
**Feature:** After a successful scan, the operator clicks Assign to place the holder into the suggested Box/ShipBox. FGI re-validates part number, product name, quantity, and pen number independently of the scan step, and additionally requires a ShipBoxName.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | After a successful scan, click Assign/Place with both box and ship box selected | Item is placed successfully with "Holder assigned to ShipBox successfully."; the rack view updates | Pending | Pending | Happy path |
| 2 | Place an item into a box/ship box that doesn't exist yet (a newly suggested target) | The box and ship box are automatically created and the item is placed; same success message as #1 | Pending | Pending | Edge case — new box/ship box creation |
| 3 | Attempt to place without an item ID or without a box selected | The system rejects the attempt with "Holder is required." or "BoxNo is required." | Pending | Pending | Negative — server-side guard |
| 4 | Attempt to place without a ship box selected | The system rejects the attempt with "ShipBoxName is required for FGI." | Pending | Pending | Negative — ship box required |
| 5 | Successfully scan an item, then have the login session expire before clicking Assign/Place | Placement is rejected with "Invalid or expired token." | Pending | Pending | Negative — session expired between scan and place |
| 6 | Successfully scan an item, then have it disappear from the source system before clicking Assign/Place | Placement is rejected with "HolderJob record was not found." | Pending | Pending | Negative — item disappeared between scan and place |
| 7 | Successfully scan an item whose part number or product name fails the additional checks performed only at placement time (not during scanning) | Placement is rejected with "PartNumber or ProductName is missing." | Pending | Pending | Negative — re-validated at placement |
| 8 | Successfully scan an item whose recorded quantities no longer match at placement time | Placement is rejected with "Holder QTY is invalid" | Pending | Pending | Negative — quantity mismatch |
| 9 | Successfully scan an item whose experiment/pen number information cannot be resolved at placement time | Placement is rejected with the underlying lookup error, or "Unable to query ExperimentDefinition." | Pending | Pending | Negative — pen number lookup failure |
| 10 | Successfully scan an item, then have another operator place that same item first, then click Assign/Place | Placement is rejected with "Holder is already assigned."; no change to the rack | Pending | Pending | Negative — race condition |
| 11 | Placement fails due to a business rule being violated in the warehouse database | Placement fails with a specific error message describing the rule that was violated | Pending | Pending | Negative — business-rule rejection |
| 12 | Placement fails due to an unexpected problem saving to the warehouse database | Placement fails with "Unable to Assign."; rack view remains unchanged | Pending | Pending | Negative — downstream system failure |

---

## Test Case 4

**Test Name:** Job Withdrawal
**Feature:** The Job Withdrawal tab lets an operator select an open KITTING_REQUEST, acknowledge it, review a FIFO/hold-aware preview of candidate holders, browse the Rack → Box → ShipBox → Holder hierarchy for that LEC, then verify a Shipping ID and each included holder before withdrawing (deleting) them from STACKER data.

### 4.1 Request List

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Open the Job Withdrawal tab while logged in | Request cards load, newest first, showing Grade, Part Number, Total Qty, LEC, Pen Number, Requestor, and Date | Pending | Pending | Happy path |
| 2 | Open the tab with no one logged in | "Login token is missing." is shown; no request is sent | Pending | Pending | Negative — client-side guard |
| 3 | Open the tab from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 4 | Open the tab with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 5 | Expand a request card (▼) | Card shows Shift, Model, Category, Head Type, Remarks, Acknowledged By, Actual Output, and Status | Pending | Pending | UI detail check |
| 6 | No withdrawal requests exist | The list shows "No withdrawal requests were found." | Pending | Pending | Empty state |

### 4.2 Selecting a Request

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 7 | Select a request card that has an LEC (location) on file | The request details are shown; the Rack → Box → Ship Box view loads for that location | Pending | Pending | Happy path |
| 8 | Select a request whose LEC is blank | The request details are shown; the Rack → Box → Ship Box view loads across all locations (filtered only by Part Number, Grade, and Pen Number if provided) | Pending | Pending | Happy path — LEC is optional |
| 9 | Select a request whose location has no matching rack layout | "No rack mapping was found for the selected LEC." is shown | Pending | Pending | Negative — empty layout |
| 10 | Quickly select request A, then request B, before A's rack finishes loading | Only request B's rack is shown; A's late-arriving data is discarded | Pending | Pending | Race-condition guard |
| 11 | Click a mapped box on the rack | A box details window opens, showing the box's Grade (Bin Name), Part Number, Pen Number, and its ship boxes | Pending | Pending | Happy path |
| 12 | Click a mapped ship box inside the box details window | A holders window opens, listing Holder, Product Name, Factory, LEC, Status, and Qty for that ship box | Pending | Pending | Happy path |
| 13 | Open a ship box with zero items | The list shows "No holder records were found." | Pending | Pending | Empty state |

### 4.3 Verify Withdrawal Request

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 14 | Click **ACKNOWLEDGE** on a request that has not been acknowledged yet | The request is marked as acknowledged by the current user; a "Withdrawal request acknowledged successfully." confirmation appears; the button is replaced by **WITHDRAW** | Pending | Pending | Happy path |
| 15 | Click **ACKNOWLEDGE** on a request that is already acknowledged, or that can no longer be updated | "The request was not found or was already acknowledged." is shown | Pending | Pending | Negative — already acknowledged |
| 16 | Attempt to acknowledge without a valid request selected | "RequestId is required." is shown | Pending | Pending | Negative — server-side guard |
| 17 | Acknowledge from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 18 | Acknowledge with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 19 | Click **WITHDRAW** on an acknowledged request | A preview loads candidate items for that location/part/pen number, oldest first, stopping once the requested total quantity is reached (with a small tolerance allowance) | Pending | Pending | Happy path |
| 20 | Preview a request that has no Total quantity on file | "The selected request does not contain a TOTAL." is shown; no preview is loaded | Pending | Pending | Negative — missing total |
| 21 | Preview the same request again shortly after the first preview | The previously loaded preview is reused, with a note that it was "loaded from cache" | Pending | Pending | Cache-hit path |
| 22 | Preview again after enough time has passed, or after anything on the rack has changed | A fresh preview is calculated and loaded successfully | Pending | Pending | Cache-miss / refresh path |
| 23 | Preview with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 24 | Preview from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 25 | Preview with a missing or negative total quantity | "TOTAL is required and cannot be negative." is shown | Pending | Pending | Negative — server-side guard |
| 26 | Preview while the hold-check lookup for a candidate item fails | A clear error naming the item and the reason the hold check could not be completed is shown | Pending | Pending | Negative — lookup outage during preview |
| 27 | Preview where a candidate item currently has an active hold reason or comment on file | The item is excluded from the total and marked "IN-SITE HOLD"; the next oldest available item is used to make up the quantity instead | Pending | Pending | Automatic backfill on hold |
| 28 | The verification window opens after a successful preview | Shows the selected request's summary, Total vs. Total Qty, Shipping ID and Holder verification fields, and the Included / Skipped-by-Hold item tables | Pending | Pending | Happy path |
| 29 | Enter a Shipping ID and click VERIFY | "Shipping ID verified." is shown | Pending | Pending | Basic entry check only |
| 30 | Click VERIFY with the Shipping ID left blank | "Shipping ID is required." is shown; the Withdraw/Close button stays disabled | Pending | Pending | Negative — required field |
| 31 | Scan/type an included item and click VERIFY | That item is marked "VERIFIED"; the row is highlighted; the progress message updates to show how many of the included items have been verified so far | Pending | Pending | Happy path |
| 32 | Scan an item that is not in the Included list (for example, one that was skipped due to a hold, or does not exist) | "Holder not found in Included." is shown; progress does not change; the Withdraw/Close button stays disabled | Pending | Pending | Negative — item not included |
| 33 | Scan the same item twice | The first scan verifies it; the second scan has no further effect since it is already verified | Pending | Pending | Edge case — duplicate scan |
| 34 | All included items are verified and the Shipping ID is verified | The button at the bottom becomes enabled, labeled **WITHDRAW** (or **CLOSE** if the request is already closed) | Pending | Pending | Enablement check |
| 35 | Click **WITHDRAW** with everything verified | A confirmation window appears: "This will permanently remove the Holders from STACKER data. This action cannot be undone." along with the item count and total quantity | Pending | Pending | Happy path |
| 36 | In the confirmation window, click CANCEL | The confirmation closes; nothing is withdrawn; previously entered/verified information is kept | Pending | Pending | Happy path |
| 37 | In the confirmation window, click **YES, WITHDRAW** | The button shows a loading indicator and "WITHDRAWING..."; on success, the window closes and a success message is shown | Pending | Pending | Happy path |
| 38 | Confirm withdrawal with a verified Shipping ID and a valid list of included items | The items are permanently removed from the system; any box/ship box left completely empty as a result is also removed; the request's Actual Output is increased by the withdrawn quantity and its Status is set to Closed; a "Holders were removed from STACKER data successfully." confirmation is shown with counts of what was removed | Pending | Pending | Happy path |
| 39 | Attempt withdrawal with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 40 | Attempt withdrawal from a version of the app that is not permitted to use Job Withdrawal | Access is blocked | Pending | Pending | Negative — access restriction |
| 41 | Attempt withdrawal without a valid request selected | "RequestId is required." is shown | Pending | Pending | Negative — server-side guard |
| 42 | Attempt withdrawal with the Shipping ID blank | "ShippingId is required." is shown | Pending | Pending | Negative — server-side guard |
| 43 | Attempt withdrawal with no items included | "At least one included Holder is required." is shown | Pending | Pending | Negative — server-side guard |
| 44 | Attempt withdrawal with an excessively long list of items | "Too many included Holders." is shown | Pending | Pending | Negative — server-side guard |
| 45 | Attempt withdrawal with a blank item, or one with an unusually long ID | "Every Holder is required and cannot exceed 50 characters." is shown | Pending | Pending | Negative — server-side guard |
| 46 | Attempt withdrawal with the same item listed more than once | "The included Holder list contains duplicates." is shown | Pending | Pending | Negative — server-side guard |
| 47 | Attempt withdrawal for a request that no longer exists (for example, it was removed by someone else in the meantime) | "The withdrawal request no longer exists." is shown | Pending | Pending | Negative — stale request |
| 48 | Attempt withdrawal for a request whose total quantity is no longer valid | "The withdrawal request no longer has valid TOTAL values." is shown | Pending | Pending | Negative — stale request data |
| 49 | Attempt withdrawal where the items actually available no longer match what was verified (for example, someone else removed one first) | "The Holder rows changed before deletion. No STACKER data was removed." is shown; nothing is removed | Pending | Pending | Negative — concurrent modification |

---

## Test Case 5

**Test Name:** Individual Holder Disassociation
**Feature:** An operator can remove a single item from the box it was previously placed in. Removal is a multi-step process — the system checks for an active hold on the item, then performs a "move out" step with the connected tracking system, and only then removes the item's placement and refreshes the rack view.

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Remove an item that is currently placed in a box, has no active hold, and completes the move-out step successfully | Removal succeeds with "Holder disassociated successfully."; the box shows as empty on the rack view | Pending | Pending | Happy path |
| 2 | Remove an item that currently has status 'HOLD' with a hold comment and reason on file | The system retrieves the hold comment and reason, releases the holder, moves it out to RBF2, and re-applies the hold with the saved comment and reason; removal succeeds with confirmation | Pending | Pending | Happy path — held holder disassociation |
| 3 | Attempt removal without being properly logged in | Removal is rejected with "Bearer token is required." or "Invalid or expired token." | Pending | Pending | Negative — missing/invalid session |
| 4 | Attempt to remove an item that no longer exists in the system's item lookup | Removal is rejected with "HolderJob record was not found." or the underlying lookup error | Pending | Pending | Negative — unknown item / lookup outage |
| 5 | Attempt to remove an item where the move-out step with the tracking system fails | Removal is rejected with a message that begins "The SQL assignment was deleted, but..." followed by the move-out error | Pending | Pending | Negative — move-out failure |
| 6 | Attempt to remove an item whose placement record no longer exists, or is no longer in a removable state (after the move-out step succeeded) | Removal is rejected with "The holder was not found or its status is not RELEASE." | Pending | Pending | Negative — status guard on removal |
| 7 | Attempt removal while the warehouse database is temporarily unavailable | Removal fails with a clear error message; the rack view is not left in a broken or partial state | Pending | Pending | Negative — database failure |
| 8 | Confirm what information is returned after a removal | The response confirms success/failure, the message shown to the user, and the updated list of boxes for the rack view | Pending | Pending | Contract check |

---

## Test Case 6

**Test Name:** Job Unship
**Feature:** An operator scans a Shipping Id (a ShipBox holder) to load its child holders, scans each child holder to verify it against the loaded list, then clicks Unship. The system re-scans for the authoritative list, then executes the FEATS transaction sequence: Unship + SuperMove/MoveIn (of the ShipBox itself), BreakupJob, and — for CAM3.4 only — SetHolderStatus('R') and TransferHolderJob to re-home each loaded "Holder-1" onto its base "Holder". CloseHolderJob('CLOSE') runs for both cam versions. The whole FEATS query fan-out (Query(HolderJob) by ParentHolder) is forked across every enabled cam version (3.4 and 7) so the correct endpoint is auto-detected.

### 6.1 Scan Shipping Id

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 1 | Enter a valid Shipping Id that has child holders on CAM3.4 and click LOAD | Child holders are loaded and shown in the table (Holder, Part Num, Grade, Model, Qty); message reads "Loaded N child holder(s) for ShippingId '...'." | Pending | Pending | Happy path — CAM3.4 |
| 2 | Enter a valid Shipping Id that has child holders on CAM7 and click LOAD | Same as #1; the fork correctly finds the match on CAM7 | Pending | Pending | Happy path — CAM7 |
| 3 | Click LOAD with the Shipping Id field left blank | The LOAD button is disabled; no request is sent | Pending | Pending | Negative — client-side guard |
| 4 | Click LOAD while not logged in | "Login token is missing. Please sign in again." is shown; no request is sent | Pending | Pending | Negative — client-side guard |
| 5 | Load with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 6 | Enter a Shipping Id that does not exist on either enabled cam version | "No child holders were found for ShippingId '...'." is shown | Pending | Pending | Negative — not found |
| 7 | Enter a Shipping Id where FEATS returns rows but every row has a blank Holder value | "No child holders were found for ShippingId '...'." is shown (same as #6, not a false success) | Pending | Pending | Negative — edge case, post-filter empty |
| 8 | Load while the FEATS service itself is unavailable/erroring on all enabled cam versions | The underlying FEATS technical error message is shown (not the generic "not found" message) | Pending | Pending | Negative — FEATS outage |
| 9 | Enter a Shipping Id that (incorrectly) has matching child holders on **both** CAM3.4 and CAM7 | CAM3.4 result wins deterministically; a warning is logged server-side; only the CAM3.4 child holders are shown | Pending | Pending | Negative — data inconsistency tie-break |
| 10 | Change the Shipping Id field after a successful load | Previous scan result, verified holders, and any messages are cleared/reset | Pending | Pending | UI reset check |

### 6.2 Holder Verification

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 11 | Scan each child holder shown in the loaded table, one at a time | Each scanned holder is marked "SCANNED" and highlighted; the progress indicator/counter increments (e.g., "2 out of 3 holders scanned.") | Pending | Pending | Happy path |
| 12 | Scan a holder ID that is not part of the loaded child holders list | "Holder not found in the loaded list." is shown; progress does not change | Pending | Pending | Negative — holder not in list |
| 13 | Scan the same (already-verified) holder again | No additional effect; it remains counted once | Pending | Pending | Edge case — duplicate scan |
| 14 | Attempt to click UNSHIP before all loaded child holders have been scanned | The UNSHIP button stays disabled | Pending | Pending | Enablement guard |
| 15 | Scan every loaded child holder successfully | All holders show "SCANNED"; the UNSHIP button becomes enabled | Pending | Pending | Enablement check |

### 6.3 Execute Unship — CAM3.4

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 16 | Click UNSHIP with all holders verified for a Shipping Id resolved to CAM3.4 | Unship, SuperMove, MoveIn, BreakupJob, SetHolderStatus('R'), TransferHolderJob, and CloseHolderJob('CLOSE') all execute in sequence; success message reads "ShippingId '...' was unshipped and returned for re-assignment successfully."; the form resets | Pending | Pending | Happy path |
| 17 | Click UNSHIP with an invalid/expired session | "Invalid or expired token." is shown | Pending | Pending | Negative — invalid session |
| 18 | Trigger Unship with a blank Shipping Id (server-side) | "ShippingId is required." is shown | Pending | Pending | Negative — server-side guard |
| 19 | Click UNSHIP, but the re-scan (performed right before transacting) no longer finds the child holders (e.g., removed by someone else in the meantime) | Unship stops and shows the re-scan's failure message (e.g., "No child holders were found for ShippingId '...'.") | Pending | Pending | Negative — concurrent modification |
| 20 | The FEATS UnShip(ShippingId, 'SHPBOX') call fails | "FEATS Unship failed: {reason}" is shown; no further steps are attempted | Pending | Pending | Negative — step 3 failure |
| 21 | UnShip succeeds but the temporary SuperMove to '735630 FGI' fails | "FEATS SuperMove failed: {reason}" is shown | Pending | Pending | Negative — step 3 failure |
| 22 | UnShip + SuperMove succeed but MoveIn fails | "FEATS MoveIn failed: {reason}" is shown | Pending | Pending | Negative — step 3 failure |
| 23 | Steps 3 succeed but BreakupJob(Holder=ShippingId, Holders=child holders) fails | "FEATS BreakupJob failed: {reason}" is shown | Pending | Pending | Negative — step 4 failure |
| 24 | Steps 3–4 succeed but SetHolderStatus('R') fails for one of the "-1"-stripped base holders | "FEATS SetHolderStatus failed for holder '{baseHolder}': {reason}" is shown | Pending | Pending | Negative — step 5 failure (CAM3.4 only) |
| 25 | Steps 3–5 succeed but TransferHolderJob fails for one loaded/base holder pair | "FEATS TransferHolderJob failed for holder '{loadedHolder}' -> '{baseHolder}': {reason}" is shown | Pending | Pending | Negative — step 6 failure (CAM3.4 only) |
| 26 | Steps 3–6 succeed but the final CloseHolderJob(Holder=ShippingId, Reason='CLOSE') fails | "FEATS CloseHolderJob failed: {reason}" is shown | Pending | Pending | Negative — step 7 failure |

### 6.4 Execute Unship — CAM7

| Step | Action | Expected Result | Actual Result | Pass/Fail | Remarks |
|---|---|---|---|---|---|
| 27 | Click UNSHIP with all holders verified for a Shipping Id resolved to CAM7 | Unship, SuperMove, MoveIn, and BreakupJob execute against the CAM7 endpoint; SetHolderStatus and TransferHolderJob are **skipped entirely**; CloseHolderJob('CLOSE') still executes; success message is shown | Pending | Pending | Happy path — CAM7 (steps 5–6 bypassed) |
| 28 | On CAM7, steps 3–4 (Unship/SuperMove/MoveIn/BreakupJob) succeed but CloseHolderJob fails | "FEATS CloseHolderJob failed: {reason}" is shown, same as the CAM3.4 case | Pending | Pending | Negative — step 7 failure on CAM7 |
| 29 | Confirm no "-1" suffix stripping or base-holder derivation logic runs for a CAM7 Shipping Id | No SetHolderStatus/TransferHolderJob FEATS calls are made for CAM7; verified only via logs/network trace | Pending | Pending | Regression check for the CAM7 skip logic |

---

## Appendix

<Additional Attachment or Evidence>

---

## Customer Survey

* Please mark √ in relevant boxes.

Comment:
