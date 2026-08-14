# ADR-015: Authorization — Roles In Keycloak, Permissions As Code Constants

## Status

Accepted

## Date

2026-08-14

## Context

Keycloak is the solution's only identity provider and owns sign-up, login, credentials and realm roles.
Endpoints need finer-grained authorization than the two coarse realm roles (`admin`, `customer`) provide —
e.g. distinguishing "who may adjust stock" from "who may create a product." That finer vocabulary can live
in Keycloak (as additional realm roles or a Keycloak-side permission system) or in the codebase.

## Decision

Keycloak owns the two coarse realm roles and who holds them. This codebase owns the fine-grained
permission vocabulary (`Permissions.Catalog.Write`, etc.) and the role → permission map
(`RolePermissions`), both as compile-time constants in `Server.Shared/Authorization/`. Endpoints name a
permission via `.HasPermission(Permissions.X.Write)` — never a role, never a string literal. A
`PermissionAuthorizationPolicyProvider` materializes an ASP.NET Core policy per permission on demand;
`KeycloakClaimsTransformation` expands a Keycloak `realm_access` role into its mapped permissions as
claims, and `PermissionAuthorizationHandler` checks against those claims. `me`-style endpoints resolve the
caller from `IUserContext`, never from a route or body identifier, so there is no ownership check anywhere
in a handler — `NoBusinessBranchingTests` would reject one there anyway (ADR-005).

## Rationale

- **Adding a permission must not mean editing a realm.** If endpoints named realm roles directly, every
  new distinction would require a Keycloak change, an import and a redeploy in lockstep. With the map in
  code, a permission is a `const` and a compiler error when misspelled; the realm keeps the two coarse
  roles it started with.
- **The two halves of the authorization contract cannot see each other**, structurally: `Server.Api`
  names a permission via `HasPermission(...)`, `Server.Host`'s claims transformation grants it — neither
  project references the other. A string literal independently typed on each side is two literals that
  can silently diverge; a typo there doesn't fail the build, it fails at runtime as a 403 that reads like
  a permissions bug rather than a spelling mistake. This is the same failure shape ADR-013 solves for
  cache tags, applied to authorization.
- **`Roles.Admin`/`Roles.Customer` are the one asymmetric case** — unlike a permission constant or a cache
  tag, they cannot be freely renamed, because each must equal a realm role name in the AppHost's realm
  export file. Renaming one side alone makes `RolePermissions.For(role)` silently return empty.
- **Ownership as structure, not a check.** A `me` endpoint that takes the caller's id from `IUserContext`
  and passes it as data makes "can this customer address another customer's profile" impossible to
  express, rather than something a handler must remember to check — the same "put the invariant where it
  can't be forgotten" reasoning as ADR-005's ban on business branching.
- Alternative rejected: naming Keycloak roles directly in endpoint authorization — rejected for the
  tight-coupling and silent-403 failure mode above; alternative rejected: an ownership `if` check inside
  each `me` handler — rejected as exactly the business branching ADR-005 forbids, and unnecessary once the
  caller id is structurally the only id a `me` command can carry.

## Consequences

**Easier:**
- A new permission is a `const string` and a compiler-checked reference at every call site — no realm
  change, no redeploy of Keycloak configuration, for a new fine-grained distinction.
- The 401 (unauthenticated) vs. 403 (authenticated, unpermitted) distinction falls out of the policy chain
  automatically (`RequireAuthenticatedUser().AddRequirements(...)`), rather than being hand-coded per
  endpoint.

**Harder:**
- Two independent literals (`Server.Api`'s permission name and `Server.Host`'s role → permission mapping)
  must be kept conceptually in sync by a human, even though each individually is compiler-checked against
  its own project — `grep -rn 'HasPermission("'` returning nothing is the mechanical check that no literal
  slipped through.
- `Roles.Admin`/`Roles.Customer` cannot be renamed independently of the Keycloak realm export — a rename
  on one side alone silently grants nothing, and nothing catches that at compile time.
