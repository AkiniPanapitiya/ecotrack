# EcoTrack — Smart E-Waste Management & Circular Economy Platform

**Course:** SE3022 — Case Study Project (Year 3, Semester 1, 2026)  
**Group:** 34 | **4 Members**

---

## 📖 What is EcoTrack?

EcoTrack is a **Smart E-Waste Management & Circular Economy Platform** that automates e-waste collection, provides fair valuation of electronic items, creates a marketplace for refurbished devices, and tracks environmental impact metrics (CO₂ diverted, heavy metals diverted from landfills).

It is built as an **Event-Driven Microservices Architecture** with:
- 4 independent **ASP.NET Core Web API (.NET 8)** microservices
- 1 **React 18 SPA** frontend
- **Apache Kafka** for event streaming
- **4 isolated MySQL 8.0 databases** (one per microservice)
- **ADO.NET direct SQL** for all database access (no ORM)

---

## 👥 Team

| Name | IT Number | Microservice Ownership |
|------|-----------|----------------------|
| Panapitiya P D A S | IT24610790 | E-Waste Collection & Logistics Service |
| Rajapaksha R M M D C | IT24610798 | User & Recycler Identity Management Service |
| **Ekanayake E M D D B** | **IT24610796** | **Impact Analytics, Monitoring & Compliance Service** |
| Wijewardhana S L | IT24610783 | Valuation, Refurbishment & Marketplace Service |

---

## 🏗 System Architecture

```
                    ┌─────────────────────────────────────────────┐
                    │           React 18 SPA (Frontend)            │
                    │  HTML5 + Tailwind CSS + Axios + Chart.js     │
                    └───────────────────┬─────────────────────────┘
                                        │
                                        ▼
                              ┌─────────────────────┐
                              │  API Gateway /       │
                              │  Reverse Proxy        │
                              └──────────┬──────────┘
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
                    ▼                    ▼                    ▼
          ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
          │  Identity Service │  │ Logistics Service │  │ Marketplace Svc  │
          │  (Port 5001)      │  │ (Port 5002)       │  │ (Port 5003)      │
          │  ASP.NET Core 8   │  │ ASP.NET Core 8    │  │ ASP.NET Core 8   │
          └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
                   │                      │                      │
                   ▼                      ▼                      ▼
          ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
          │  MySQL:           │  │  MySQL:           │  │  MySQL:           │
          │  ecotrack_        │  │  ecotrack_        │  │  ecotrack_        │
          │  identity_db      │  │  logistics_db     │  │  marketplace_db   │
          └──────────────────┘  └──────────────────┘  └──────────────────┘
                   │                      │                      │
                   └─────────────────────┼──────────────────────┘
                                         │
                                         ▼
                              ┌─────────────────────┐
                              │   Apache Kafka       │
                              │   (Event Broker)     │
                              └──────────┬──────────┘
                                         │
                                         ▼
                              ┌─────────────────────┐
                              │  Analytics Service   │
                              │  (Port 5004)         │
                              │  ASP.NET Core 8      │
                              └────────┬────────────┘
                                       │
                                       ▼
                              ┌─────────────────────┐
                              │  MySQL:              │
                              │  ecotrack_analytics  │
                              │  _db                 │
                              └─────────────────────┘
```

### Data Flow

1. **Synchronous:** React SPA → API Gateway → microservice → MySQL (ADO.NET)
2. **Asynchronous:** Microservice → Kafka topic → consumer microservice → MySQL

### Kafka Event Topics

| Topic | Producer | Consumer | Trigger |
|-------|----------|----------|---------|
| `pickup.lifecycle.events` | Logistics Service (5002) | Analytics & Alerts (5004) | When pickup is REQUESTED, PICKED_UP, or DELIVERED |
| `marketplace.order.events` | Marketplace Service (5003) | Analytics & Alerts (5004) | When order checkout completes |
| `ewaste.disposal.events` | Logistics / Yard (5002) | Impact Engine (5004) | On facility verification with batch weights → triggers CO₂ calculation + certificate generation |

---

## 🧩 The 4 Microservices

### 1. Identity & Access Management Service (Rajapaksha — IT24610798)

Handles user and recycler identity, authentication, and access control.

**Entities (4 CRUD + 1 Dynamic Report):**

| Entity | Create | Read | Update | Delete |
|--------|--------|------|--------|--------|
| Recycler Profile | Register profile | View profile/facility data | Update profile & facility specs | Deactivate profile |
| Role & Permission | Create system role | Read assigned permissions | Modify permission scopes | Revoke access role |
| KYC Verification | Upload license/permits | Fetch verification status | Re-upload compliance docs | Purge KYC records |
| User Feedback | Post rating/review | Read public reviews | Update feedback entry | Remove flagged review |

**Dynamic Report:** Recycler Verification & User Activity Audit Report (filtered by verification state and date range)

**Database Schema:** `ecotrack_identity_db` — Tables: Users, RecyclerProfiles, Roles, KycDocuments, UserFeedback

---

### 2. E-Waste Collection & Logistics Service (Panapitiya — IT24610790)

Handles e-waste pickup scheduling, route assignment, and logistics tracking.

**Entities (4 CRUD + 1 Dynamic Report):**

| Entity | Create | Read | Update | Delete |
|--------|--------|------|--------|--------|
| Pickup Request | Submit e-waste request | View request & QR token | Modify details/address | Cancel request |
| Collection Schedule | Create time window | Query regional slots | Reschedule time slot | Close slot |
| Driver Assignment | Assign driver to batch | Fetch driver manifest | Reassign driver allocation | Unassign driver record |
| Drop-off Record | Log delivery | Query history | Verify intake weight | Void invalid record |

**Dynamic Report:** Logistics Fleet & Collection Efficiency Report (filtered by zone, completion status, and vehicle capacity)

**Database Schema:** `ecotrack_logistics_db` — Tables: PickupRequests, PickupItems, CollectionSchedules, DriverAssignments, DropoffRecords

---

### 3. Valuation, Refurbishment & Marketplace Service (Wijewardhana — IT24610783)

Handles scrap valuation, refurbished listings, marketplace orders, and payments.

**Entities (4 CRUD + 1 Dynamic Report):**

| Entity | Create | Read | Update | Delete |
|--------|--------|------|--------|--------|
| Item Valuation | Calculate valuation estimate | Query estimates | Update valuation factors | Clear expired estimate |
| Refurbished Listing | Publish product listing | Browse/filter catalog | Update price & stock | Delist product |
| Marketplace Order | Place order | View invoices | Update dispatch status | Cancel pending order |
| Payment Record | Charge/record checkout | Fetch transaction log | Process status update | Void authorized refund record |

**Dynamic Report:** Sales, Inventory & Revenue Breakdown Report (filtered by product category, date range, and seller ID)

**Database Schema:** `ecotrack_marketplace_db` — Tables: ValuationRecords, RefurbishedListings, Orders, OrderItems, PaymentTransactions

---

### 4. Impact Analytics, Monitoring & Compliance Service (Ekanayake — IT24610796)

Handles environmental impact tracking, carbon offset calculation, disposal certificates, and compliance auditing.

**Entities (4 CRUD + 1 Dynamic Report):**

| Entity | Create | Read | Update | Delete |
|--------|--------|------|--------|--------|
| Disposal Certificate | Generate certificate | Download signed PDF | Append verification seal | Revoke invalid certificate |
| Carbon Impact Target | Create regional target | Read impact metrics | Adjust baseline targets | Delete obsolete target |
| Alert Rule | Create threshold rule | View active alerts | Update notification trigger | Delete rule |
| Environmental Audit Log | Record disposal event | Query compliance log | Annotate audit record | Purge archived log |

**Dynamic Report:** Environmental Impact & Carbon Offset Report (filtered by material type, region, and corporate entity)

**Database Schema:** `ecotrack_analytics_db` — Tables: DisposalCertificates, CarbonTargets, AlertRules, EnvironmentalAuditLogs

---

## 👥 User Classes & Responsibilities

| User Class | Responsibilities & Permissions |
|------------|-------------------------------|
| **Individual / Corporate User** | Schedule e-waste collection requests, track reverse logistics in real time, view scrap valuation estimates, purchase refurbished electronics, download Digital Safe Disposal Certificates |
| **Certified Recycler** | Submit regulatory licensing and KYC documents, accept bulk e-waste batches, manage refurbishment pipelines, publish refurbished devices with warranties to the marketplace |
| **Logistics Driver** | Accept regional pickup jobs, navigate collection routes, scan batch QR codes at user doorsteps, execute verified drop-offs at recycling yards |
| **System Admin / Auditor** | Review and approve recycler licensing, monitor system-wide compliance metrics, inspect audit logs, export dynamic environmental impact reports |

---

## 📋 Functional Requirements (FR-01 to FR-20)

### Must Have

| ID | Requirement | Priority |
|----|------------|:--------:|
| FR-01 | Allow users and recyclers to register, sign in, and manage profiles via JWT Role-Based Access Control (RBAC) | Must |
| FR-02 | Enable recyclers to upload regulatory KYC licensing documentation for administrative approval | Must |
| FR-03 | Allow System Admins to verify, approve, reject, or suspend recycler accounts and review activity audit logs | Must |
| FR-04 | Allow users to schedule doorstep e-waste pickups specifying category, weight, and preferred time slot | Must |
| FR-05 | Assign collection routes to logistics drivers based on regional proximity and vehicle capacity | Must |
| FR-06 | Generate unique QR verification tokens for each pickup batch to enforce verifiable Chain of Custody | Must |
| FR-07 | Allow drivers to scan QR codes and update batch statuses (Assigned → Picked Up → Delivered) | Must |
| FR-08 | Calculate rule-based scrap valuations based on device category, physical condition, and commodity base indices | Must |
| FR-09 | Allow verified recyclers to list certified refurbished hardware with warranties on the marketplace | Must |
| FR-10 | Enable users to browse refurbished catalogs, manage shopping carts, and execute checkout transactions | Must |
| FR-11 | Publish pickup and order life-cycle events to Apache Kafka topics asynchronously upon database commit | Must |
| FR-12 | Consume Kafka disposal events to calculate total carbon offsets (kg CO₂) and heavy metals diverted from landfills | Must |
| FR-13 | Automatically generate digitally verifiable, tamper-evident Digital Safe Disposal Certificates (PDF) | Must |
| FR-14 | Provide System Admins with a real-time operational telemetry dashboard for regional recycling metrics | Must |
| FR-17 | Enforce strict schema isolation ensuring each microservice queries only its own MySQL database via direct ADO.NET | Must |
| FR-18 | Execute all database transactions via ADO.NET parameterized queries to guarantee 100% protection against SQL injection | Must |
| FR-19 | Provide interactive Swagger/OpenAPI documentation for all four microservice API surfaces | Must |
| FR-20 | Generate four distinct dynamic reports exportable to PDF/CSV with customizable filters | Must |

### Should Have

| ID | Requirement | Priority |
|----|------------|:--------:|
| FR-15 | Trigger automated threshold alert notifications when hazardous waste volumes exceed safe facility limits | Should |
| FR-16 | Award Green Reward points to user accounts upon verified e-waste facility delivery for marketplace redemptions | Should |

---

## 🎯 Non-Functional Requirements (NFR-01 to NFR-08)

| ID | Category | Requirement | Verification |
|----|----------|------------|:-----------:|
| NFR-01 | Performance | Core CRUD endpoints shall execute and respond within **≤ 300ms** under standard operational load | JMeter / Postman |
| NFR-02 | Throughput | The platform shall sustain **500 concurrent active sessions** without latency exceeding **1.5 seconds** | JMeter Concurrency Suite |
| NFR-03 | Security | All passwords hashed using **BCrypt**; client-server traffic encrypted via **TLS 1.3** | OWASP ZAP |
| NFR-04 | Data Access | Direct ADO.NET SQL parameterization across all data repositories with **zero dynamic string concatenations** | Static Code Analysis |
| NFR-05 | Messaging Reliability | Kafka event producers must use **acks=all** to guarantee zero message loss during streaming | Kafka Failure Test |
| NFR-06 | Maintainability | Backend code must achieve **≥ 70% Unit Test code coverage** across business and data access layers | xUnit / Coverlet |
| NFR-07 | Portability | The full multi-service environment (APIs, DBs, Kafka, Zookeeper) must run locally via a **single `docker compose up`** | Docker Test |
| NFR-08 | Observability | All services must expose **Prometheus health metrics** and integrate with central **Grafana telemetry dashboards** | Grafana Scrapes |

---

## 🛠️ Technology Stack

| Technology | Concrete Implementation Task | Primary Owner | Measurable Output |
|------------|------------------------------|---------------|:----------------:|
| **React 18 SPA** | Build responsive user, recycler, driver, and admin portals with Axios HTTP client | All Members | Deployed Web UI |
| **ASP.NET Core (.NET 8)** | Develop 4 autonomous REST Web API microservices with Swagger documentation | All Service Owners | OpenAPI Swagger UI |
| **ADO.NET (Direct SQL)** | Write parameterized SQL commands and data readers; enforce zero ORM usage | Each Service Owner | Repository Code |
| **MySQL 8.0** | Design, script, and provision 4 isolated database schemas with indexing | Each Service Owner | DDL Scripts / Migration |
| **Apache Kafka** | Setup Kafka broker, define topic schemas, implement C# Producers and Consumer Groups | DevOps / Member 04 | Running Kafka Cluster |
| **Docker & Compose** | Create Dockerfiles for services, frontend, Kafka, and Zookeeper; configure Compose network | DevOps Role | `docker-compose.yml` |
| **Azure Cloud** | Provision Azure App Services, configure staging environment variables, and manage secrets | DevOps Role | Live Azure URL |
| **GitHub Actions** | Configure multi-stage CI/CD pipeline (Build → Test → Dockerize → Deploy to Azure) | DevOps Role | `Workflow.yml` & Runs |
| **Selenium & JMeter** | Automate E2E browser test suites and execute API performance benchmark tests | QA Role | QA Test Reports |

---

## 📐 Repository Structure

```
EcoTrack/
│
├── .gitignore                          # Ignore rules for VS, .NET, Node, Docker, secrets
├── README.md                           # This file — project overview
├── LICENSE                             # Project license
├── EcoTrack.slnx                       # Solution file (all 4 projects)
│
├── src/                                # ─── All 4 Microservices ───
│   ├── Identity/
│   │   ├── IdentityService.csproj      # .NET 8 Web API project
│   │   ├── Program.cs                  # Entry point, DI, middleware
│   │   ├── appsettings.json             # Configuration (DB, Kafka, JWT)
│   │   ├── appsettings.Development.json
│   │   ├── IdentityService.http        # HTTP request samples
│   │   └── Properties/
│   │       └── launchSettings.json
│   │
│   ├── Logistics/
│   │   ├── LogisticsService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── LogisticsService.http
│   │   └── Properties/
│   │       └── launchSettings.json
│   │
│   ├── Marketplace/
│   │   ├── MarketplaceService.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── MarketplaceService.http
│   │   └── Properties/
│   │       └── launchSettings.json
│   │
│   └── Analytics/
│       ├── AnalyticsService.csproj     # ← Your microservice (Ekanayake)
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── AnalyticsService.http
│       └── Properties/
│           └── launchSettings.json
│
├── apps/
│   └── web/                            # ─── React Frontend ───
│       ├── package.json
│       ├── src/
│       │   ├── components/
│       │   ├── pages/
│       │   └── services/
│       └── public/
│
├── scripts/                            # ─── MySQL Initialization SQL ───
│   ├── init-identity.sql               # ecotrack_identity_db schema
│   ├── init-logistics.sql              # ecotrack_logistics_db schema
│   ├── init-marketplace.sql            # ecotrack_marketplace_db schema
│   └── init-analytics.sql              # ecotrack_analytics_db schema
│
├── docs/                                # ─── Documentation ───
│   └── (SRS, architecture docs, etc.)
│
└── .github/
    └── workflows/                       # ─── CI/CD ───
        ├── ci.yml                       # GitHub Actions CI pipeline
        └── (CD workflows — Sprint 3+)
```

---

## 🔄 4-Sprint Implementation Plan

| Sprint | Core Objectives | Deliverables | QA / DevOps Output |
|--------|---------------|--------------|:------------------:|
| **Sprint 1** (DevOps: Ekanayake) | Setup Git repo, Docker baseline (Services + Kafka + Zookeeper), API Gateway, 4 MySQL schemas, Docker Compose, Working Auth API, Pickup Booking UI, GitHub Actions CI pipeline, xUnit baseline | Working Auth API, Pickup Request MVP, Docker Compose, CI pipeline | GitHub Actions CI build pipeline, xUnit baseline |
| **Sprint 2** (DevOps: Wijewardhana) | Implement Driver Logistics assignment, Item Scrap Valuation calculator, Refurbished Listing catalog. Connect initial Kafka event publishers | Logistics Dispatch flow, Product Listing UI, Kafka containers | Integration test suites, Docker staging containers, Kafka Producer tests |
| **Sprint 3** (DevOps: Panapitiya) | Implement Marketplace Checkout, Order Impact Analytics calculation flow, Carbon engine, Safe Disposal Certificate PDF engine, Azure staging deployment | Checkout flow, Dashboard, Kafka Consumer Group, Azure CD deployment | Azure CD deployment, JMeter load baseline |
| **Sprint 4** (DevOps: Rajapaksha) | Platform-wide integration, end-to-end testing, security hardening, multi-service Prometheus/Grafana monitoring setup, dynamic report exports, documentation finalization | Fully deployed multi-service platform, Prometheus dash, Selenium E2E suite | Selenium E2E suite, Prometheus/Grafana dash, documentation |

---

## 🔄 Role Rotation (4 Sprints)

| Sprint | Rajapaksha | Panapitiya | Wijewardhana | **Ekanayake (You)** |
|--------|-----------|-----------|-------------|---------------------|
| **Sprint 1** | Business Analyst (BA) | Developer (Dev) | QA Engineer (QA) | **DevOps Engineer** |
| **Sprint 2** | Developer (Dev) | QA Engineer (QA) | DevOps Engineer | **Business Analyst (BA)** |
| **Sprint 3** | QA Engineer (QA) | DevOps Engineer | Business Analyst (BA) | **Developer (Dev)** |
| **Sprint 4** | DevOps Engineer | Business Analyst (BA) | Developer (Dev) | **QA Engineer (QA)** |

**Definition of Done (per User Story):**
- User story satisfies all documented acceptance criteria
- API endpoints documented and testable in Swagger UI
- Unit test coverage ≥ 70% for new code paths
- Kafka event producers and consumers pass integration tests
- Pull Request passes GitHub Actions automated CI build and test execution
- Feature deployed and verified in Dockerized staging environment
- All associated CRUD operations implemented via parameterized ADO.NET SQL

---

## ⚠️ Risk Assessment

| Identified Risk | Impact Level | Mitigation Strategy |
|----------------|:-----------:|---------------------|
| Kafka Broker Outage in Staging | High | Use Docker Compose health-checks and resilient retry policies with dead-letter queue topics |
| Cross-Service Data Coupling | **Critical** | Enforce strict physical database schema isolation; prohibit direct cross-schema SQL queries |
| Duplicate Event Processing | High | Incorporate unique eventId idempotency tokens on all Kafka consumer handlers |
| Deployment Delays | Medium | Establish Docker Compose configuration and GitHub Actions CI pipelines starting in Sprint 1 |

---

## 📁 Key Files & Resources

| File/Resource | Description |
|---------------|-------------|
| `README.md` | This file — project overview and architecture |
| `EcoTrack.slnx` | Visual Studio solution file (all 4 microservice projects) |
| `src/Identity/IdentityService.csproj` | Identity microservice project file |
| `src/Logistics/LogisticsService.csproj` | Logistics microservice project file |
| `src/Marketplace/MarketplaceService.csproj` | Marketplace microservice project file |
| `src/Analytics/AnalyticsService.csproj` | **Analytics microservice project file (your service)** |
| `scripts/init-*.sql` | MySQL DDL scripts for all 4 database schemas |
| `.github/workflows/ci.yml` | GitHub Actions CI pipeline (Build → Test → Docker) |
| `docs/` | Documentation folder (SRS PDF, architecture diagrams) |

---

## 🚀 Quick Start

### Prerequisites
- **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** — [download](https://nodejs.org/)
- **Docker + Docker Compose** — [download](https://www.docker.com/products/docker-desktop/)
- **git** — for version control

### Clone & Build

```bash
# Clone the repository
git clone https://github.com/your-github-username/EcoTrack.git
cd EcoTrack

# Restore dependencies
dotnet restore

# Build all microservices
dotnet build

# Start the frontend (when ready)
cd apps/web
npm install
npm start
```

### Run with Docker Compose (when ECO-56 is complete)

```bash
docker compose up -d

# Access services:
# Identity:   http://localhost:5001/swagger
# Logistics:  http://localhost:5002/swagger
# Marketplace: http://localhost:5003/swagger
# Analytics:  http://localhost:5004/swagger
# Frontend:   http://localhost:3000
```

---

## 📌 Project Info

- **Course:** SE3022 — Case Study Project
- **Academic Year:** Year 3, Semester 1, 2026
- **Group:** 34
- **Document Version:** 1.0 (SRS)
- **Architecture:** Event-Driven Microservices with Apache Kafka
- **Cloud Target:** Microsoft Azure App Services
- **CI/CD:** GitHub Actions

---

*For questions, contact your team or refer to the Jira board.*
