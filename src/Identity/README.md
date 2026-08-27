# EcoTrack - Identity & Access Management Service (Auth Service)

**Service Owner**: Akini Panapitiya (IT24610790)  
**Sprint**: Sprint 1 (SE3022 Case Study Project)  
**Assigned Scope**: ECO-12 (E1.1), ECO-13 (E1.2), ECO-14 (E1.3), Dynamic Audit Report  
**Framework**: ASP.NET Core .NET 10 (`net10.0`)  
**Data Access**: 100% Direct ADO.NET (Zero-ORM policy, `MySqlCommand` parameterized queries)  
**Database**: `ecotrack_identity_db` (MySQL 8.0)  

---

## 1. Service Overview
The **Identity & Access Management Service** provides secure, role-based authentication and profile management for the EcoTrack platform. It handles:
- Multi-tenant registration for **Users** and **Recyclers** with BCrypt salted password hashing.
- Default `Pending` verification status assignment for Recycler business accounts.
- JWT Bearer token issuance containing claims for `UserId`, `Email`, `Role`, `FullName`, and `VerificationStatus`.
- Role-Based Access Control (RBAC) across endpoints.
- User profile retrieval and inline validation for profile updates.
- Real-time audit activity logging and dynamic reporting for registration/login audit trails.

---

## 2. API Endpoints Catalog

### Authentication (`/api/auth`)
| Method | Endpoint | Description | Auth Required | Status Codes |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | Register a new User or Recycler account | None | `201 Created`, `400 Bad Request`, `409 Conflict` |
| `POST` | `/api/auth/login` | Authenticate with email and password | None | `200 OK`, `400 Bad Request`, `401 Unauthorized` |

### Profile Management (`/api/profile`)
| Method | Endpoint | Description | Auth Required | Status Codes |
|---|---|---|---|---|
| `GET` | `/api/profile` | Fetch authenticated user and recycler profile details | Bearer JWT | `200 OK`, `401 Unauthorized`, `404 Not Found` |
| `PUT` | `/api/profile` | Update contact information (Name, Phone, Address) | Bearer JWT | `200 OK`, `400 Bad Request`, `401 Unauthorized` |

### Dynamic Reporting & Audit (`/api/audit`)
| Method | Endpoint | Description | Auth Required | Status Codes |
|---|---|---|---|---|
| `GET` | `/api/audit/report` | Query User Audit & Registration Activity Log | Bearer JWT | `200 OK`, `401 Unauthorized` |

### Health Check (`/health`)
| Method | Endpoint | Description | Auth Required | Status Codes |
|---|---|---|---|---|
| `GET` | `/health` | Service health status check | None | `200 OK` |

---

## 3. Database Schema (`ecotrack_identity_db`)

### Core Tables Owned:
1. **`Users`**: `Id` (CHAR 36), `FullName`, `Email` (UNIQUE), `PasswordHash`, `Role`, `PhoneNumber`, `Address`, `IsActive`, `CreatedAt`, `UpdatedAt`.
2. **`RecyclerProfiles`**: `Id`, `UserId` (FK), `CompanyName`, `BusinessRegistrationNumber`, `FacilityAddress`, `OperationalCapacityKg`, `VerificationStatus` (`Pending`/`Approved`/`Rejected`), `CreatedAt`, `UpdatedAt`.
3. **`UserAuditLogs`**: `Id`, `UserId`, `UserEmail`, `Action`, `Role`, `Details`, `IpAddress`, `Timestamp`.
4. **`KycDocuments`**: `Id`, `UserId` (FK), `DocumentType`, `DocumentUrl`, `VerificationStatus`, `UploadedAt`, `ReviewedAt`.
5. **`UserFeedback`**: `Id`, `UserId` (FK), `RecyclerId` (FK), `Rating`, `Comments`, `IsFlagged`, `CreatedAt`.

Migration script: `database/migrations/01_init_identity_db.sql`.

---

## 4. Environment Variables & Configuration (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "IdentityDb": "Server=localhost;Port=3306;Database=ecotrack_identity_db;User=root;Password=ecotrack_root_pwd;AllowUserVariables=True;UseAffectedRows=False;"
  },
  "Jwt": {
    "SecretKey": "EcoTrack_Super_Secret_Key_For_Jwt_Auth_2026_SE3022_CaseStudy!",
    "Issuer": "EcoTrack.IdentityService",
    "Audience": "EcoTrack.ClientApps",
    "ExpiryMinutes": 180
  }
}
```

---

## 5. Local Setup & Execution Guide

### Prerequisites:
- .NET 10 SDK (`dotnet --version` >= 10.0)
- MySQL 8.0 instance running on port 3306 with `ecotrack_identity_db` provisioned

### Steps:
1. **Initialize Database**:
   ```bash
   mysql -u root -p < database/migrations/01_init_identity_db.sql
   ```
2. **Restore & Build Service**:
   ```bash
   dotnet restore services/IdentityService/IdentityService.csproj
   dotnet build services/IdentityService/IdentityService.csproj
   ```
3. **Run Service**:
   ```bash
   dotnet run --project services/IdentityService/IdentityService.csproj --urls "http://localhost:5001"
   ```
4. **Access Swagger UI**:
   Open browser at: `http://localhost:5001/swagger`

5. **Run xUnit Test Suite**:
   ```bash
   dotnet test tests/IdentityService.Tests/IdentityService.Tests.csproj
   ```
