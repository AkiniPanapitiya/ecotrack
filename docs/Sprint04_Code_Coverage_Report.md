# SE3022 · CASE STUDY PROJECT
BSc (Hons) in Computer Science · Year 3 Semester 1 · 2026
Sprint 04
Code Coverage Report

* **QA Engineer:** Dilanka Dineth (IT24610796)
* **Sprint:** Sprint 04
* **Report Date:** 09.10.2026

---

## 1. Executive Summary

This report records the code coverage analysis for all Sprint 4 unit tests across the EcoTrack microservices. Code coverage was collected using .NET's XPlat Code Coverage adapter and reported using ReportGenerator.

### 1.1 Coverage Summary Table

| Service | Test Project | Line Coverage | Branch Coverage | Total Lines | Covered Lines | Uncovered Lines | Status |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Identity Service** | IdentityService.Tests | 13.6% | 6.3% | 4,364 | 594 | 3,770 | ⚠️ Low |
| **Marketplace Service** | MarketplaceService.Tests | 0% | 0% | TBD | 0 | TBD | ❌ None |
| **Logistics Service** | LogisticsService.Tests | TBD | TBD | TBD | TBD | TBD | ⚠️ Not Run |

### 1.2 Key Findings

- **Identity Service** has **13.6% line coverage** and **6.3% branch coverage** - the test suite exercises only a small fraction of the codebase
- **Critical uncovered areas** in Identity Service:
  - `Identity/Program.cs` Main method (0% coverage - 14 complexity)
  - All Controller and Repository methods untested
  - BcryptPasswordHasher methods untested (security-critical)
- **Marketplace Service** has **0% coverage** - no unit tests are currently run with coverage collection
- **Logistics Service** coverage not yet collected

---

## 2. Detailed Coverage by Service

### 2.1 Identity Service (`IdentityService.Tests`)

**Coverage Metrics:**
- **Line Coverage:** 13.6% (594 / 4,364 lines covered)
- **Branch Coverage:** 6.3% (114 / 1,804 branches covered)
- **Total Methods:** Multiple controllers, repositories, and services

**Uncovered High-Priority Areas:**

| File | Lines | Complexity | Coverage | Reason |
| :--- | :---: | :---: | :---: | :--- |
| `Identity/Program.cs` | 1-14 | 14 | 0% | Main method - not tested |
| `Identity/Controllers/` | All controllers | varies | 0% | API endpoints not tested |
| `Identity/Repositories/` | All repositories | varies | 0% | Data access not tested |
| `Identity/Services/BcryptPasswordHasher.cs` | All methods | varies | 0% | **Security-critical** - password hashing not tested |
| `Identity/Services/AuthService.cs` | All methods | varies | 0% | Authentication not tested |
| `Identity/DTOs/` | All DTOs | varies | 0% | Data transfer objects not tested |

**Uncovered Methods Summary:**
- Password hashing and verification (security critical)
- JWT token generation and validation
- User registration and login flows
- KYC document verification flows
- Role and permission management

### 2.2 Marketplace Service (`MarketplaceService.Tests`)

**Coverage Metrics:**
- **Line Coverage:** 0% - no coverage data collected
- **Branch Coverage:** 0% - no branch coverage data
- **Test Status:** Tests pass but coverage not instrumented

**Issue:** Coverage adapter not configured for this project

### 2.3 Logistics Service (`LogisticsService.Tests`)

**Coverage Metrics:** Not yet collected

---

## 3. Uncovered Areas & Follow-up Subtasks

### 3.1 High Priority (Must Fix)

| Subtask | Area | Impact | Owner | Target Sprint |
| :--- | :--- | :--- | :--- | :--- |
| **ECO-172** | Identity `BcryptPasswordHasher` | **Security** - Password hashing not tested | QA / Developer | Sprint 5 |
| **ECO-173** | Identity `AuthService` | **Authentication** - Login/logout not tested | QA / Developer | Sprint 5 |
| **ECO-174** | Identity `UserRepository` | **Data** - User CRUD not tested | QA / Developer | Sprint 5 |
| **ECO-175** | Identity `RoleService` | **Authorization** - Role management not tested | QA / Developer | Sprint 5 |

### 3.2 Medium Priority

| Subtask | Area | Impact | Owner | Target Sprint |
| :--- | :--- | :--- | :--- | :--- |
| **ECO-176** | Identity `KycService` | **Compliance** - KYC flow not tested | QA / Developer | Sprint 5 |
| **ECO-177** | Marketplace `OrderService` | **Business** - Order flow not tested | QA / Developer | Sprint 5 |
| **ECO-178** | Logistics `PickupService` | **Operations** - Pickup flow not tested | QA / Developer | Sprint 5 |

### 3.3 Low Priority

| Subtask | Area | Impact | Owner | Target Sprint |
| :--- | :--- | :--- | :--- | :--- |
| **ECO-179** | All Controllers | **API** - Endpoint-level testing | QA / Developer | Sprint 5 |
| **ECO-180** | All Models | **Data** - Model validation not tested | QA / Developer | Sprint 5 |

---

## 4. Recommendations

1. **Immediate:** Add unit tests for `BcryptPasswordHasher` - this is a **security-critical** component that must have test coverage
2. **Short-term:** Implement unit tests for Controller and Repository patterns in all services
3. **Medium-term:** Achieve minimum 80% line coverage target for all services
4. **Long-term:** Integrate coverage gates in CI/CD pipeline to prevent regression

### 4.1 Minimum Coverage Standards

| Service | Min Line Coverage | Min Branch Coverage | Status |
| :--- | :---: | :---: | :--- |
| **Identity Service** | 80% | 70% | ⚠️ Currently 13.6% / 6.3% |
| **Marketplace Service** | 80% | 70% | ❌ Currently 0% / 0% |
| **Logistics Service** | 80% | 70% | ⚠️ Not yet collected |

---

## 5. Coverage Dashboard

![Coverage Dashboard](tests/IdentityService.Tests/Coverage/HtmlReport/index.html)

*(Note: HTML report generated at `tests/IdentityService.Tests/Coverage/HtmlReport/index.html`)*

---

## 6. Conclusion

Sprint 4 unit test coverage is **inadequate** for production-quality software. The Identity Service, which handles **authentication, authorization, and security**, has critically low coverage. Immediate action is required to add tests for:

1. Password hashing (`BcryptPasswordHasher`)
2. Authentication flows (`AuthService`)
3. User management (`UserRepository`)
4. Role management (`RoleService`)

Without these tests, the system risks **security vulnerabilities** and **undefined behavior** in production.

---

*Code Coverage Report generated for Group 34 Eco Track Sprint 04.*

**Note:** Coverage collection was performed using .NET's XPlat Code Coverage adapter. Report was generated using ReportGenerator. All test projects must have the coverage collector configured in their `.csproj` files for future runs.