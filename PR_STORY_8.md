## Story 8: Role & Permission Management

### Summary
Implements user role viewing and management for EcoTrack. Admins can view all users and their current roles, and change any user's role (User / Recycler / Admin / Driver). Role-based access control is enforced on both frontend and backend: admin-only routes are blocked for non-admins with an "Access Denied" page.

---

### Subtasks completed

- **Subtask 1 — Service + Repository**: Added `UpdateRoleAsync` and `GetAllUsersAsync` to `IUserRepository` / `UserRepository`. New `RoleService` with `GetAllUsersAsync` and `ChangeUserRoleAsync` (validates role, checks user exists, audits changes).

- **Subtask 2 — DTOs**: New `RoleDtos.cs` with `UserListDto`, `ChangeRoleRequestDto`, `ChangeRoleResponseDto`.

- **Subtask 3 — API**: New `RoleController` (`api/admin/*`, Admin-only via `[Authorize(Roles = "Admin")]`):
  - `GET /api/admin/users` — list all users with roles
  - `PUT /api/admin/users/{userId}/role` — change a user's role

- **Subtask 4 — DI**: Registered `IRoleService` → `RoleService` in `Program.cs`.

- **Subtask 5 — Frontend UI: Admin user list**: New `AdminUsersView.jsx` at `/admin/users` — table showing all users with name, email, current role (dropdown to change), active status, joined date.

- **Subtask 6 — Frontend UI: Access Denied page**: New `AccessDeniedView.jsx` at `/access-denied` — shown when a logged-in user tries to access a page they don't have permission for.

- **Subtask 7 — Frontend routing + access control**: 
  - Added `/admin/users` and `/access-denied` routes to `App.jsx`
  - Updated `ProtectedRoute.jsx` to redirect role-blocked users to `/access-denied` instead of silently bouncing to `/dashboard`
  - Added "User Management" link to Navbar for Admin users

- **Subtask 8 — Frontend API client**: Added `getUsers()` and `changeUserRole()` to `kycApi.js`.

- **Subtask 9 — Unit tests**: 13 `RoleServiceTests` covering: list all users, valid role change, same-role rejection, invalid role rejection (4 cases), user not found, all valid roles accepted (3 cases), admin self-demotion behavior.

---

### Acceptance Criteria mapping

| Scenario | How it's covered |
|---|---|
| **Scenario 1 — View Roles** | `GET /api/admin/users` returns all users with their current role. Admin UI at `/admin/users` shows the full list. |
| **Scenario 2 — Change a Role** | `PUT /api/admin/users/{id}/role` with `{ "newRole": "Recycler" }` updates the DB and returns previous/new role. UI shows a dropdown per user. After change, the user sees their new role-based features on next login (JWT refreshed). |
| **Scenario 3 — Blocked Action** | `ProtectedRoute` checks `rolesAllowed` against the user's role. If mismatched, redirects to `/access-denied` page showing "Access Denied" message. Backend also enforces via `[Authorize(Roles = "Admin")]` on the RoleController. |

---

### How to test

**Scenario 1 — View Roles (Admin):**
1. Login as an **Admin** at `/login`
2. Navigate to **User Management** in the navbar (or go to `/admin/users`)
3. You should see a table with all users, their names, emails, current roles, active status, and join date

**Scenario 2 — Change a Role (Admin):**
1. On `/admin/users`, find a user with role "User"
2. Use the dropdown in the Role column to select "Recycler"
3. Click the dropdown — the change is sent immediately
4. Green success message appears: "Role updated successfully."
5. The table row updates to show "Recycler"
6. Log in as that user — they now see Recycler features (KYC page, Schedule Management) in the navbar

**Scenario 3 — Blocked Action (Non-Admin):**
1. Login as a regular **User** (not Admin)
2. Try to navigate to `/admin/users` directly in the browser address bar
3. You should be redirected to `/access-denied` showing the "Access Denied" page
4. Try to navigate to `/kyc-admin` — same result

**Prerequisites:**
- Identity service running: `http://localhost:5001/identity/health` returns healthy
- Frontend running: `http://localhost:5173`

---

### Key files changed

| File | Description |
|---|---|
| `src/Identity/Repositories/UserRepository.cs` | Added `UpdateRoleAsync` + `GetAllUsersAsync` |
| `src/Identity/Services/RoleService.cs` | New — business logic for role management + audit logging |
| `src/Identity/DTOs/RoleDtos.cs` | New — `UserListDto`, `ChangeRoleRequestDto`, `ChangeRoleResponseDto` |
| `src/Identity/Controllers/RoleController.cs` | New — `GET /api/admin/users`, `PUT /api/admin/users/{id}/role` |
| `src/Identity/Program.cs` | Registered `IRoleService` |
| `src/apps/web/src/views/AdminUsersView.jsx` | New — admin user management page |
| `src/apps/web/src/views/AccessDeniedView.jsx` | New — access denied page (Scenario 3) |
| `src/apps/web/src/components/ProtectedRoute.jsx` | Updated — redirects to `/access-denied` on role mismatch |
| `src/apps/web/src/components/Navbar.jsx` | Added "User Management" link for Admins |
| `src/apps/web/src/App.jsx` | Added `/admin/users` and `/access-denied` routes |
| `src/apps/web/src/services/kycApi.js` | Added `getUsers()` and `changeUserRole()` |
| `tests/IdentityService.Tests/RoleServiceTests.cs` | New — 13 service-layer unit tests |

---

### Notes
- No new database tables needed — `Users.Role` column already exists.
- Role change updates the DB. The JWT still has the old role until next login. Frontend updates local user state immediately so the UI reflects the new role right away in the table. For full role-based UI changes, the user should log out and back in.
- Valid roles: `User`, `Recycler`, `Admin`, `Driver`.
- `ProtectedRoute` now redirects role-blocked users to `/access-denied` instead of `/dashboard` — this gives the user clear feedback (Scenario 3).
