# WDC Stacker: Implementation Changes

This document lists the changes made to implement the fixes declared in the plan file `wdc-stacker-fixes-34ecf9.md`.

## Changes Summary

| Category | Files | Changes |
|----------|-------|---------|
| **Login Validation** | `LoginPage.tsx` (both PWD & FGI) | Changed input placeholder to `Employee ID` and added validation in `handleSubmit` to reject usernames containing `\` or `@` with appropriate error messages |
| **Login Validation** | `AuthController.cs` | Added server-side validation in `Login` method to reject usernames containing `\` or `@` and return `BadRequest` |
| **Rack Data Loading** | `HomePage.tsx` (both PWD & FGI) | Added mount `useEffect` that calls `getBoxesApi(user.token)` to load initial rack/box data and seed `gridViewBoxes` state, plus required imports |
| **Holder Assignment Check** | `StackerAggregate.cs` | Moved `HolderAssignExistsAsync` check from `AssignHolderAsync` to `ScanHolderJobAsync` (step 4), returns `Success = false, CanAssign = false, Message = "Holder is already assigned."` |
| **Job Information in SQL Insert** | `StackerSqlService.cs` | Added `JOB` field to `HOLDER_ASSIGN` table INSERT statement in `InsertHolderAssignAsync` method (line 170, 172, 183) to store job information with holder assignments |
| **Session Timeout Modal** | `SessionExpiredModal.tsx` (both PWD & FGI) | Created new modal component displaying session expiry message with "Go to Login" button that calls `logout()` and navigates to `/login` |
| **Session Timeout Context** | `AuthContext.tsx` (both PWD & FGI) | Added `sessionExpired` state, `expiryTimerRef`, `clearExpiryTimer()`, `scheduleExpiry()` functions, mount `useEffect` for JWT expiry scheduling, event listener for `SESSION_EXPIRED_EVENT`, and modified `login`/`logout` to manage timer |
| **Session Timeout Events** | `sessionEvents.ts` (both PWD & FGI) | Created new file with `SESSION_EXPIRED_EVENT` constant and `notifySessionExpired()` function |
| **401 Handling** | `stackerApi.ts` (both PWD & FGI) | Added 401 status check in all API functions (`getBoxesApi`, `scanApi`, `assignApi`, `getBoxAssignmentsApi`, `disassociateHolderApi`) that calls `notifySessionExpired()`; FGI also includes `getKittingRequestsApi` and `acknowledgeKittingRequestApi` |
| **Session Modal Integration** | `App.tsx` (both PWD & FGI) | Added `<SessionExpiredModal />` at root level inside `AuthProvider` |
| **Box Count Query** | `StackerSqlService.cs` | Rewrote `GetBoxListCountAndPercentageAsync` with CTE approach: added `HolderCounts` CTE to pre-aggregate counts per `BOXNAME`, added `DistinctBoxes` CTE with `SELECT DISTINCT` to handle duplicates, modified final query to left join for accurate counts |
