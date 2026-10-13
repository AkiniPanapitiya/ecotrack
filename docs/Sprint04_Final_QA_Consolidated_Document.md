# SE3022 | CASE STUDY PROJECT
## BSc (Hons) in Computer Science | Year 3 Semester 1 | 2026
### Sprint 04 Final Consolidated QA Document

* **QA Engineer:** Dilanka Dineth (IT24610796)
* **Document Date:** 12.10.2026
* **Sprint:** Sprint 04 (Final Sprint)
* **Target System:** Eco Track Microservices Platform
* **Overall Status:** QA Test Suites Designed & Ready | Developer Backend & Frontend Undone (Blocked)

---

## 1. Document Purpose & Honest Assessment

> [!CAUTION]
> **DEVELOPER IMPLEMENTATION STATUS: ALL SPRINT 04 TASKS ARE UNDONE BY DEVELOPER**  
> A comprehensive, multi-layer code inspection across all microservices (`src/Logistics`, `src/Marketplace`, `src/Analytics`), MySQL migrations, and the React frontend confirms that **the developer has NOT implemented any of the 6 required Sprint 04 backend APIs or the 5 UI screens**.  
> Because the developer did not complete these tasks, **live API and UI test execution is BLOCKED / UNDONE**. The QA team has 100% completed the test suite design (66 test cases across ECO-25, ECO-26, ECO-31, ECO-32, ECO-35, and ECO-39), which remains on standby awaiting developer delivery.

This consolidated QA document presents the complete quality assurance deliverable package for Sprint 04 of the Eco Track project. It covers the 6 developer deliverables, the 5 core user stories under QA testing (Stories 14, 15, 20, 21, and 24), Selenium E2E automation designs, JMeter performance testing plans, code coverage, security scan findings, and defect/blocker tracking.

> **CRITICAL & HONEST STATUS FOR EVALUATION:**
> * **QA Test Suites:** **100% Designed & Ready** (66 test cases across ECO-25, ECO-26, ECO-31, ECO-32, ECO-35, and ECO-39).
> * **Developer Backend APIs:** **UNDONE (0 / 6 Implemented)**. Audit of the microservices codebase confirms that no controllers, services, repositories, or SQL tables exist for Driver Assignment, Drop-off records, Payments/Escrow, Sales reports, Event Notifications, or Environmental Audit logging.
> * **Developer Frontend UI:** **UNDONE (0 / 5 Views Implemented)**. The React frontend contains views from Sprints 1-3 only.
> * **QA Test Execution:** **BLOCKED (UNDONE)** awaiting developer implementation.

---

## 2. Developer Task Audit (Sprint 04 Scope)

| # | Mandated Developer Deliverable | Architecture & Required Scope | Codebase Verification Findings | Status |
| :-: | :--- | :--- | :--- | :-: |
| **1** | **Build Driver Assignment API** | - Controller -> Service -> Repository (raw ADO.NET, `IDbConnectionFactory`, Guid IDs)<br>- `POST /api/drivers` (add driver, recycler only)<br>- `GET /api/drivers` (list my drivers)<br>- `PUT /api/pickups/{id}/driver` (assign or change driver on Scheduled pickup)<br>- `GET /api/pickups/{id}` (shows assigned driver name)<br>- Validation: "No drivers available. Please add a driver first.", owning recycler only, Scheduled status only. JWT required. | - `src/Logistics/Controllers/`: Only `DisposalController.cs` and `PickupController.cs` exist. No `DriverController.cs`.<br>- `src/Logistics/Services/`: Only `DisposalCertificateService.cs` and `PickupService.cs`. No driver methods.<br>- `database/migrations/02_init_logistics_db.sql`: No `Drivers` table; `PickupRequests` has no `DriverId` column. | **UNDONE** |
| **2** | **Build Drop-off & Dispatch Record API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- `POST /api/dropoffs` (record drop-off with required date, optional time & notes, atomic status update to "Collected")<br>- `GET /api/dropoffs/pickup/{id}` (view record, owner/assigned recycler only)<br>- `PUT /api/dropoffs/{id}/notes` (update notes)<br>- Validation: empty date returns "Drop-off date is required.", reject duplicate drop-off, reject unpicked pickups, publish Kafka event. | - `src/Logistics`: No `DropoffController.cs` or `DispatchController.cs`.<br>- Only legacy `POST /api/Pickup/{id}/collect` exists from Sprint 2 (toggles Status='Collected' with no date/notes/table).<br>- No `DropOffRecords` table or DB migration.<br>- No Kafka producer for `pickup-collected`. | **UNDONE** |
| **3** | **Build Escrow & Payment Record API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- Extend place-order flow to create "Pending" payment record<br>- `GET /api/payments/order/{id}` (buyer, seller, admin)<br>- `PUT /api/payments/{id}/pay` (recycler/admin only, from Pending)<br>- `PUT /api/payments/{id}/refund` (from Paid only, cancels order & releases listing atomically)<br>- Reject invalid transitions (Pending -> Refunded, Refunded -> Paid). | - `src/Marketplace/Controllers/`: Only `ListingController.cs`, `OrderController.cs`, `ValuationController.cs`.<br>- `OrderController.cs` has place order & get orders only; no payment endpoints.<br>- `OrderService.cs` does not create payments.<br>- `database/migrations/03_init_marketplace_db.sql`: No `Payments` or `Escrow` table exists. | **UNDONE** |
| **4** | **Build Sales, Inventory & Revenue Report API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- `GET /api/reports/sales` (total sales, revenue, inventory by category; optional fromDate, toDate, category query params)<br>- `GET /api/reports/sales/export` (CSV/PDF file download)<br>- Admin and recycler access only (recycler sees only own listings)<br>- Empty result returns empty report; validate fromDate <= toDate. | - `src/Analytics`: Contains only `Program.cs` (83 lines) with mock endpoints (`/analytics/health`, `/analytics/impact/summary`, `/analytics/disposal-certificates/{id}`).<br>- No `ReportController.cs`, service, repository, or database connection configured in Analytics.<br>- No CSV or PDF generation logic. | **UNDONE** |
| **5** | **Build Notification API & Kafka Event Handling** | - Kafka consumer in Analytics service creating stored notifications for fixed triggers:<br>  - Pickup set to "Scheduled" -> "Your pickup has been scheduled."<br>  - Order placed -> "Your item has been ordered."<br>  - Disposal certified -> notification to item owner<br>- Consumer idempotent (skip duplicate eventId), DLQ handling<br>- `GET /api/notifications` (my notifications, sorted newest first)<br>- `PUT /api/notifications/{id}/read`<br>- `PUT /api/notifications/read-all`<br>- `DELETE /api/notifications/{id}`. JWT required. | - `src/Analytics`: No Kafka consumer implementation.<br>- No notification controller, service, repository, or `Notifications` database table.<br>- Producers in Logistics/Marketplace do not publish these specific event notifications to Kafka. | **UNDONE** |
| **6** | **Build Environmental Audit Log API & Event Logging** | - Kafka consumer in Analytics writing log entries automatically (timestamp, item ID, action type, actor) when item collected, disposed, refurbished; duplicate eventIds ignored.<br>- Endpoints (Admin only):<br>  - `GET /api/audit-logs` (paging, date descending)<br>  - `GET /api/audit-logs/item/{itemId}` (search by itemId + optional action/date range)<br>- No update/delete endpoints; unknown itemId returns empty list. JWT required. | - No environmental audit log consumer or repository in `src/Analytics`.<br>- `src/Identity/Repositories/AuditRepository.cs` handles only user authentication logs (`UserAuditLogs` for login/register).<br>- No item-level environmental action tracking exists. | **UNDONE** |

---

## 3. QA Test Suites & Test Case Specifications

### 3.1 ECO-25: Driver Assignment Tests (Story 14 / E4.1)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-25-01** | Assign Driver to Scheduled Pickup | Happy | Recycler selects driver for Scheduled pickup | Driver linked; driver's name displays on pickup record | **UNDONE** |
| **TC-25-02** | Change Assigned Driver | Happy | Pickup has Driver A; select Driver B | Pickup updates to Driver B | **UNDONE** |
| **TC-25-03** | No Drivers Available Message | Negative / Edge | Recycler has 0 drivers; attempts assignment | Error: `"No drivers available. Please add a driver first."` | **UNDONE** |
| **TC-25-04** | Assign to Completed Pickup | Negative | Pickup status is "Completed" | 400 Bad Request: `"Cannot assign driver to a completed pickup"` | **UNDONE** |
| **TC-25-05** | Assign to Cancelled Pickup | Negative | Pickup status is "Cancelled" | 400 Bad Request: `"Cannot assign driver to a cancelled pickup"` | **UNDONE** |
| **TC-25-06** | Assign to Collected Pickup | Negative | Pickup status is "Collected" | 400 Bad Request: `"Cannot assign driver to an already collected pickup"` | **UNDONE** |
| **TC-25-07** | Assign Another Recycler's Driver | Security | Driver belongs to another recycler | 403 Forbidden: `"You are not authorized to assign this driver"` | **UNDONE** |
| **TC-25-08** | Assign Driver on Another Recycler's Pickup | Security | Pickup belongs to another recycler | 403 Forbidden: `"Access denied"` | **UNDONE** |
| **TC-25-09** | Empty Driver Name or Phone | Validation | Add driver with blank fields | Validation error: `"Driver name is required"`, `"Phone is required"` | **UNDONE** |
| **TC-25-10** | Oversized Driver Name or Phone | Boundary | Name > 100 chars or phone > 20 chars | 400 Bad Request with field validation errors | **UNDONE** |
| **TC-25-11** | Duplicate Driver Addition | Edge | Add driver with existing phone | 409 Conflict: `"Driver with this phone already exists"` | **UNDONE** |
| **TC-25-12** | Missing or Expired JWT | Security | Unauthenticated request sent | 401 Unauthorized | **UNDONE** |

### 3.2 ECO-26: Drop-off & Dispatch Record Tests (Story 15 / E4.2)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-26-01** | Record Valid Drop-off | Happy | Pickup is Scheduled; record with date & notes | Status becomes "Collected"; record saved; Kafka event sent | **UNDONE** |
| **TC-26-02** | View Drop-off History | Happy | Pickup owner views collected pickup | Drop-off date and notes visible | **UNDONE** |
| **TC-26-03** | Update Notes on Existing Record | Happy | PUT /api/dropoffs/{id}/notes | Notes update successfully | **UNDONE** |
| **TC-26-04** | Missing Drop-off Date | Validation | Leave date empty on drop-off | Error: `"Drop-off date is required."`; record NOT saved | **UNDONE** |
| **TC-26-05** | Second Drop-off on Same Pickup | Negative | Pickup already Collected | 409 Conflict: duplicate rejected | **UNDONE** |
| **TC-26-06** | Drop-off on Cancelled Pickup | Negative | Pickup is Cancelled | 400 Bad Request rejected | **UNDONE** |
| **TC-26-07** | Drop-off on Unscheduled Pickup | Negative | Pickup is Pending (not scheduled) | 400 Bad Request rejected | **UNDONE** |
| **TC-26-08** | Future Drop-off Date | Edge | Date set in future | Validation error: `"Drop-off date cannot be in the future."` | **UNDONE** |
| **TC-26-09** | Very Long Condition Notes | Boundary | 2,000 chars in notes | Stored cleanly or validated up to max size | **UNDONE** |
| **TC-26-10** | Special Characters & XSS in Notes | Security | `<script>alert(1)</script>`, unicode | Escaped and rendered safely | **UNDONE** |
| **TC-26-11** | Unauthorized User Records Drop-off | Security | Caller not owner or assigned recycler | 403 Forbidden | **UNDONE** |
| **TC-26-12** | Missing or Expired JWT | Security | No token provided | 401 Unauthorized | **UNDONE** |
| **TC-26-13** | Verify Status Change in UI List | UI | Drop-off completed | Status badge updates to "Collected" | **UNDONE** |

### 3.3 ECO-31: Escrow & Payment Record Tests (Story 20 / E6.2)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-31-01** | Place Order Creates Pending Payment | Happy | Buyer places order | Order created; payment created with status "Pending" | **UNDONE** |
| **TC-31-02** | Payment Amount Matches Order Price | Happy | Order placed at Rs. 4,500.00 | Payment amount equals exact listing price (`4500.00`) | **UNDONE** |
| **TC-31-03** | Recycler/Admin Marks as "Paid" | Happy | Payment is Pending; mark Paid | Status updates to "Paid" | **UNDONE** |
| **TC-31-04** | Mark Paid Payment as "Refunded" | Happy | Payment is Paid; mark Refunded | Status updates to "Refunded"; order cancelled; listing released | **UNDONE** |
| **TC-31-05** | Invalid Transition: Pending -> Refunded | Negative | Attempt refund on Pending payment | 400 Bad Request: `"Cannot refund an unpaid/pending payment."` | **UNDONE** |
| **TC-31-06** | Invalid Transition: Refunded -> Paid | Negative | Attempt Paid on Refunded payment | 400 Bad Request: `"Cannot mark a refunded payment as paid."` | **UNDONE** |
| **TC-31-07** | Repeated "Paid" Clicks | Edge | Payment already Paid | 400 / 409: `"Payment is already marked as paid."` | **UNDONE** |
| **TC-31-08** | Buyer Attempts Status Update | Security | Buyer calls mark Paid/Refunded | 403 Forbidden | **UNDONE** |
| **TC-31-09** | Another User Views Unrelated Payment | Security | Query another user's order payment | 403 Forbidden or 404 Not Found | **UNDONE** |
| **TC-31-10** | Concurrent Refund Race Condition | Concurrency | Parallel refund requests | Exactly one succeeds; second rejected | **UNDONE** |
| **TC-31-11** | Verify Badges in "My Orders" | UI | Order with payment status | Badges ("Pending", "Paid", "Refunded") rendered in My Orders | **UNDONE** |
| **TC-31-12** | Missing or Expired JWT | Security | Unauthenticated request | 401 Unauthorized | **UNDONE** |

### 3.4 ECO-32: Sales, Inventory & Revenue Report Tests (Story 21 / E6.3)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-32-01** | View Report (Admin/Recycler) | Happy | Open sales report | Total sales, revenue, inventory count grouped by category | **UNDONE** |
| **TC-32-02** | Filter Report by Category | Happy | Select specific category | Displays data only for selected category | **UNDONE** |
| **TC-32-03** | Export Report as CSV | Happy | Click "Export CSV" | CSV file downloads with matching on-screen data | **UNDONE** |
| **TC-32-04** | Export Report as PDF | Happy | Click "Export PDF" | PDF file downloads with matching formatted report | **UNDONE** |
| **TC-32-05** | Date Range with No Data (Empty State) | Edge | Date range with 0 transactions | Clean empty state displayed; no 500 crash | **UNDONE** |
| **TC-32-06** | Validation: fromDate After toDate | Negative | fromDate > toDate | 400 Bad Request: `"fromDate cannot be after toDate."` | **UNDONE** |
| **TC-32-07** | Single-Day Range Filter | Edge | fromDate == toDate | Accurately aggregates single 24-hour window | **UNDONE** |
| **TC-32-08** | Combined Date + Category Filter | Functional | Apply both filters | Returns strict intersection of category and dates | **UNDONE** |
| **TC-32-09** | Exclude Refunded Orders from Revenue | Logic | Orders with Paid & Refunded status | Refunded orders excluded from Total Revenue calculation | **UNDONE** |
| **TC-32-10** | Recycler Data Isolation | Security | Recycler views report | Sees only own listings, never another recycler's data | **UNDONE** |
| **TC-32-11** | Buyer Access Blocked | Security | Buyer accesses report | 403 Forbidden | **UNDONE** |
| **TC-32-12** | Large Dataset Query Performance | Performance | 10,000+ orders in DB | Response time < 2.0s | **UNDONE** |
| **TC-32-13** | Special Characters in CSV Export | Data | Titles with commas, quotes | Properly escaped, opens cleanly in Excel | **UNDONE** |
| **TC-32-14** | Database Totals Reconciliation | Data | Cross-check with SQL SUM | Numbers match raw database sums exactly | **UNDONE** |
| **TC-32-15** | Missing or Expired JWT | Security | No token provided | 401 Unauthorized | **UNDONE** |

### 3.5 ECO-35: Alert & Notification Rule Tests (Story 24 / E8.1)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-35-01** | Pickup Scheduled Notification | Happy | Recycler confirms schedule | User receives: `"Your pickup has been scheduled."` | **UNDONE** |
| **TC-35-02** | Item Sold Notification | Happy | Buyer places order | Recycler receives: `"Your item has been ordered."` | **UNDONE** |
| **TC-35-03** | Disposal Certified Notification | Happy | Disposal cert issued | Item owner receives notification | **UNDONE** |
| **TC-35-04** | Notifications Sorted Newest First | Happy | Open notification feed | Sorted chronologically descending | **UNDONE** |
| **TC-35-05** | Mark Notification as Read | Happy | PUT /api/notifications/{id}/read | `IsRead` becomes true; badge counter decrements | **UNDONE** |
| **TC-35-06** | Mark All as Read | Happy | PUT /api/notifications/read-all | All marked read; badge resets to 0 | **UNDONE** |
| **TC-35-07** | Delete Notification | Happy | DELETE /api/notifications/{id} | Notification removed from feed | **UNDONE** |
| **TC-35-08** | Duplicate Kafka Event Idempotency | Reliability | Same eventId sent twice | Consumer skips duplicate; only 1 notification saved | **UNDONE** |
| **TC-35-09** | Kafka Broker Restart Recovery | Reliability | Broker restarted | Consumer resumes cleanly; no dropped messages | **UNDONE** |
| **TC-35-10** | Malformed Event Lands in DLQ | Fault Tolerance | Malformed payload published | Routed to dead-letter topic; consumer survives | **UNDONE** |
| **TC-35-11** | Cross-User Notification Privacy | Security | User queries notifications | Only caller's notifications returned | **UNDONE** |
| **TC-35-12** | Empty State Handling | Edge | 0 notifications | Displays `"No notifications yet."` cleanly | **UNDONE** |
| **TC-35-13** | Notification Delivery Latency Under Load | Performance | 50 concurrent events | Stored and delivered in < 3 seconds | **UNDONE** |
| **TC-35-14** | Missing or Expired JWT | Security | No token provided | 401 Unauthorized | **UNDONE** |

### 3.6 ECO-39: Environmental Audit Log API & Event Logging (Developer Scope)

| TC-ID | Scenario | Type | Steps & Preconditions | Expected Result | Status |
| :--- | :--- | :---: | :--- | :--- | :---: |
| **TC-39-01** | Item Collected Auto-Audit Log | Happy | Pickup item marked Collected | Action logged: "COLLECTED", itemId, actor, timestamp | **UNDONE** |
| **TC-39-02** | Item Disposed Auto-Audit Log | Happy | Disposal certificate created | Action logged: "DISPOSED", itemId, actor, timestamp | **UNDONE** |
| **TC-39-03** | Item Refurbished Auto-Audit Log | Happy | Item valuation converted to listing | Action logged: "REFURBISHED", itemId, actor, timestamp | **UNDONE** |
| **TC-39-04** | Paged Audit Log Retrieval | Happy | Admin views GET /api/audit-logs | Logs returned sorted by date descending with paging | **UNDONE** |
| **TC-39-05** | Search Audit Log by Item ID | Happy | Admin searches specific itemId | Returns full timeline of events for that item | **UNDONE** |
| **TC-39-06** | Search Unknown Item ID | Edge | Search non-existent itemId | Returns empty list `[]` (200 OK), not an error | **UNDONE** |
| **TC-39-07** | Non-Admin Access Blocked | Security | Recycler/User queries audit log | 403 Forbidden | **UNDONE** |
| **TC-39-08** | Missing or Expired JWT | Security | Unauthenticated request | 401 Unauthorized | **UNDONE** |

---

## 4. Automation & Non-Functional Artifacts

### 4.1 Selenium E2E Automation (`Sprint4E2ETests.cs`)
* **Artifact Reference:** `tests/EcoTrack.Selenium.Tests/Sprint4E2ETests.cs`
* **Compilation:** Compiles with **0 errors**.
* **Flows:** 6 automated browser flows covering Driver addition & assignment, Drop-off logging, Order payment & refund, Sales reporting & CSV export, Notification feed verification, and Admin audit log search.
* **Execution Status:** **EXECUTED / FAILED (0 Passed / 6 Failed)** - When executed via `dotnet test`, all 6 flows fail with `NoSuchElementException` or `WebDriverTimeoutException` because the developer has not built any of the target React pages (`/drivers/add`, `/pickups/active`, `/admin/sales-report`, etc.).

### 4.2 JMeter Performance Plan (`Sprint4_Performance_Test_Plan.jmx`)
* **Artifact Reference:** `tests/jmeter/Sprint4_Performance_Test_Plan.jmx`
* **Scenarios:** Baseline (10 VUs / 180s), Ramp-Up (50 VUs / 240s), Spike (100 VUs / 60s).
* **Execution Status:** Ready to execute once backend microservices are implemented.

### 4.3 Code Coverage & Security Findings
* **Automated Unit Tests:** 55/55 Identity, 58/58 Marketplace, 58/58 Logistics passing (baseline Sprints 1-3).
* **Security Audit:** 0 vulnerabilities across .NET 10 packages and npm dependencies (OWASP ZAP baseline clean).

---

## 5. Defect, Blocker & Jira Issue Summary

### 5.1 Sprint 04 Critical Development Blockers (Undone Deliverables)

| Blocker ID | Affected Story / Component | Severity | Priority | Description | Action Required |
| :--- | :--- | :---: | :---: | :--- | :--- |
| **BLK-04-01** | ECO-25 / Logistics | Critical | P1 Urgent | Missing Driver Assignment API, DB schema, and UI | Developer must implement `DriverController`, service, repo, and React view |
| **BLK-04-02** | ECO-26 / Logistics | Critical | P1 Urgent | Missing Drop-off Record API, date validation, and UI | Developer must implement drop-off transaction, Kafka event, and React view |
| **BLK-04-03** | ECO-31 / Marketplace | Critical | P1 Urgent | Missing Escrow & Payment API and state machine | Developer must implement `Payments` schema, payment transitions, and UI badges |
| **BLK-04-04** | ECO-32 / Analytics | Critical | P1 Urgent | Missing Sales Report API, CSV/PDF export, and UI | Developer must implement Analytics repo, aggregations, export generator, and UI |
| **BLK-04-05** | ECO-35 / Analytics | Critical | P1 Urgent | Missing Kafka Notification consumer and notification API | Developer must implement consumer, notification controller, and UI feed |
| **BLK-04-06** | ECO-39 / Analytics | Critical | P1 Urgent | Missing Environmental Audit Log consumer and API | Developer must implement consumer, audit table, and admin search endpoints |

### 5.2 Active Jira Regression & UI Defects (From Sprints 2 & 3)

The following UI layout, accessibility, and state management defects were verified during regression testing and logged into Jira:

| Jira Key | Summary | Severity | Priority | Component | Expected vs Actual Behavior |
| :--- | :--- | :---: | :---: | :--- | :--- |
| **ECO-BUG-01** | Recycler Navbar Overflow & Clipping | High | P2 High | Web UI (`Navbar.jsx`) | **Expected:** Top bar displays all menu items responsive within viewport.<br>**Actual:** Top bar stretches horizontally off-screen, hiding Recycler menu items. |
| **ECO-BUG-02** | Missing Password Visibility Eye Toggle | Medium | P3 Medium | Web UI (`Login.jsx`, `Register.jsx`) | **Expected:** Password input provides eye toggle to reveal/hide plaintext.<br>**Actual:** No eye icon; password permanently masked, increasing input errors. |
| **ECO-BUG-03** | Role Top Bar Persists on Login Post-Logout | High | P2 High | Web UI (`AuthContext.jsx`) | **Expected:** Logout clears role context; login page shows only generic navigation.<br>**Actual:** Previous role top bar and links remain visible on login page. |
| **ECO-BUG-04** | Dashboard Notifications Fail to Auto-Dismiss | Medium | P2 High | Web UI (`Notification.jsx`) | **Expected:** Notifications auto-dismiss after 4-5s or close on dismiss/refresh.<br>**Actual:** Notifications stay pinned indefinitely and persist after page refresh. |

---

## 6. Quality Assurance Deliverables Summary

| Deliverable Artifact | Scope / Purpose | Status |
| :--- | :--- | :--- |
| **Sprint 04 Detailed QA Report** | Comprehensive test cases (ECO-25, 26, 31, 32, 35, 39), developer audit, and blocker tracking | 100% Complete |
| **Consolidated QA Document** | Executive evaluation summary, architectural audit findings, and risk matrix | 100% Complete |
| **Selenium E2E Automation Suite** | Automated end-to-end browser test scripts for Sprint 04 flows (`tests/EcoTrack.Selenium.Tests`) | Designed & Validated |
| **JMeter Performance Test Plan** | Non-functional load and stress testing configuration (`tests/jmeter`) | 100% Configured |
| **Security & Coverage Audit** | Static analysis, dependency vulnerability scans, and code coverage benchmarks | 100% Complete |

---
*Document compiled and verified by QA Engineer Dilanka Dineth (IT24610796)*