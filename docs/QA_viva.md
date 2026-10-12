# SE3022 | CASE STUDY PROJECT
## BSc (Hons) in Computer Science | Year 3 Semester 1
### Quality Assurance Oral Examination & Viva Preparation Guide (Sprint 04)

* **Candidate / QA Engineer:** Dilanka Dineth (IT24610796)
* **Project Name:** EcoTrack (Distributed Waste Management & Recycling Marketplace)
* **Target Sprint:** Sprint 04 (Final Sprint)
* **Academic Module:** SE3022 - Case Study Project (Y3S1)
* **Role:** Lead Quality Assurance Engineer

---

## 1. Quick Viva Pitch (The 60-Second Overview)

> **If the examiner asks: "Summarize your QA contribution for Sprint 04 in one minute:"**
> 
> "For Sprint 04, I served as the Lead QA Engineer covering the final sprint deliverables across Logistics, Marketplace, and Analytics. I conducted requirements analysis against the 5 core user stories (Stories 14, 15, 20, 21, and 24) plus the developer scope. 
> 
> Applying the ISTQB **Shift-Left testing methodology**, I fully designed **87 comprehensive test cases** (66 Sprint 4 cases, 8 developer scope cases, and 13 regression cases) categorized across Happy Path, Boundary Value, Negative, Security/RBAC, and Concurrency partitions. 
> 
> Additionally, I developed a 6-flow **Selenium WebDriver E2E test suite in C#** that compiles cleanly with zero errors, configured a **3-tier JMeter performance plan**, ran **Coverlet code coverage** (recording 31.8% Identity, 19.6% Marketplace, 21.9% Logistics), and conducted **dependency and OWASP security audits**. 
> 
> Through a deep-dive codebase audit, I determined that the developer has not implemented the 6 backend APIs or the 5 UI views. I documented this transparently, logging **6 architectural blockers (BLK-04-01 to BLK-04-06)** and **4 active Jira UI/regression bugs (ECO-BUG-01 to ECO-BUG-04)** from earlier sprints, leaving the QA suite 100% prepared for execution the moment code is deployed."

---

## 2. Core Viva Defense: Designing Tests Without Developer Implementation

### Question: "The developer did not implement the features. How could you design test cases without working code?"

#### The Academic & Professional Answer:
In modern software engineering (ISTQB and Agile standards), **testing does not begin when coding ends**. Testing begins at the requirements phase through **Shift-Left Testing**.

1. **Specification-Based Black-Box Design:**
   - Test cases are derived from **Acceptance Criteria, Business Value, and Definition of Done**, not by reading backend implementation code.
   - Software requirements define what the system *must* do. Designing tests prior to or in parallel with development ensures that tests are unbiased by developer implementation bugs.

2. **Formal Test Design Techniques Applied:**
   - **Equivalence Partitioning (EP):** Partitioned inputs into valid and invalid classes:
     * *Driver Assignment:* Scheduled pickups (valid) vs Completed/Cancelled/Collected pickups (invalid).
     * *Drop-off Record:* Valid ISO date (valid) vs Empty date or future timestamp (invalid).
     * *Payments:* Exact order amount (valid) vs mismatched amounts (invalid).
   - **Boundary Value Analysis (BVA):** Evaluated extreme boundaries:
     * *Date filters in Sales Report:* 0-day range, single day, `fromDate > toDate`, large multi-year date range.
     * *String lengths:* Maximum driver name length, 500+ character drop-off condition notes, special Unicode/SQL characters.
   - **State Transition Testing:** Mapped the business lifecycle state machines:
     * *Logistics Lifecycle:* `Scheduled` -> `Collected` -> `Disposed`.
     * *Escrow Payment Lifecycle:* `Pending` -> `Paid` -> `Refunded`.
     * Tested illegal state transitions: `Pending` -> `Refunded` (rejected), `Refunded` -> `Paid` (rejected), repeat `Paid` clicks (idempotency rejected).
   - **Role-Based Access Control (RBAC) & Security Partitioning:**
     * Multi-tenant boundary checks: Recycler A attempting to assign Recycler B's driver (Expect `403 Forbidden`).
     * Buyer attempting to mark an order as Paid or Refunded (Expect `403 Forbidden`).
     * Unauthenticated API calls without a valid Bearer token (Expect `401 Unauthorized`).
   - **Error Guessing & Resilience:**
     * No drivers available state ("No drivers available. Please add a driver first.").
     * Concurrent double-click refunds.
     * Kafka duplicate event delivery (idempotency) and Kafka broker outage recovery.

---

## 3. Test Case Categorization & Quantitative Breakdown

### 3.1 Overall Quantitative Metrics

| Metric / Dimension | Value | Notes |
| :--- | :---: | :--- |
| **Total Test Cases Designed** | **87 Cases** | Across all stories, dev tasks, and regression |
| **Sprint 04 Feature Test Cases** | **66 Cases** | ECO-25, ECO-26, ECO-31, ECO-32, ECO-35, ECO-39 |
| **Sprint 1-3 Regression Test Cases** | **13 Cases** | Carried forward across Identity, Logistics, Market |
| **Environmental Audit Scope** | **8 Cases** | ECO-39 Analytics Kafka consumer & API |
| **Selenium E2E Flows** | **6 Flows** | `tests/EcoTrack.Selenium.Tests/Sprint4E2ETests.cs` |
| **JMeter Load Scenarios** | **3 Scenarios** | Baseline (10 VU), Ramp-Up (50 VU), Spike (100 VU) |
| **Jira Regression Bug Tickets** | **4 Bugs** | ECO-BUG-01 to ECO-BUG-04 |
| **Architectural Blockers Logged** | **6 Blockers** | BLK-04-01 to BLK-04-06 |

---

### 3.2 Breakdown by Testing Category

| Test Category | Case Count | Percentage | Purpose & Coverage |
| :--- | :---: | :---: | :--- |
| **Happy Path / Positive Functional** | **21** | 24.1% | Verifies normal user journeys meeting core acceptance criteria |
| **Negative / Input Validation** | **22** | 25.3% | Validates system defense against invalid inputs, missing fields, format errors |
| **Boundary Value & Edge Cases** | **16** | 18.4% | Validates extreme boundaries, empty states, zero-data conditions, large payloads |
| **Security, RBAC & JWT** | **16** | 18.4% | Validates 401 Unauthorized, 403 Forbidden, cross-tenant isolation, expired tokens |
| **Concurrency & Event Idempotency** | **6** | 6.9% | Validates race conditions, double-click refunds, duplicate Kafka event delivery |
| **Cross-Sprint Regression** | **6** | 6.9% | Validates existing authentication, pickup scheduling, and listing creation |
| **Total** | **87** | **100.0%** | Comprehensive coverage across all functional & non-functional dimensions |

---

### 3.3 Detailed Breakdown by User Story

#### 1. ECO-25 (Story 14) - Driver Assignment API & UI
* **Assigned Service:** Logistics Service (`:5002`)
* **Total Test Cases:** **12 Cases** (`TC-25-01` to `TC-25-12`)
* **Category Breakdown:**
  - Happy Path (3): Assign driver to Scheduled pickup, change assigned driver, view assigned driver name on pickup record.
  - Negative (4): Empty driver name/phone, duplicate driver registration, invalid phone format, assign driver to Completed/Cancelled/Collected pickup.
  - Boundary & Edge (2): No drivers available empty state prompt ("No drivers available. Please add a driver first."), oversized driver name string (255+ chars).
  - Security / RBAC (3): Assign another recycler's driver (403), assign driver on another recycler's pickup (403), unauthenticated request without JWT (401).
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built `DriverController` or React `/drivers/add` view).

#### 2. ECO-26 (Story 15) - Drop-off & Dispatch Record API & UI
* **Assigned Service:** Logistics Service (`:5002`)
* **Total Test Cases:** **13 Cases** (`TC-26-01` to `TC-26-13`)
* **Category Breakdown:**
  - Happy Path (3): Record drop-off with date and notes, pickup status transitions atomically to "Collected", owner views drop-off history.
  - Negative (3): Empty drop-off date ("Drop-off date is required."), record drop-off on Cancelled pickup, record drop-off on unassigned/unpicked request.
  - Boundary & Edge (3): Future drop-off date rejected, 1000+ character notes boundary, notes containing SQL/XSS special characters sanitized.
  - Security / RBAC (3): Non-owner/unassigned recycler recording drop-off (403), non-owner viewing drop-off history (403), missing/expired JWT (401).
  - Concurrency / Idempotency (1): Second drop-off attempt on already collected pickup rejected.
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built `DropoffController` or React `/pickups/active` view).

#### 3. ECO-31 (Story 20) - Escrow & Payment Record API & UI
* **Assigned Service:** Marketplace Service (`:5003`)
* **Total Test Cases:** **12 Cases** (`TC-31-01` to `TC-31-12`)
* **Category Breakdown:**
  - Happy Path (3): Order placement creates payment record in "Pending" status, recycler/admin marks payment as "Paid", marking "Paid" payment as "Refunded" cancels order and releases listing.
  - Negative (3): Direct transition Pending -> Refunded rejected, transition Refunded -> Paid rejected, repeated "Paid" clicks rejected.
  - Boundary & Edge (2): Payment amount strictly equals order total price, zero/negative payment validation.
  - Security / RBAC (3): Buyer attempting to mark order Paid or Refunded (403), user accessing another user's payment record (403), missing JWT (401).
  - Concurrency (1): Concurrent double-click refund handled safely without duplicate inventory release.
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built payment endpoints or React order payment actions).

#### 4. ECO-32 (Story 21) - Sales, Inventory & Revenue Report API & UI
* **Assigned Service:** Analytics Service (`:5004`)
* **Total Test Cases:** **15 Cases** (`TC-32-01` to `TC-32-15`)
* **Category Breakdown:**
  - Happy Path (3): Admin/recycler views total sales, revenue, and inventory by category; filter by category displays isolated category data; CSV/PDF export matches screen data.
  - Negative (3): Date range where `fromDate > toDate` rejected with validation message, invalid date formats, non-numeric category IDs.
  - Boundary & Edge (5): Date range with 0 data returns clean empty state, single-day range (`fromDate == toDate`), combined date and category filter, refunded orders excluded from gross revenue calculation, special characters in exported file properly escaped.
  - Security / RBAC (3): Recycler viewing another recycler's sales report (403), buyer account accessing sales report (403), missing JWT (401).
  - Performance / Volume (1): Large dataset report generation completes within acceptable SLA (< 3 seconds).
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built report aggregations, export logic, or React `/admin/sales-report`).

#### 5. ECO-35 (Story 24) - Alert & Notification Rule API & UI
* **Assigned Service:** Analytics Service (`:5004`)
* **Total Test Cases:** **14 Cases** (`TC-35-01` to `TC-35-14`)
* **Category Breakdown:**
  - Happy Path (3): Scheduled pickup triggers "Your pickup has been scheduled." notification to user; order placement triggers "Your item has been ordered." to recycler; notifications list displays newest first.
  - Negative (2): Mark non-existent notification as read returns 404, delete non-existent notification returns 404.
  - Boundary & Edge (3): Empty notifications list displays clean empty state, mark-all-read updates unread badge counter to zero, single notification delete behavior.
  - Security / RBAC (3): User accessing another user's notifications list returns empty/403, unauthorized read/delete of foreign notification (403), missing JWT (401).
  - Kafka Resilience & Idempotency (3): Duplicate Kafka event received twice results in only one stored notification; Kafka broker outage and recovery results in zero lost messages; malformed event payload routed to Dead Letter Queue (DLQ).
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built Kafka notification consumer or UI notification tray).

#### 6. ECO-39 (Developer Scope) - Environmental Audit Log API & Logging
* **Assigned Service:** Analytics Service (`:5004`)
* **Total Test Cases:** **8 Cases** (`TC-39-01` to `TC-39-08`)
* **Category Breakdown:**
  - Happy Path (2): Event consumer writes item audit entry upon collection/disposal; admin views audit logs sorted date descending.
  - Boundary & Edge (2): Search by non-existent Item ID returns clean empty list `[]` (200 OK); search with date range filters.
  - Security / RBAC (2): Non-admin user querying audit log returns 403 Forbidden; missing JWT returns 401 Unauthorized.
  - Idempotency & Immutability (2): Duplicate Kafka events ignored by message key; API does not expose PUT or DELETE endpoints (immutable audit trail).
* **Execution Verdict:** **BLOCKED / UNDONE** (Developer has not built audit log consumer or API).

#### 7. Carried Forward Regression Suite (Sprints 1-3)
* **Scope:** Identity, Logistics, Marketplace baseline flows
* **Total Test Cases:** **13 Cases** (`TC-S2-01` to `TC-S2-13`)
* **Execution Verdict:** **PASSED (13 / 13)** - Validates that core authentication, pickup creation, and item listings continue functioning without regressions.

---

## 4. Automation & Tooling Breakdown (How to Explain Your Code)

### 4.1 Selenium E2E Automation Suite
* **File Reference:** `tests/EcoTrack.Selenium.Tests/Sprint4E2ETests.cs`
* **Technology Stack:** C#, .NET 10, NUnit 4, Selenium WebDriver 4, Chrome headless / headful mode.
* **Compilation Status:** Compiles with **0 errors**.
* **Key Design Architecture:**
  - Implements the **Page Object Model (POM)** and explicit `WebDriverWait` polling strategy.
  - Reusable helper methods: `LoginAsAsync(email, password)`, `WaitForElement()`, `ClickWithWait()`.
  - Avoided brittle CSS selectors (e.g., replaced `:has(text())` with robust XPath `//button[contains(text(), '...')]`).
* **Viva Question: "Why did all 6 Selenium tests fail when executed?"**
  - **Your Answer:** "They failed honestly with `NoSuchElementException` and `WebDriverTimeoutException` because the developer has not built any of the target React views (`/drivers/add`, `/pickups/active`, `/admin/sales-report`). This failure is the expected, correct behavior of an automated test suite. It proves that the tests are not fake mocks; they actively inspect the browser DOM, search for input fields, and fail when the developer's work is absent."

### 4.2 JMeter Performance Testing Plan
* **File Reference:** `tests/jmeter/Sprint4_Performance_Test_Plan.jmx` and `tests/jmeter/run_sprint4_jmeter.sh`
* **Technology Stack:** Apache JMeter 5.6.3, CLI non-interactive execution mode.
* **3 Configured Scenarios:**
  1. *Baseline Load:* 10 Virtual Users (VUs), 30s ramp-up, 180s duration (tests steady-state latency < 500ms).
  2. *Stress / Ramp-Up:* 50 Virtual Users, 60s ramp-up, 240s duration (tests concurrency limits and throughput).
  3. *Spike Load:* 100 Virtual Users sudden burst within 10s (tests error rates and database connection pooling under sudden surges).

### 4.3 Code Coverage Collection
* **Tool Used:** Coverlet data collector integrated with `dotnet test`, summarized using ReportGenerator.
* **Baseline Coverage Collected:**
  - Identity Service: **31.8% Line Coverage / 15.7% Branch Coverage**
  - Marketplace Service: **19.6% Line Coverage / 14.7% Branch Coverage**
  - Logistics Service: **21.9% Line Coverage / 9.4% Branch Coverage**
* **Viva Question: "Why are line coverage percentages in the 20-30% range?"**
  - **Your Answer:** "These figures represent unit test coverage over core business services (e.g., password hashing, order validation). Because the developer has not built the Sprint 4 controllers, repositories, or React UI views, E2E tests cannot exercise those execution paths yet. Once the developer implements and deploys the views, running the integrated Selenium suite against the backend will raise overall coverage to our 80% target."

### 4.4 Security Scanning & Vulnerability Remediation
* **npm Dependency Audit:** Scanned React frontend dependencies. Successfully patched **4 high/moderate vulnerabilities** using `npm audit fix`:
  - `axios`: Fixed prototype pollution and `NO_PROXY` bypass.
  - `source-map-js`: Fixed event-loop Denial of Service (DoS).
  - `react-router`: Fixed open redirect vulnerability.
  - Result: **0 vulnerable npm packages remaining**.
* **.NET Package Audit:** `dotnet list package --vulnerable` across all microservice projects revealed **0 vulnerable NuGet packages**.
* **Dynamic Analysis:** OWASP ZAP baseline scan executed against running microservices ports (`:5001`, `:5002`, `:5003`, `:5004`).

---

## 5. Defects & Jira Issue Tracking

### 5.1 Sprint 04 Architectural Blockers (Developer Missing Scope)
The QA team formally logged 6 blockers preventing sprint sign-off:
* **BLK-04-01:** Missing Driver Assignment API, DB schema, and UI (ECO-25)
* **BLK-04-02:** Missing Drop-off Record API, atomic status transition, and UI (ECO-26)
* **BLK-04-03:** Missing Escrow & Payment API and state machine (ECO-31)
* **BLK-04-04:** Missing Sales, Inventory & Revenue Report aggregations and export (ECO-32)
* **BLK-04-05:** Missing Kafka Notification consumer and notification API (ECO-35)
* **BLK-04-06:** Missing Environmental Audit Log consumer and API (ECO-39)

### 5.2 Active Jira Regression & UI Bugs (Carried from Sprints 2 & 3)
QA conducted exploratory and regression testing on existing UI screens and logged 4 defects into Jira:

1. **`ECO-BUG-01` (Severity: High, Priority: P2): Recycler Navbar Overflow & Clipping**
   - *Issue:* Top navigation bar stretches too long when logged in as Recycler, causing parts of the menu to be hidden off-screen.
   - *Remediation:* Implement flex wrapping and responsive hamburger dropdown for roles with extensive links.

2. **`ECO-BUG-02` (Severity: Medium, Priority: P3): Missing Password Visibility Eye Toggle**
   - *Issue:* Password input field lacks an eye icon to toggle visibility on `/login` and `/register`, increasing typo rates.
   - *Remediation:* Add a show/hide toggle state with an eye icon button adorning password inputs.

3. **`ECO-BUG-03` (Severity: High, Priority: P2): Stale Role Top Bar Persisting on Login Page Post-Logout**
   - *Issue:* After user logs out, the login page retains the role-specific top bar from the previous role until a hard refresh.
   - *Remediation:* Purge user and role state in `AuthContext` on logout and conditionally render top bar only when authenticated.

4. **`ECO-BUG-04` (Severity: Medium, Priority: P2): Notifications Fail to Auto-Close & Persist Across Refresh**
   - *Issue:* System toast notifications on the dashboard remain pinned indefinitely and persist even after page reload.
   - *Remediation:* Implement a 4-second `setTimeout` auto-dismiss, provide a close button, and clear toast state on unmount.

---

## 6. Top 10 Viva Questions & Winning Answers (Cheat Sheet)

### Q1: "What was your main role in the project?"
**Answer:** "I am Dilanka Dineth (IT24610796), the Quality Assurance Engineer. My responsibility is end-to-end quality governance: designing test suites from requirements, executing functional, negative, boundary, and security tests, developing automated Selenium E2E scripts, authoring performance testing plans in JMeter, collecting code coverage, conducting security audits, and managing defect lifecycles."

### Q2: "Why does the report say '0 / 6 Developer APIs Implemented' and 'UNDONE'?"
**Answer:** "As a QA Engineer, my primary duty is providing honest, objective verification of the codebase. A thorough audit of `src/Logistics`, `src/Marketplace`, `src/Analytics`, and MySQL migrations revealed that the developer has not yet written the controllers, services, repositories, or SQL tables for Sprint 4. Rather than producing fabricated test passes, I documented the honest technical status and classified the developer tasks as UNDONE."

### Q3: "If developer code is missing, what evidence proves that you actually worked in this sprint?"
**Answer:** "My deliverables are extensive and verifiable:
1. 87 fully designed test cases documented with exact preconditions, test steps, test data, and expected results.
2. A complete Selenium WebDriver C# test project (`Sprint4E2ETests.cs`) compiling with 0 errors across 6 user journeys.
3. An Apache JMeter test plan (`Sprint4_Performance_Test_Plan.jmx`) covering 3 workload profiles.
4. Code coverage collection reports using Coverlet and ReportGenerator.
5. Security audit resolving 4 npm vulnerabilities to zero.
6. 6 documented architectural blockers and 4 verified Jira UI bug tickets."

### Q4: "What is Shift-Left testing and how did you apply it?"
**Answer:** "Shift-Left testing means moving testing activities earlier in the software development lifecycle. Instead of waiting for code deployment, QA begins at the requirements stage. In Sprint 4, I analyzed the acceptance criteria, modeled input partitions and state transition diagrams, identified boundary limits, and pre-wrote automated Selenium test scripts. This ensures that the moment the developer deploys code, testing can execute immediately without delay."

### Q5: "What is the difference between a Blocker and a Bug?"
**Answer:** "A **Bug** (e.g., `ECO-BUG-01` to `ECO-BUG-04`) is a defect in working code where the actual behavior deviates from the expected behavior, but the application remains testable. A **Blocker** (e.g., `BLK-04-01` to `BLK-04-06`) is a missing component or fatal dependency that halts testing entirely, preventing the execution of test cases for that feature."

### Q6: "How did you test security in Sprint 04?"
**Answer:** "Security testing had three tiers:
1. *Authentication & Authorization Test Design:* Designed 16 test cases covering missing/expired JWTs (expect 401 Unauthorized) and RBAC privilege escalation (e.g., Buyer accessing Recycler sales reports or payments, expecting 403 Forbidden).
2. *Static Dependency Scanning:* Executed `npm audit` on the React frontend, fixing 4 high/moderate vulnerabilities in axios and react-router, and `dotnet list package --vulnerable` showing 0 vulnerable NuGet packages.
3. *Dynamic Scanning:* Configured OWASP ZAP baseline scan against API and UI endpoints."

### Q7: "Why did Selenium tests fail with `NoSuchElementException`?"
**Answer:** "Because Selenium tests look for real DOM elements on the browser. When testing `E2E-01` (Driver Assignment), Selenium navigated to `/drivers/add` and attempted to find `input[name='driverName']`. Since the developer has not built that React route or form, the WebDriver accurately timed out and threw `NoSuchElementException`. This proves that our automation framework is genuine and correctly asserting DOM existence."

### Q8: "How did you test state transitions in Story 20 (Escrow & Payment Record)?"
**Answer:** "I mapped the valid state machine: `Pending` -> `Paid` -> `Refunded`. I designed test cases to verify that invalid state transitions are strictly rejected by the backend with HTTP 400:
- Transition from `Pending` directly to `Refunded` (rejected).
- Transition from `Refunded` back to `Paid` (rejected).
- Repeated clicks on 'Paid' when already Paid (idempotency rejected).
- Marking 'Paid' as 'Refunded' atomically updates payment status and cancels the linked order, making the listing available again."

### Q9: "What JMeter performance scenarios did you design?"
**Answer:** "I designed three load profiles in `Sprint4_Performance_Test_Plan.jmx`:
1. Baseline: 10 concurrent users over 180s to measure baseline API latency (< 500ms SLA).
2. Ramp-Up: 50 concurrent users over 240s to observe throughput under realistic peak operational load.
3. Spike: 100 concurrent users bursting in 10s to evaluate system stability and database connection pool behavior under sudden surges."

### Q10: "If the developer delivers the code tomorrow, what are your next steps?"
**Answer:** "My test framework is fully prepared for immediate execution:
1. Verify database migrations run and tables exist (`Drivers`, `DropOffRecords`, `Payments`, `Notifications`).
2. Run automated Selenium E2E suite via `dotnet test` to confirm happy path journeys.
3. Execute the 66 functional, boundary, negative, and security test cases.
4. Capture required screenshots for each test case (`TC-25-XX` to `TC-35-XX`).
5. Re-run Coverlet to verify code coverage reaches our 80% line target.
6. Run JMeter script to validate response time SLAs."

---

*Viva Guide compiled and verified by QA Engineer Dilanka Dineth (IT24610796)*
