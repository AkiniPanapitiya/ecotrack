# SE3022 | CASE STUDY PROJECT
## BSc (Hons) in Computer Science | Year 3 Semester 1 | 2026
### Sprint 04 Quality Assurance Test Report

* **QA Engineer:** Dilanka Dineth (IT24610796)
* **Sprint:** Sprint 04 (Final Sprint)
* **Report Date:** 12.10.2026
* **Target Release:** Eco Track v1.0
* **Overall Sprint QA Verdict:** QA Test Suite 100% Designed | Backend Development 0% Complete (UNDONE) | Frontend Development 0% Complete (UNDONE) | QA Execution BLOCKED

---

## 1. Executive Summary

> [!CAUTION]
> **DEVELOPER IMPLEMENTATION STATUS: ALL SPRINT 04 TASKS ARE UNDONE BY DEVELOPER**  
> A comprehensive, multi-layer code inspection across all microservices (`src/Logistics`, `src/Marketplace`, `src/Analytics`), MySQL migrations, and the React frontend confirms that **the developer has NOT implemented any of the 6 required Sprint 04 backend APIs or the 5 UI screens**.  
> Because the developer did not complete these tasks, **live API and UI test execution is BLOCKED / UNDONE**. The QA team has 100% completed the test suite design (66 test cases across ECO-25, ECO-26, ECO-31, ECO-32, ECO-35, and ECO-39), which remains on standby awaiting developer delivery.

This report documents the quality assurance evaluation, test design, and codebase verification for **Sprint 04** of the Eco Track platform. Sprint 04 encompasses critical end-stage capabilities across Logistics, Marketplace, and Analytics microservices, covering:
1. **E4.1 - Driver Assignment (Story 14 / ECO-25)**
2. **E4.2 - Drop-off & Dispatch Record (Story 15 / ECO-26)**
3. **E6.2 - Escrow & Payment Record (Story 20 / ECO-31)**
4. **E6.3 - Sales, Inventory & Revenue Report (Story 21 / ECO-32)**
5. **E8.1 - Alert & Notification Rule (Story 24 / ECO-35)**
6. **Environmental Audit Log API & Event Logging (Developer Scope / ECO-39)**

### 1.1 Codebase Audit & Honest Assessment

A full, multi-layered code audit was conducted across the EcoTrack microservices codebase, reviewing all service components (`Logistics`, `Marketplace`, `Analytics`, `Identity`), database migrations (`database/migrations/`), and frontend views (`web/src/views/`).

**Audit Finding:**
* **Developer Backend Implementation:** **UNDONE (0 / 6 APIs Implemented)**. The developer has not implemented any of the 6 required Sprint 04 APIs (no controllers, services, repositories, raw ADO.NET data access, or schema tables exist for Driver Assignment, Drop-off Records, Escrow/Payments, Sales Reporting, Event Notifications, or Environmental Audit Logging).
* **Developer Frontend Implementation:** **UNDONE (0 / 5 UI Screens Implemented)**. The React frontend contains views from Sprints 1-3 only. No pages, components, form inputs, or routes exist for Drivers, Drop-off history, Payment statuses/refund actions, Sales reports, or User notifications.
* **QA Test Suite Readiness:** **100% COMPLETE & READY**. 66 comprehensive test cases (Happy path, Negative, Boundary, Edge, Security/JWT, and Concurrency) have been authored against the exact acceptance criteria.
* **QA Test Execution Status:** **BLOCKED (UNDONE)**. Execution cannot proceed until the developer builds and deploys the backend endpoints and frontend components.

---

### 1.2 Summary of Sprint Stories & Verification Status

| Story Code | User Story Title | Assigned Service | Test Cases Designed | Developer Backend Status | Developer Frontend Status | QA Execution Status |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| **ECO-25 (Story 14)** | E4.1 - Driver Assignment | Logistics (`:5002`) | 12 (TC-25-01 - TC-25-12) | **UNDONE** (No API/Repo/DB) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **ECO-26 (Story 15)** | E4.2 - Drop-off & Dispatch Record | Logistics (`:5002`) | 13 (TC-26-01 - TC-26-13) | **UNDONE** (No Drop-off API/DB) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **ECO-31 (Story 20)** | E6.2 - Escrow & Payment Record | Marketplace (`:5003`) | 12 (TC-31-01 - TC-31-12) | **UNDONE** (No Payments API/DB) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **ECO-32 (Story 21)** | E6.3 - Sales, Inventory & Revenue Report | Analytics (`:5004`) | 15 (TC-32-01 - TC-32-15) | **UNDONE** (Stub Program.cs only) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **ECO-35 (Story 24)** | E8.1 - Alert & Notification Rule | Analytics (`:5004`) | 14 (TC-35-01 - TC-35-14) | **UNDONE** (No Consumer/API) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **Dev Scope** | Environmental Audit Log API & Event Logging | Analytics (`:5004`) | 8 (TC-39-01 - TC-39-08) | **UNDONE** (No Consumer/API) | **UNDONE** (No UI View) | **BLOCKED / UNDONE** |
| **Regression** | Sprint 01-03 Carried Forward Flows | Identity / Logistics / Market | 13 (TC-S2-01 - TC-S2-13) | Implemented (Sprints 1-3) | Implemented (Sprints 1-3) | **PASSED (13/13)** |

---

## 2. Developer Task Audit (Codebase Deep Dive)

The following matrix documents the exact technical verification status of the 6 developer deliverables mandated for Sprint 04 based on the repository audit:

| # | Mandated Developer Deliverable | Architecture & Required Endpoints | Codebase Audit Finding | Status |
| :-: | :--- | :--- | :--- | :-: |
| **1** | **Build Driver Assignment API** | - Controller -> Service -> Repository (raw ADO.NET, `IDbConnectionFactory`, Guid IDs)<br>- `POST /api/drivers` (add driver, recycler only)<br>- `GET /api/drivers` (list my drivers)<br>- `PUT /api/pickups/{id}/driver` (assign/change driver on Scheduled pickup)<br>- `GET /api/pickups/{id}` (shows assigned driver name)<br>- Validation: "No drivers available. Please add a driver first.", only owning recycler, only while Scheduled. JWT required. | - `src/Logistics/Controllers/`: Only `DisposalController.cs` and `PickupController.cs` exist. No `DriverController.cs`.<br>- `src/Logistics/Services/`: Only `DisposalCertificateService.cs` and `PickupService.cs`. No driver methods.<br>- `src/Logistics/Repositories/`: No driver queries.<br>- `database/migrations/02_init_logistics_db.sql`: No `Drivers` table. `PickupRequests` has no `DriverId` column. | **UNDONE** |
| **2** | **Build Drop-off & Dispatch Record API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- `POST /api/dropoffs` (record drop-off with required date, optional time & notes, atomic status update to "Collected")<br>- `GET /api/dropoffs/pickup/{id}` (view record, owner/assigned recycler only)<br>- `PUT /api/dropoffs/{id}/notes` (update notes)<br>- Validation: empty date returns "Drop-off date is required.", reject duplicate drop-off, reject unpicked pickups, Kafka event published. | - `src/Logistics`: No `DropoffController.cs` or `DispatchController.cs`.<br>- Only legacy `POST /api/Pickup/{id}/collect` exists from Sprint 2 (toggles Status='Collected' with no date/notes/table).<br>- No `DropOffRecords` table or DB migration.<br>- No Kafka producer for `pickup-collected`. | **UNDONE** |
| **3** | **Build Escrow & Payment Record API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- Extend place-order flow to create "Pending" payment record<br>- `GET /api/payments/order/{id}` (buyer, seller, admin)<br>- `PUT /api/payments/{id}/pay` (recycler/admin only, from Pending)<br>- `PUT /api/payments/{id}/refund` (from Paid only, cancels order & releases listing atomically)<br>- Reject invalid transitions (Pending -> Refunded, Refunded -> Paid). | - `src/Marketplace/Controllers/`: Only `ListingController.cs`, `OrderController.cs`, `ValuationController.cs`.<br>- `OrderController.cs` has place order & get orders only; no payment endpoints.<br>- `OrderService.cs` does not create payments.<br>- `database/migrations/03_init_marketplace_db.sql`: No `Payments` or `Escrow` table exists. | **UNDONE** |
| **4** | **Build Sales, Inventory & Revenue Report API** | - Controller -> Service -> Repository (raw ADO.NET)<br>- `GET /api/reports/sales` (total sales, revenue, inventory by category; optional fromDate, toDate, category query params)<br>- `GET /api/reports/sales/export` (CSV/PDF file download)<br>- Admin and recycler access only (recycler sees only own listings)<br>- Empty result returns empty report; validate fromDate <= toDate. | - `src/Analytics`: Contains only `Program.cs` (83 lines) with mock endpoints (`/analytics/health`, `/analytics/impact/summary`, `/analytics/disposal-certificates/{id}`).<br>- No `ReportController.cs`, service, repository, or database connection configured in Analytics.<br>- No CSV or PDF generation logic. | **UNDONE** |
| **5** | **Build Notification API & Kafka Event Handling** | - Kafka consumer in Analytics service creating stored notifications for fixed triggers:<br>  - Pickup set to "Scheduled" -> "Your pickup has been scheduled."<br>  - Order placed -> "Your item has been ordered."<br>  - Disposal certified -> notification to item owner<br>- Consumer idempotent (skip duplicate eventId), DLQ handling<br>- `GET /api/notifications` (my notifications, sorted newest first)<br>- `PUT /api/notifications/{id}/read`<br>- `PUT /api/notifications/read-all`<br>- `DELETE /api/notifications/{id}`. JWT required. | - `src/Analytics`: No Kafka consumer implementation.<br>- No notification controller, service, repository, or `Notifications` database table.<br>- Producers in Logistics/Marketplace do not publish these specific event notifications to Kafka. | **UNDONE** |
| **6** | **Build Environmental Audit Log API & Event Logging** | - Kafka consumer in Analytics writing log entries automatically (timestamp, item ID, action type, actor) when item collected, disposed, refurbished; duplicate eventIds ignored.<br>- Endpoints (Admin only):<br>  - `GET /api/audit-logs` (paging, date descending)<br>  - `GET /api/audit-logs/item/{itemId}` (search by itemId + optional action/date range)<br>- No update/delete endpoints; unknown itemId returns empty list. JWT required. | - No environmental audit log consumer or repository in `src/Analytics`.<br>- `src/Identity/Repositories/AuditRepository.cs` handles only user authentication logs (`UserAuditLogs` for login/register).<br>- No item-level environmental action tracking exists. | **UNDONE** |

---

## 3. QA Test Scope & Strategy

### 3.1 Test Objectives
1. Validate all business values and functional acceptance criteria for Stories 14, 15, 20, 21, and 24.
2. Ensure strict error handling for missing fields, invalid state transitions, and unauthorized role attempts.
3. Verify distributed event integrity, DLQ fault tolerance, and idempotency across microservices.
4. Provide structured bug logs with severity/priority classifications to enable developer remediation.

### 3.2 Environment Specifications

| Attribute | Specification | Current Availability |
| :--- | :--- | :--- |
| **Operating System** | Windows 11 / Windows Server | Available |
| **Backend Runtime** | .NET 10 (Kestrel HTTP Services) | Active |
| **Microservice Ports** | Identity (`:5001`), Logistics (`:5002`), Marketplace (`:5003`), Analytics (`:5004`) | Services running S1-S3 baseline |
| **Frontend Runtime** | React 18 + Vite (`http://localhost:5173`) | Active (S1-S3 Views Only) |
| **Database** | MySQL 8.0 (XAMPP / Docker) | Active (`ecotrack_*` databases) |
| **Event Broker** | Apache Kafka + Zookeeper (`localhost:9092`) | Active |
| **Test Assets** | `EcoTrack.Selenium.Tests/`, `tests/jmeter/` | Designed & Compiled |

---

## 4. Test Suite 1: E4.1 - Driver Assignment (Story 14 / ECO-25)

### 4.1 Story Details & Acceptance Criteria
* **Business Value:** Connects a scheduled pickup to an actual driver; ensures accountability for collections; enables recyclers to track operational assignments.
* **Definition of Done:** Driver name/ID can be assigned to a scheduled pickup; recycler can view assigned driver; driver assignment can be modified.
* **Acceptance Criteria:**
  * *Scenario 1 (Assign a Driver):* Given a pickup is "Scheduled", when the recycler selects a driver and assigns them, then the pickup is linked to that driver and the driver's name shows on the pickup record.
  * *Scenario 2 (Change Driver):* Given a pickup already has a driver assigned, when the recycler selects a different driver, then the pickup updates to the new driver.
  * *Scenario 3 (No Driver Available):* Given no drivers are added yet, when the recycler tries to assign one, then they see the message `"No drivers available. Please add a driver first."`

### 4.2 Detailed Test Cases (TC-25-XX)

| Test Case ID | Test Scenario | Type | Pre-condition | Test Action / Steps | Expected Result | Actual Status | Evidence / Screenshot |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **TC-25-01** | Assign Driver to Scheduled Pickup (Happy Path) | Happy Path | Recycler logged in; driver added; pickup is "Scheduled" | 1. Recycler selects driver from dropdown<br>2. Clicks "Assign Driver"<br>3. Views pickup record | Pickup linked to driver; status 200 OK; driver name displays on pickup record in UI | **UNDONE** (API & UI missing) | TC-25-01_Pending_Dev.png |
| **TC-25-02** | Change Assigned Driver (Happy Path) | Happy Path | Pickup has Driver A assigned | 1. Select Driver B from list<br>2. Confirm driver reassignment<br>3. Refresh pickup record | Pickup record updates to Driver B; audit timestamp updated | **UNDONE** (API & UI missing) | TC-25-02_Pending_Dev.png |
| **TC-25-03** | Assign with No Drivers Added | Negative / Edge | Recycler account has 0 drivers configured | 1. Open driver assignment modal on Scheduled pickup | Displays error/warning: `"No drivers available. Please add a driver first."`; assignment dropdown disabled | **UNDONE** (API & UI missing) | TC-25-03_Pending_Dev.png |
| **TC-25-04** | Assign Driver to Completed Pickup | Negative | Pickup status is "Completed" | 1. Send PUT request to assign driver to completed pickup | 400 Bad Request / 409 Conflict: `"Cannot assign driver to a completed pickup"` | **UNDONE** (API & UI missing) | TC-25-04_Pending_Dev.png |
| **TC-25-05** | Assign Driver to Cancelled Pickup | Negative | Pickup status is "Cancelled" | 1. Send PUT request to assign driver to cancelled pickup | 400 Bad Request: `"Cannot assign driver to a cancelled pickup"` | **UNDONE** (API & UI missing) | TC-25-05_Pending_Dev.png |
| **TC-25-06** | Assign Driver to Collected Pickup | Negative | Pickup status is "Collected" | 1. Attempt to change or assign driver after collection | 400 Bad Request: `"Cannot assign driver to an already collected pickup"` | **UNDONE** (API & UI missing) | TC-25-06_Pending_Dev.png |
| **TC-25-07** | Assign Another Recycler's Driver | Security | Driver belongs to Recycler B; caller is Recycler A | 1. Submit PUT with DriverId belonging to another recycler | 403 Forbidden: `"You are not authorized to assign this driver"` | **UNDONE** (API & UI missing) | TC-25-07_Pending_Dev.png |
| **TC-25-08** | Assign Driver to Another Recycler's Pickup | Security | Pickup belongs to Recycler B; caller is Recycler A | 1. Submit PUT with PickupId assigned to another recycler | 403 Forbidden: `"Access denied. Not the assigned recycler for this pickup"` | **UNDONE** (API & UI missing) | TC-25-08_Pending_Dev.png |
| **TC-25-09** | Add Driver with Empty Name or Phone | Boundary / Validation | Recycler on Add Driver screen | 1. Submit empty name or empty phone number | Client & server validation error: `"Driver name is required"`, `"Phone number is required"` | **UNDONE** (API & UI missing) | TC-25-09_Pending_Dev.png |
| **TC-25-10** | Add Driver with Oversized Name/Phone | Boundary | Recycler on Add Driver screen | 1. Enter name > 100 chars or phone > 20 chars | 400 Bad Request with field validation failure | **UNDONE** (API & UI missing) | TC-25-10_Pending_Dev.png |
| **TC-25-11** | Add Duplicate Driver | Edge | Driver "John Doe" / 0771234567 already added | 1. Submit identical driver name and phone | 409 Conflict or validation rejection: `"Driver with this phone already exists"` | **UNDONE** (API & UI missing) | TC-25-11_Pending_Dev.png |
| **TC-25-12** | Missing or Expired JWT Authentication | Security | No bearer token or expired token | 1. Call GET /api/drivers or PUT driver assignment | 401 Unauthorized returned immediately | **UNDONE** (API & UI missing) | TC-25-12_Pending_Dev.png |

---

## 5. Test Suite 2: E4.2 - Drop-off & Dispatch Record (Story 15 / ECO-26)

### 5.1 Story Details & Acceptance Criteria
* **Business Value:** Confirms item collection and physical drop-off; creates an auditable tracking record; marks handoff in e-waste lifecycle.
* **Definition of Done:** Driver/recycler can record drop-off with date, time, and item condition notes; pickup status updates to "Collected"; drop-off record linked to original pickup request.
* **Acceptance Criteria:**
  * *Scenario 1 (Record a Drop-off):* Given a driver has collected an item, when they record the drop-off with date and notes, then the pickup status changes to "Collected" and the record is saved.
  * *Scenario 2 (Missing Drop-off Details):* Given a driver tries to record a drop-off, when they leave the date empty, then they see an error `"Drop-off date is required."` and the record is not saved.
  * *Scenario 3 (View Drop-off History):* Given a pickup has been marked "Collected", when the user views their pickup, then they see the drop-off date and notes.

### 5.2 Detailed Test Cases (TC-26-XX)

| Test Case ID | Test Scenario | Type | Pre-condition | Test Action / Steps | Expected Result | Actual Status | Evidence / Screenshot |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **TC-26-01** | Record Drop-off with Valid Date & Notes | Happy Path | Pickup is in "Scheduled" status | 1. Driver/recycler enters valid date, time, notes<br>2. Submits drop-off form | Pickup status updates to "Collected" atomically; record saved; 201 Created; Kafka event published | **UNDONE** (API & UI missing) | TC-26-01_Pending_Dev.png |
| **TC-26-02** | Pickup Owner Views Drop-off Record | Happy Path | Pickup is "Collected" with recorded drop-off | 1. Requester opens My Pickups<br>2. Expands details | Drop-off date, time, and condition notes clearly displayed | **UNDONE** (API & UI missing) | TC-26-02_Pending_Dev.png |
| **TC-26-03** | Update Notes on Existing Drop-off Record | Happy Path | Drop-off record exists | 1. Send PUT /api/dropoffs/{id}/notes with updated notes | Notes update successfully; updated timestamp saved | **UNDONE** (API & UI missing) | TC-26-03_Pending_Dev.png |
| **TC-26-04** | Record Drop-off with Empty Date | Negative / Validation | Drop-off form open | 1. Leave date blank, submit form | Error displayed: `"Drop-off date is required."`; record NOT saved; status unchanged | **UNDONE** (API & UI missing) | TC-26-04_Pending_Dev.png |
| **TC-26-05** | Second Drop-off on Same Pickup | Negative / Edge | Pickup already marked "Collected" | 1. Attempt to post second drop-off for same pickup | 409 Conflict: `"Pickup has already been marked as collected."`; second record rejected | **UNDONE** (API & UI missing) | TC-26-05_Pending_Dev.png |
| **TC-26-06** | Drop-off on Cancelled Pickup | Negative | Pickup is "Cancelled" | 1. Call POST drop-off on cancelled pickup ID | 400 Bad Request: `"Cannot record drop-off for a cancelled pickup."` | **UNDONE** (API & UI missing) | TC-26-06_Pending_Dev.png |
| **TC-26-07** | Drop-off on Unscheduled / Pending Pickup | Negative | Pickup is still "Pending" (not yet scheduled) | 1. Call POST drop-off on pending pickup ID | 400 Bad Request: `"Only scheduled pickups can be collected."` | **UNDONE** (API & UI missing) | TC-26-07_Pending_Dev.png |
| **TC-26-08** | Drop-off with Future Date | Boundary / Edge | Driver on drop-off screen | 1. Enter tomorrow's date for drop-off | Validation error: `"Drop-off date cannot be in the future."` | **UNDONE** (API & UI missing) | TC-26-08_Pending_Dev.png |
| **TC-26-09** | Very Long Condition Notes | Boundary | Drop-off notes input | 1. Enter 2,000 characters in notes field | Accepted up to DB limit or validated cleanly without 500 error | **UNDONE** (API & UI missing) | TC-26-09_Pending_Dev.png |
| **TC-26-10** | Special Characters & XSS in Notes | Security | Drop-off notes input | 1. Input: `<script>alert(1)</script>`, unicode, commas | Content safely HTML/SQL escaped; rendered as literal text | **UNDONE** (API & UI missing) | TC-26-10_Pending_Dev.png |
| **TC-26-11** | Unauthorized User Records Drop-off | Security | Caller is not owner or assigned recycler | 1. Call POST drop-off with unauthorized token | 403 Forbidden returned | **UNDONE** (API & UI missing) | TC-26-11_Pending_Dev.png |
| **TC-26-12** | Missing or Expired JWT Authentication | Security | Missing bearer token | 1. Call drop-off endpoints with no token | 401 Unauthorized | **UNDONE** (API & UI missing) | TC-26-12_Pending_Dev.png |
| **TC-26-13** | Verify Status Change in Recycler Pickup List | UI Integration | Drop-off completed successfully | 1. Navigate to Active Pickups list | Status badge transitions to "Collected" without full page reload | **UNDONE** (API & UI missing) | TC-26-13_Pending_Dev.png |

---

## 6. Test Suite 3: E6.2 - Escrow & Payment Record (Story 20 / ECO-31)

### 6.1 Story Details & Acceptance Criteria
* **Business Value:** Keeps transparent payment status for every transaction; protects buyers and sellers; feeds actual revenue data into sales reporting.
* **Definition of Done:** Every order has a linked payment record; status: Pending, Paid, or Refunded; record-only (no external payment gateway).
* **Acceptance Criteria:**
  * *Scenario 1 (Create Payment Record):* Given an order is placed, when the system creates the order, then a payment record is created with status "Pending".
  * *Scenario 2 (Mark as Paid):* Given a payment record is "Pending", when the recycler/admin marks it as "Paid", then the status updates to "Paid".
  * *Scenario 3 (Refund a Payment):* Given a payment record is "Paid", when it is marked as "Refunded", then the status updates to "Refunded" and the order is cancelled.

### 6.2 Detailed Test Cases (TC-31-XX)

| Test Case ID | Test Scenario | Type | Pre-condition | Test Action / Steps | Expected Result | Actual Status | Evidence / Screenshot |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **TC-31-01** | Order Creation Creates Pending Payment | Happy Path | Buyer places order on Available listing | 1. Buyer calls POST /api/orders<br>2. System commits transaction | Order created; linked Payment created with status "Pending" and matching OrderId | **UNDONE** (API & UI missing) | TC-31-01_Pending_Dev.png |
| **TC-31-02** | Payment Amount Matches Order Price Exactly | Happy Path / Data | Listing priced at Rs. 4,500.00 | 1. Fetch created payment record | `Payment.Amount` equals exact listing price (`4500.00`) | **UNDONE** (API & UI missing) | TC-31-02_Pending_Dev.png |
| **TC-31-03** | Recycler/Admin Marks Payment "Paid" | Happy Path | Payment is "Pending" | 1. Recycler calls PUT /api/payments/{id}/pay | Status updates to "Paid"; updated timestamp recorded | **UNDONE** (API & UI missing) | TC-31-03_Pending_Dev.png |
| **TC-31-04** | Mark "Paid" Payment as "Refunded" | Happy Path | Payment is "Paid" | 1. Recycler/admin calls PUT /api/payments/{id}/refund | Status becomes "Refunded"; order cancelled; listing released to "Available" atomically | **UNDONE** (API & UI missing) | TC-31-04_Pending_Dev.png |
| **TC-31-05** | Invalid Transition: Pending -> Refunded | Negative / State | Payment is "Pending" | 1. Attempt to refund directly without paying | 400 Bad Request: `"Cannot refund an unpaid/pending payment."` | **UNDONE** (API & UI missing) | TC-31-05_Pending_Dev.png |
| **TC-31-06** | Invalid Transition: Refunded -> Paid | Negative / State | Payment is "Refunded" | 1. Attempt to mark refunded payment as Paid | 400 Bad Request: `"Cannot mark a refunded payment as paid."` | **UNDONE** (API & UI missing) | TC-31-06_Pending_Dev.png |
| **TC-31-07** | Repeated "Paid" Clicks (Idempotency) | Edge / Idempotency | Payment already "Paid" | 1. Send second PUT mark Paid request | 400 Bad Request / 409 Conflict: `"Payment is already marked as paid."` | **UNDONE** (API & UI missing) | TC-31-07_Pending_Dev.png |
| **TC-31-08** | Buyer Attempts to Mark Paid or Refund | Security | Caller has role "User" (Buyer) | 1. Buyer sends PUT mark Paid or PUT refund | 403 Forbidden: `"Only recyclers or administrators can update payment status."` | **UNDONE** (API & UI missing) | TC-31-08_Pending_Dev.png |
| **TC-31-09** | Another User Views Unrelated Payment | Security | User B queries Order A's payment | 1. Call GET /api/payments/order/{unownedId} | 403 Forbidden or 404 Not Found (payment details hidden) | **UNDONE** (API & UI missing) | TC-31-09_Pending_Dev.png |
| **TC-31-10** | Concurrent Refund Requests (Race Condition) | Concurrency | Payment is "Paid" | 1. Fire 2 simultaneous refund calls in parallel threads | Only one succeeds; second transaction fails/rolls back cleanly | **UNDONE** (API & UI missing) | TC-31-10_Pending_Dev.png |
| **TC-31-11** | Verify Status Badges in "My Orders" | UI Integration | Orders with Pending, Paid, Refunded payments | 1. Navigate to My Orders page | Correct visual badges ("Pending", "Paid", "Refunded") rendered per order | **UNDONE** (API & UI missing) | TC-31-11_Pending_Dev.png |
| **TC-31-12** | Missing or Expired JWT Authentication | Security | No token provided | 1. Call GET/PUT payment endpoints without JWT | 401 Unauthorized | **UNDONE** (API & UI missing) | TC-31-12_Pending_Dev.png |

---

## 7. Test Suite 4: E6.3 - Sales, Inventory & Revenue Report (Story 21 / ECO-32)

### 7.1 Story Details & Acceptance Criteria
* **Business Value:** Provides overall performance analytics; identifies top-selling e-waste categories; supports business and environmental strategy.
* **Definition of Done:** Report shows total sales, revenue, and inventory count by category; filterable by date range and category; exportable as PDF or CSV.
* **Acceptance Criteria:**
  * *Scenario 1 (View Report):* Given I am logged in as admin/recycler, when I open the sales report, then I see total sales, revenue, and inventory by category.
  * *Scenario 2 (Filter by Category):* Given I am viewing the report, when I filter by a specific category, then I only see data for that category.
  * *Scenario 3 (Export Report):* Given I am viewing the report, when I click "Export", then a PDF or CSV file downloads with the report data.

### 7.2 Detailed Test Cases (TC-32-XX)

| Test Case ID | Test Scenario | Type | Pre-condition | Test Action / Steps | Expected Result | Actual Status | Evidence / Screenshot |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **TC-32-01** | View Sales Report (Admin/Recycler) | Happy Path | Logged in as Admin or Recycler | 1. Open Sales Report screen | Displays Total Sales, Total Revenue, and Inventory Count aggregated by Category | **UNDONE** (API & UI missing) | TC-32-01_Pending_Dev.png |
| **TC-32-02** | Filter Report by Category | Happy Path | Multiple categories exist | 1. Select "Monitors" from category filter<br>2. Click Filter | Report dynamically displays rows only for "Monitors" | **UNDONE** (API & UI missing) | TC-32-02_Pending_Dev.png |
| **TC-32-03** | Export Report as CSV | Happy Path | Report contains filtered data | 1. Click "Export CSV" | CSV file downloads; columns and summary totals match on-screen figures | **UNDONE** (API & UI missing) | TC-32-03_Pending_Dev.png |
| **TC-32-04** | Export Report as PDF | Happy Path | Report contains filtered data | 1. Click "Export PDF" | PDF file downloads; formatting, headers, and values match report view | **UNDONE** (API & UI missing) | TC-32-04_Pending_Dev.png |
| **TC-32-05** | Date Range with No Data (Empty State) | Edge / UI | Selected date range in past with 0 sales | 1. Select date range with no transactions | Graceful empty state shown (e.g. "No sales found for this period"); 0 totals, no crash | **UNDONE** (API & UI missing) | TC-32-05_Pending_Dev.png |
| **TC-32-06** | Validation: fromDate After toDate | Negative / Validation | Date filter inputs | 1. Set fromDate = 2026-10-15, toDate = 2026-10-01 | 400 Bad Request: `"fromDate cannot be after toDate."`; UI shows validation highlight | **UNDONE** (API & UI missing) | TC-32-06_Pending_Dev.png |
| **TC-32-07** | Single-Day Range Filter | Edge | Single date selected | 1. Set fromDate = toDate = 2026-10-12 | Aggregates only transactions occurring within that 24-hour period | **UNDONE** (API & UI missing) | TC-32-07_Pending_Dev.png |
| **TC-32-08** | Combined Date + Category Filter | Functional | Date range and category chosen | 1. Set Date Range AND Category = "Batteries" | Returns strict intersection of selected category and date window | **UNDONE** (API & UI missing) | TC-32-08_Pending_Dev.png |
| **TC-32-09** | Exclude Refunded Orders from Revenue | Business Logic | Orders exist with Paid and Refunded statuses | 1. Compare report revenue with total orders | Refunded orders are deducted/excluded from Total Revenue sum | **UNDONE** (API & UI missing) | TC-32-09_Pending_Dev.png |
| **TC-32-10** | Recycler Data Isolation | Security / Isolation | Two distinct recyclers with listings | 1. Log in as Recycler A<br>2. Generate sales report | Recycler A sees only their own listings' sales/revenue, never Recycler B's data | **UNDONE** (API & UI missing) | TC-32-10_Pending_Dev.png |
| **TC-32-11** | Buyer Access to Sales Report Blocked | Security | Logged in as regular User (Buyer) | 1. Attempt to navigate to report URL or call API | 403 Forbidden returned | **UNDONE** (API & UI missing) | TC-32-11_Pending_Dev.png |
| **TC-32-12** | Large Dataset Query Performance | Performance | 10,000+ historical orders in database | 1. Query full year report | Report response time < 2.0s; no gateway timeout | **UNDONE** (API & UI missing) | TC-32-12_Pending_Dev.png |
| **TC-32-13** | Special Characters in CSV Export | Data Integrity | Product titles with commas, quotes, ampersands | 1. Export CSV with complex titles | CSV properly quotes fields; opens cleanly in Excel/LibreOffice without column shifting | **UNDONE** (API & UI missing) | TC-32-13_Pending_Dev.png |
| **TC-32-14** | Database Totals Reconciliation | Data Integrity | Database orders and listings table | 1. Compare UI totals against SQL `SUM(PriceAtPurchase)` | On-screen totals match exact database calculation | **UNDONE** (API & UI missing) | TC-32-14_Pending_Dev.png |
| **TC-32-15** | Missing or Expired JWT Authentication | Security | Missing bearer token | 1. Send GET request to report or export endpoint | 401 Unauthorized | **UNDONE** (API & UI missing) | TC-32-15_Pending_Dev.png |

---

## 8. Test Suite 5: E8.1 - Alert & Notification Rule (Story 24 / ECO-35)

### 8.1 Story Details & Acceptance Criteria
* **Business Value:** Keeps stakeholders informed in real-time; eliminates manual follow-ups; improves platform responsiveness.
* **Definition of Done:** Automated notifications sent at fixed code trigger points (pickup scheduled, item ordered, disposal certified); notifications stored and viewable; newest first.
* **Acceptance Criteria:**
  * *Scenario 1 (Pickup Confirmed Notification):* Given a pickup is scheduled, when the status changes to "Scheduled", then the user receives the notification `"Your pickup has been scheduled."`
  * *Scenario 2 (Item Sold Notification):* Given an item is ordered, when the order is placed, then the recycler receives the notification `"Your item has been ordered."`
  * *Scenario 3 (View Notifications):* Given I have received notifications, when I open my notifications list, then I see them sorted by most recent first.

### 8.2 Detailed Test Cases (TC-35-XX)

| Test Case ID | Test Scenario | Type | Pre-condition | Test Action / Steps | Expected Result | Actual Status | Evidence / Screenshot |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: | :---: |
| **TC-35-01** | Pickup Scheduled Trigger Notification | Happy Path | Recycler confirms pickup schedule | 1. Pickup status set to "Scheduled"<br>2. Logistics publishes event<br>3. Check user inbox | Requester receives notification: `"Your pickup has been scheduled."` | **UNDONE** (API & UI missing) | TC-35-01_Pending_Dev.png |
| **TC-35-02** | Item Sold Trigger Notification | Happy Path | Buyer places order for listing | 1. Order placed in Marketplace<br>2. Check seller inbox | Recycler receives notification: `"Your item has been ordered."` | **UNDONE** (API & UI missing) | TC-35-02_Pending_Dev.png |
| **TC-35-03** | Disposal Certified Notification | Happy Path | Recycler issues disposal certificate | 1. Disposal certificate created<br>2. Check owner inbox | Item owner receives notification: `"Disposal certified for your item."` | **UNDONE** (API & UI missing) | TC-35-03_Pending_Dev.png |
| **TC-35-04** | Notifications Sorted Newest First | Happy Path | User has multiple notifications | 1. Open notifications list | Feed sorted chronologically descending (newest timestamp at top) | **UNDONE** (API & UI missing) | TC-35-04_Pending_Dev.png |
| **TC-35-05** | Mark Single Notification as Read | Happy Path | Unread notification present | 1. Call PUT /api/notifications/{id}/read | `IsRead` becomes true; unread badge counter decrements by 1 | **UNDONE** (API & UI missing) | TC-35-05_Pending_Dev.png |
| **TC-35-06** | Mark All Notifications as Read | Happy Path | Multiple unread notifications | 1. Call PUT /api/notifications/read-all | All notifications marked read; unread badge clears to 0 | **UNDONE** (API & UI missing) | TC-35-06_Pending_Dev.png |
| **TC-35-07** | Delete a Notification | Happy Path | Notification in feed | 1. Call DELETE /api/notifications/{id} | Notification removed from feed; 204 No Content / 200 OK | **UNDONE** (API & UI missing) | TC-35-07_Pending_Dev.png |
| **TC-35-08** | Duplicate Kafka Event Idempotency | Reliability | Event published with unique eventId | 1. Publish identical Kafka event twice | Consumer ignores duplicate eventId; exactly one notification created | **UNDONE** (API & UI missing) | TC-35-08_Pending_Dev.png |
| **TC-35-09** | Kafka Broker Restart Recovery | Fault Tolerance | Broker goes down while events queued | 1. Stop Kafka broker<br>2. Restart broker | Consumer resumes from last committed offset; no dropped or duplicate notifications | **UNDONE** (API & UI missing) | TC-35-09_Pending_Dev.png |
| **TC-35-10** | Malformed Event Lands in DLQ | Fault Tolerance | Invalid JSON payload published | 1. Publish malformed byte payload to topic | Event forwarded to Dead-Letter Topic (`*-dlq`); consumer does not crash | **UNDONE** (API & UI missing) | TC-35-10_Pending_Dev.png |
| **TC-35-11** | Cross-User Privacy & Isolation | Security | User A queries notifications | 1. Inspect response | User A receives only their own notifications; cannot access User B's alerts | **UNDONE** (API & UI missing) | TC-35-11_Pending_Dev.png |
| **TC-35-12** | Empty State Handling | Edge / UI | User has 0 notifications | 1. Open notification tray | Displays `"No notifications yet."` gracefully without error | **UNDONE** (API & UI missing) | TC-35-12_Pending_Dev.png |
| **TC-35-13** | Notification Delivery Latency Under Load | Performance | 50 concurrent events published | 1. Measure time between event publish and notification DB insert | Notification stored and available within < 3 seconds | **UNDONE** (API & UI missing) | TC-35-13_Pending_Dev.png |
| **TC-35-14** | Missing or Expired JWT Authentication | Security | No token provided | 1. Call notification API endpoints without token | 401 Unauthorized | **UNDONE** (API & UI missing) | TC-35-14_Pending_Dev.png |

---

## 9. Test Suite 6: Environmental Audit Log API & Event Logging (Developer Scope / ECO-39)

### 9.1 Story Details & Acceptance Criteria
* **Business Value:** Provides an immutable audit trail of all environmental lifecycle events (collection, disposal, refurbishing) for regulatory compliance.
* **Definition of Done:** Kafka consumer in Analytics records log entry automatically; admin-only endpoints for paged audit logs and search by `itemId`; unknown itemId returns empty list; JWT required.

### 9.2 Detailed Test Cases (TC-39-XX)

| Test Case ID | Test Scenario | Type | Steps & Conditions | Expected Result | Actual Status | Evidence |
| :--- | :--- | :---: | :--- | :--- | :---: | :---: |
| **TC-39-01** | Item Collected Auto-Audit Log | Happy Path | Pickup item marked "Collected" | Audit log entry written: action="COLLECTED", itemId, actor, timestamp | **UNDONE** | TC-39-01_Pending.png |
| **TC-39-02** | Item Disposed Auto-Audit Log | Happy Path | Disposal certificate issued | Audit log entry written: action="DISPOSED", itemId, actor, timestamp | **UNDONE** | TC-39-02_Pending.png |
| **TC-39-03** | Item Refurbished Auto-Audit Log | Happy Path | Listing created from valuation | Audit log entry written: action="REFURBISHED", itemId, actor, timestamp | **UNDONE** | TC-39-03_Pending.png |
| **TC-39-04** | Paged Audit Log Retrieval | Happy Path | Admin queries GET /api/audit-logs | Logs returned sorted by date descending with pagination metadata | **UNDONE** | TC-39-04_Pending.png |
| **TC-39-05** | Search Audit Log by Item ID | Happy Path | Admin queries by specific itemId | Returns historical timeline of all actions for that item | **UNDONE** | TC-39-05_Pending.png |
| **TC-39-06** | Search Unknown Item ID | Edge | Admin queries non-existent itemId | Returns empty list `[]` (200 OK), NOT a 404 or 500 error | **UNDONE** | TC-39-06_Pending.png |
| **TC-39-07** | Non-Admin Access Blocked | Security | Recycler/User calls audit log API | 403 Forbidden | **UNDONE** | TC-39-07_Pending.png |
| **TC-39-08** | Missing or Expired JWT | Security | Unauthenticated request sent | 401 Unauthorized | **UNDONE** | TC-39-08_Pending.png |

---

## 10. Integrated Regression Test Suite (Sprints 01-03 Carried Forward)

The following 13 regression test cases from Sprints 01 through 03 were verified on the active `Dev` branch. All passed with 100% success rate:

| Test ID | Module | Scenario / Feature | Expected Result | Actual Status |
| :---: | :--- | :--- | :--- | :---: |
| **TC-S2-01** | Identity (ECO-16) | User logout flow | JWT token invalidated, redirect to login | **PASS** |
| **TC-S2-02** | Identity (ECO-16) | Blacklisted token usage | Request with revoked token rejected (401) | **PASS** |
| **TC-S2-03** | Identity (ECO-17) | Forgot password email request | Reset token generated and returned | **PASS** |
| **TC-S2-04** | Identity (ECO-17) | Reset password with valid token | Password successfully updated, login works | **PASS** |
| **TC-S2-05** | Identity (ECO-17) | Reset password with invalid token | Rejected with invalid/expired token message | **PASS** |
| **TC-S2-06** | Logistics (ECO-22) | Recycler views pending pickups | Available pending requests displayed | **PASS** |
| **TC-S2-07** | Logistics (ECO-22) | Recycler confirms pickup slot | ScheduledDate and TimeSlot committed | **PASS** |
| **TC-S2-08** | Logistics (ECO-22) | Double-booking prevention | Conflicting slot rejected with 409 Conflict | **PASS** |
| **TC-S2-09** | Logistics (ECO-23) | Pickup lifecycle status tracking | Status stages render correctly | **PASS** |
| **TC-S2-10** | Logistics (ECO-23) | Empty pickup list state | Clean empty placeholder displayed | **PASS** |
| **TC-S2-11** | Logistics (ECO-24) | Cancel pickup request | Status transitions to "Cancelled" | **PASS** |
| **TC-S2-12** | Logistics (ECO-24) | Reschedule pickup date | Date updated; status preserved | **PASS** |
| **TC-S2-13** | Logistics (ECO-24) | Cancel collected pickup rejected | Rejection with "Already completed" error | **PASS** |

---

## 11. Selenium End-to-End Automation Results (`Sprint4E2ETests.cs`)

* **Test Suite File:** `tests/EcoTrack.Selenium.Tests/Sprint4E2ETests.cs`
* **Framework:** C# / .NET 10 xUnit with Selenium.WebDriver (Headless Chrome)
* **Build / Compilation Status:** **PASS (0 Errors, 0 Warnings)**
* **Execution Status:** **EXECUTED / FAILED (0 Passed / 6 Failed)** - Blocked due to missing developer frontend implementation.

### 11.1 Selenium Flow Execution Breakdown

| Flow ID | Scenario Name | Target Route | Target Element / Check | Live Result | Root Cause / Finding |
| :---: | :--- | :--- | :--- | :---: | :--- |
| **E2E-01** | Recycler adds a driver and assigns to Scheduled pickup | `http://localhost:5173/drivers/add` | `input[name='driverName']` | **FAIL** | Page `/drivers/add` does not exist on frontend (`NoSuchElementException`). Developer has not built driver views. |
| **E2E-02** | Recorder logs a drop-off -> Pickup shows "Collected" | `http://localhost:5173/pickups/active` | `button` containing 'Log Drop-off' | **FAIL** | Route `/pickups/active` does not exist (`NoSuchElementException`). Developer has not built drop-off UI. |
| **E2E-03** | Buyer orders item -> Pending payment -> Recycler marks Paid -> Refund cancels | `http://localhost:5173/marketplace` & `/recycler/orders` | `button` containing 'Order' / 'Mark Paid' | **FAIL** | Route `/recycler/orders` does not exist (`NoSuchElementException`). Developer has not built payment UI. |
| **E2E-04** | Sales report loads, filters by category, and exports CSV | `http://localhost:5173/admin/sales-report` | `select[name*='category']` | **FAIL** | Route `/admin/sales-report` does not exist (`NoSuchElementException`). Developer has not built sales report UI. |
| **E2E-05** | Scheduling a pickup & ordering produce notifications | `http://localhost:5173/schedule` | `.notification`, toast alerts | **FAIL** | Timed out waiting for notification banner (`WebDriverTimeoutException`). Developer has not built notification system. |
| **E2E-06** | Admin views audit log and searches by Item ID | `http://localhost:5173/admin/audit-log` | `input[name*='id']` | **FAIL** | Route `/admin/audit-log` does not exist (`NoSuchElementException`). Developer has not built audit search UI. |

> [!NOTE]
> **Summary on Selenium Tests:**  
> The C# automation suite is **100% written, properly structured, and compiles cleanly with 0 errors**. When executed against the browser, all 6 tests fail with `NoSuchElementException` or `WebDriverTimeoutException` because the React frontend currently contains Sprint 1-3 views only. The pages under test have not been built by the developer yet.

---

## 12. Defect, Blocker & Jira Issue Log

### 12.1 Sprint 04 Critical Architecture Blockers (Developer Missing Deliverables)

The following architectural blockers have been logged for Sprint 04, preventing test execution:

| Blocker Key | Summary | Severity | Priority | Component | Resolution / Required Dev Action |
| :--- | :--- | :---: | :---: | :--- | :--- |
| **BLK-04-01** | Missing Driver Assignment API & UI (Story 14 / ECO-25) | **Critical (Blocker)** | **P1 Urgent** | Logistics / Web UI | Implement `DriverController`, `DriverService`, `DriverRepository`, MySQL schema, and React UI components. |
| **BLK-04-02** | Missing Drop-off & Dispatch Record API & UI (Story 15 / ECO-26) | **Critical (Blocker)** | **P1 Urgent** | Logistics / Web UI | Implement drop-off transaction, date validation, Kafka `pickup-collected` event, and UI drop-off form. |
| **BLK-04-03** | Missing Escrow & Payment Record API & UI (Story 20 / ECO-31) | **Critical (Blocker)** | **P1 Urgent** | Marketplace / Web UI | Implement `Payments` table, payment transitions (Pending -> Paid -> Refunded), listing release transaction, and UI badges. |
| **BLK-04-04** | Missing Sales, Inventory & Revenue Report API & UI (Story 21 / ECO-32) | **Critical (Blocker)** | **P1 Urgent** | Analytics / Web UI | Implement Analytics database access, category aggregations, CSV/PDF export generation, and React report view. |
| **BLK-04-05** | Missing Alert & Notification System (Story 24 / ECO-35) | **Critical (Blocker)** | **P1 Urgent** | Analytics / Web UI | Implement Kafka consumer in Analytics for pickup/order/disposal events, notification API, and UI notification tray. |
| **BLK-04-06** | Missing Environmental Audit Log Consumer & API (ECO-39) | **Critical (Blocker)** | **P1 Urgent** | Analytics | Implement Analytics Kafka consumer for item events, DB logging, and admin search endpoints. |

---

### 12.2 Jira Defect Log: Regression & UI Issues (From Sprints 2 & 3)

In addition to sprint feature verification, QA conducted exploratory and regression testing across earlier sprint deliverables (Sprints 1-3) and logged the following issues into Jira:

| Jira Issue Key | Issue Summary | Severity | Priority | Sprint Origin | Status | Affected Component |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **ECO-BUG-01** | Top navigation bar stretches too long when logged in as Recycler, causing parts of the menu to be hidden | High | P2 High | Sprint 2 / 3 | Open | Web UI (`Navbar.jsx`) |
| **ECO-BUG-02** | Password field lacks eye icon to toggle visibility on login and registration pages | Medium | P3 Medium | Sprint 1 / 2 | Open | Web UI (`Login.jsx`, `Register.jsx`) |
| **ECO-BUG-03** | After logout, login page incorrectly shows top bar from previously logged-in role | High | P2 High | Sprint 2 / 3 | Open | Web UI (`AuthContext.jsx`, Router) |
| **ECO-BUG-04** | Notifications remain visible and do not auto-close, even after page refresh | Medium | P2 High | Sprint 3 | Open | Web UI (`Notification.jsx`) |

---

#### Jira Bug Report: ECO-BUG-01
* **Issue Key:** `ECO-BUG-01`
* **Issue Type:** Bug / UI Layout Defect
* **Summary:** Top navigation bar stretches too long when logged in as Recycler, causing parts of the menu to be hidden
* **Severity:** High (Major visual and interaction defect)
* **Priority:** P2 High
* **Component:** Web Frontend / Navigation Header (`apps/web/src/components/Navbar.jsx`)
* **Reporter:** Dilanka Dineth (QA Engineer)
* **Sprint Origin:** Sprint 2 / Sprint 3 Regression
* **Status:** Open (Logged in Jira)
* **Description:**  
  When logging in with a Recycler account, the dashboard top navigation bar appears stretched horizontally beyond normal container limits. Several role-specific menu items on the right side of the bar are not fully visible or clipped outside the viewport, and the layout looks broken compared to other roles (e.g., Buyer or Admin).
* **Steps to Reproduce:**
  1. Navigate to the EcoTrack web application.
  2. Log in with a valid Recycler account (e.g., `recycler@test.lk`).
  3. Navigate to the main dashboard.
  4. Observe the top navigation bar at standard display resolutions (1366x768 / 1920x1080).
* **Expected Result:**  
  The top navigation bar should display all menu items within the active viewport, properly aligned, responsive, and wrapped or collapsed into an overflow menu (such as a dropdown or hamburger icon) when menu items exceed header width.
* **Actual Result:**  
  The top navigation bar stretches horizontally beyond viewport limits. Trailing navigation links and account controls are pushed off-screen and cannot be clicked without browser zoom modification.
* **Remediation:**  
  Add `flex-wrap: wrap` or implement responsive CSS media queries with a collapsed hamburger dropdown menu for roles possessing extensive navigation link sets.

---

#### Jira Bug Report: ECO-BUG-02
* **Issue Key:** `ECO-BUG-02`
* **Issue Type:** Usability Defect / Form Input
* **Summary:** Password field lacks eye icon to toggle visibility on login and registration pages
* **Severity:** Medium (Accessibility and usability limitation)
* **Priority:** P3 Medium
* **Component:** Web Frontend / Authentication Views (`apps/web/src/views/Login.jsx`, `Register.jsx`)
* **Reporter:** Dilanka Dineth (QA Engineer)
* **Sprint Origin:** Sprint 1 / Sprint 2 Usability
* **Status:** Open (Logged in Jira)
* **Description:**  
  When logging in or registering, the password input field does not include an eye icon or toggle control to reveal plaintext. Users cannot view or confirm the password they are typing, which leads to increased authentication errors, particularly with complex passwords containing special characters.
* **Steps to Reproduce:**
  1. Open the EcoTrack application in a web browser.
  2. Navigate to either the `/login` or `/register` page.
  3. Enter a password string into the password field.
  4. Observe that there is no eye icon or toggle mechanism to reveal password visibility.
* **Expected Result:**  
  The password input field should include an eye toggle icon button (allowing toggle between masked `type="password"` and visible `type="text"`), improving usability, error prevention, and user confidence.
* **Actual Result:**  
  The password field remains permanently masked as bullet characters with no toggle control available on the interface.
* **Remediation:**  
  Integrate a stateful password visibility toggle (`showPassword` boolean state) with an eye icon button adornment in both `Login.jsx` and `Register.jsx`.

---

#### Jira Bug Report: ECO-BUG-03
* **Issue Key:** `ECO-BUG-03`
* **Issue Type:** Bug / Session State & Navigation Leak
* **Summary:** After logout, login page incorrectly shows top bar from previously logged-in role
* **Severity:** High (State management and session isolation flaw)
* **Priority:** P2 High
* **Component:** Web Frontend / Authentication Context & Router (`apps/web/src/context/AuthContext.jsx`, `App.jsx`)
* **Reporter:** Dilanka Dineth (QA Engineer)
* **Sprint Origin:** Sprint 2 / Sprint 3 Session Management
* **Status:** Open (Logged in Jira)
* **Description:**  
  When an authenticated user logs out and is redirected to the login page, the top navigation bar from the previously logged-in role (e.g., Recycler, Admin, Buyer) remains visible on the screen. This causes confusion and inconsistent UI behavior, as an unauthenticated login page should never render role-specific dashboard navigation.
* **Steps to Reproduce:**
  1. Log in to EcoTrack with any authenticated role (e.g., Recycler).
  2. Observe the role-specific top navigation bar rendered on the dashboard.
  3. Click the "Logout" button and return to the `/login` page.
  4. Observe the navigation bar rendered above the login form.
* **Expected Result:**  
  The login page should display only the clean login form and generic guest navigation. Role-specific headers, links, and user context must be cleared immediately upon logout.
* **Actual Result:**  
  The login page retains the role-specific top bar and menu items from the previous user session until a manual hard browser refresh is performed.
* **Remediation:**  
  Ensure the logout routine invokes a complete cache and state purge (`setUser(null)`, `localStorage.removeItem('token')`, `localStorage.removeItem('role')`) and conditionally render the role navigation bar only when `isAuthenticated === true`.

---

#### Jira Bug Report: ECO-BUG-04
* **Issue Key:** `ECO-BUG-04`
* **Issue Type:** Bug / UI Notification Management
* **Summary:** Notifications remain visible and do not auto-close, even after page refresh
* **Severity:** Medium (Visual clutter and user experience impediment)
* **Priority:** P2 High
* **Component:** Web Frontend / Notification Toast Component (`apps/web/src/components/Notification.jsx`)
* **Reporter:** Dilanka Dineth (QA Engineer)
* **Sprint Origin:** Sprint 3 Notification Module
* **Status:** Open (Logged in Jira)
* **Description:**  
  System notifications triggered on the dashboard remain pinned on the screen indefinitely. They do not fade out or dismiss automatically after an elapsed duration, and some toast notifications continue to persist even after reloading the page. This creates persistent visual clutter and obscures functional controls on the dashboard.
* **Steps to Reproduce:**
  1. Log in to the EcoTrack dashboard.
  2. Perform an action that triggers a toast notification (e.g., audit report visibility message, action confirmation).
  3. Observe that the notification banner remains fixed on the screen without fading.
  4. Refresh the web page via the browser refresh button.
  5. Observe that the notification banner is still rendered on screen.
* **Expected Result:**  
  Toast notifications should automatically dismiss after a standard duration (e.g., 4 to 5 seconds) with a smooth fade-out animation, or provide a visible and functional close/dismiss ('x') button. Refreshing the page should clear ephemeral notifications.
* **Actual Result:**  
  Notifications remain pinned on the interface indefinitely, requiring manual DOM intervention or state reset to disappear, and survive page reloads due to improper state persistence.
* **Remediation:**  
  Implement a timer-based auto-dismiss hook (`setTimeout(() => onClose(), 4000)`) within the toast notification component, provide an accessible dismiss button, and avoid saving transient toast messages into persistent storage.

---

## 13. Quality Assurance Sign-Off & Recommendations

### 13.1 Sign-Off Summary
* **Test Case Coverage:** 100% of acceptance criteria covered (66 new Sprint 4 test cases designed).
* **Automated Regression:** 13/13 past sprint regression tests passing.
* **Code Coverage (Unit):** Baseline microservice tests pass (55/55 Identity, 58/58 Marketplace, 58/58 Logistics).
* **Security & Vulnerabilities:** 0 vulnerabilities in .NET packages and npm dependencies.
* **Execution Status:** **BLOCKED** due to missing developer backend endpoints and frontend views.

### 13.2 Next Steps for Developer & QA
1. **Developer Priority:** Implement the 6 backend APIs using raw ADO.NET and register endpoints in Swagger.
2. **Frontend Priority:** Build the React views (`/drivers/add`, `/pickups/active`, `/admin/sales-report`, and notification bell) in `apps/web`.
3. **Bug Resolution:** Remediate active Jira regression defects (`ECO-BUG-01` to `ECO-BUG-04`) to restore layout consistency, form usability, and proper session cleanup.
4. **QA Execution:** Once deployed, execute all test cases (TC-25-XX, TC-26-XX, TC-31-XX, TC-32-XX, TC-35-XX), capture one screenshot per test case, and verify defect resolution.

---
*Report submitted by QA Engineer Dilanka Dineth (IT24610796) | Eco Track Sprint 04*