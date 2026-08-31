---
description: "Coarse roles live in Keycloak, fine-grained permissions live in code as constants; the policy-provider chain behind HasPermission"
applyTo: "src/server/**/*.cs,aspire/**/*.json"
---

# Rule — roles live in Keycloak, permissions live in code

**Keycloak owns coarse realm roles and who holds them. This codebase owns the fine-grained permission
vocabulary and the role → permission map.** An endpoint names a permission; nothing in `Server.*` ever
names a role, and nothing in `Server.*` ever writes to Keycloak.

| Concern | Owner |
|---|---|
| Sign-up, login, password, password reset, email change | **Keycloak** (`/realms/eshop/account`) |
| Realm roles `admin` / `customer`, and who has them | **Keycloak** — an operator, or the default-role composite |
| Permission names (`catalog:write`, …) and role → permission mapping | **This codebase**, as compile-time constants |
| `Customer` profile: name, phone, addresses | **This codebase**, `Server.Domain/Identity` |
| `Customer.Email` | Keycloak is the source of truth; the local copy is *reconciled*, never edited |

## Why

**Adding a permission must not mean editing a realm.** If endpoints named realm roles directly, every
new distinction (“who may adjust stock, as opposed to creating a product?”) would mean a Keycloak
change, an import, and a redeploy in lockstep. With the map in code, a permission is a constant and a
compiler error when misspelled; the realm keeps the two coarse roles it started with.

**The two halves of the contract cannot see each other.** `Server.Api` *names* a permission via
`HasPermission(...)`; `Server.Host`'s `KeycloakClaimsTransformation` *grants* it by expanding a role
through `RolePermissions`. Neither project references the other. A string literal on each side is two
independent literals, and a typo does not fail the build — it fails at runtime as a **403**, which
reads like a permissions bug rather than a spelling mistake. This is exactly the `cache-tags.instructions.md`
argument, with a worse failure mode.

**One asymmetry to hold on to:** a `[CacheTag]` value is internal to the process and can be renamed
freely. `Roles.Admin` / `Roles.Customer` **cannot** — each must equal a realm role name in
`aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json`. Rename one here alone and
`RolePermissions.For(role)` silently returns empty, so the role grants nothing.

## How

```csharp
// src/server/Tnosc.EShop.Server.Shared/Authorization/
Permissions.Catalog.Write        // const string "catalog:write"  — nested static classes
Roles.Admin                      // const string "admin"          — must match the realm
RolePermissions.For(role)        // FrozenDictionary lookup; empty for an unknown role
```

```csharp
// Server.Api — name the permission, never the role
.HasPermission(permission: Permissions.Catalog.Write);

// any authenticated caller will do
.RequireAuthorization();
```

The chain that makes this work, none of which is optional:

1. `ApiEndpointExtensions.HasPermission(p)` is `RequireAuthorization(policyNames: p)`.
2. Nothing registers a policy per permission, so **`PermissionAuthorizationPolicyProvider`
   materialises one on demand** and memoises it. Without it every such endpoint throws at request time.
3. It builds `RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(p))` — which is what
   yields **401 unauthenticated** versus **403 authenticated-but-unpermitted** rather than collapsing both.
4. `PermissionAuthorizationHandler` succeeds it from the `"permissions"` claim.
5. `KeycloakClaimsTransformation` put those claims there, from `realm_access`.

`Tnosc.Lib.Host/Authorization/` holds 1–4 and is identity-provider agnostic; only the transformation
in `Server.Host/Authentication/` knows what `realm_access` is. Point the host at a different provider
and that one class is what changes.

## Things that are decisions, not oversights

- **Storefront catalogue reads are anonymous.** Only the three Catalog *writes* carry a permission.
- **There is no anonymous write endpoint anywhere.** Sign-up is Keycloak's hosted registration page.
  Self-registration grants the default-role composite, which contains `customer` and never `admin`.
- **No API path grants a role,** and there is no Keycloak Admin REST client. `admin` is assigned by an
  operator in the admin console.
- **No dev token-issuing endpoint.** `directAccessGrantsEnabled` on `eshop-web` gives a password grant
  over `curl` instead.
- **`verifyEmail: false`** in the realm is development-only — enabling it needs an SMTP server the
  AppHost does not provision.
- **`HttpUserContext` is not modified by any of this.** It already reads `ClaimTypes.NameIdentifier`
  (fallback `sub`), `ClaimTypes.Email` (fallback `email`), `ClaimTypes.Role` and `"permissions"`. The
  transformation exists precisely so those four keep working untouched — which is also why
  `MapInboundClaims` stays at its default.

## Ownership is structural, never a check

**A `me` endpoint resolves the caller from `IUserContext.UserId` and passes it into the command or
query as data.** A customer therefore cannot address another customer's profile at all, so there is
nothing to check:

```csharp
// ✅ the caller is the subject; no identifier is accepted from the route or body
public UpdateCustomerProfileCommand ToCommand(IUserContext userContext) =>
    new(ExternalUserId: userContext.UserId, …);

// ❌ never — and NoBusinessBranchingTests would reject it in a handler anyway
if (customer.Id != callerId) return Error.Forbidden(…);
```

Endpoints read `IUserContext`; handlers take the identity as a parameter and stay testable without an
HTTP context. **No endpoint reads `ClaimsPrincipal` directly.** Where a token carries something
`IUserContext` does not expose — the caller's name, for instance — the client passes it in the body
rather than the endpoint reaching past the abstraction.

## Configuration

`KeycloakOptions` (`Realm`, `Audience`, `RequireHttpsMetadata`) binds in
`AuthenticationExtensions.AddKeycloakAuthentication` — the one sanctioned place `IConfiguration` and
`IOptions<T>` are touched, per `configuration-options.instructions.md`, which `ConfigurationTests` enforces against
the Host assembly too. **No authority URL is configured anywhere:** `AddKeycloakJwtBearer` composes it
from the service name and realm, and service discovery resolves it from the AppHost's
`WithReference(keycloak)`.

Pipeline order in `Program.cs` is load-bearing:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestContextMiddleware>();   // AFTER auth — it logs IUserContext.UserId
```

`RequestContextMiddleware` moved after `UseAuthorization()` deliberately; before it, the user id in
its logging scope is always null. Knock-on to expect: `UnitOfWork`'s audit columns stop reading
`"system"` and start carrying the Keycloak subject.

## Checklist

- [ ] The endpoint names a `Permissions.*` constant — no string literal in any `HasPermission(...)`.
- [ ] Any new realm role name added to `Roles` also exists in `eshop-realm.json`, spelled identically.
- [ ] A new role is mapped in `RolePermissions`, or it grants nothing.
- [ ] `me` endpoints take the caller from `IUserContext`; no handler contains an ownership check.
- [ ] Reads that should be public are left anonymous on purpose, and said so in the description.
- [ ] The 401 → 403 → 200 progression is covered by a test that mints a token with a **`realm_access`**
      claim, so the real claims transformation runs.

Verify with `grep -rn 'HasPermission("' --include=*.cs src lib` — it must return nothing.

## Test coverage

`AuthorizationEndpointTests` covers the progression against the real pipeline via `EShopApiFactory`:
no token → 401, a bad signature → 401, a `customer` token against a Catalog write → 403, an `admin`
token → past authorization, and Catalog reads anonymous. `KeycloakClaimsTransformationTests` and
`PermissionAuthorizationPolicyProviderTests` cover the two halves in isolation — including that a
second transformation run adds nothing, and that an unknown role grants nothing.
