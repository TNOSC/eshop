# Task 07 — Auth

**Goal:** OIDC code flow against Keycloak, a cookie session on the web host, roles that survive into WASM,
and the realm/AppHost changes that make it possible.

**Depends on:** [06](06-storefront-catalog.md).

**This is the highest-friction task in the plan.** Four of the top gotchas live here. Read the whole file
before starting.

---

## Files to create — in the host project

```
Tnosc.EShop.Client.Web/
├─ Authentication/
│  ├─ KeycloakRoleClaimsTransformation.cs
│  ├─ CookieRefreshEvents.cs
│  └─ PersistingRevalidatingAuthenticationStateProvider.cs
├─ Options/
│  └─ OidcOptions.cs
├─ Bff/
│  ├─ LoginEndpoint.cs   LogoutEndpoint.cs   UserInfoEndpoint.cs
└─ Extensions/
   └─ WebAuthenticationExtensions.cs   # AddEShopBffAuthentication()
```

In `.Client`:

```
Infrastructure/Auth/
├─ UserInfo.cs                             # (string UserId, string Name, string[] Roles)
└─ PersistentAuthenticationStateProvider.cs
```

## Files to edit

| File | Change |
|---|---|
| `aspire/Tnosc.EShop.AppHost/Program.cs` | `.WithReference(keycloak)` + `.WaitFor(keycloak)` on `eshop-web` |
| `aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json` | redirect URIs, web origins, post-logout URIs, realm-role mapper |
| `Directory.Packages.props` | `Microsoft.AspNetCore.Authentication.OpenIdConnect`, `Microsoft.AspNetCore.Components.Authorization` |
| both `Program.cs` | registration |

---

## Gotcha 1 — `eshop-web` cannot resolve Keycloak

The AppHost registers the web app with a reference to the API only:

```csharp
builder.AddProject<Projects.Tnosc_EShop_Client_Web>(name: "eshop-web")
    .WithReference(source: eshopHost)
    .WithReference(source: keycloak)      // ← ADD
    .WaitFor(dependency: eshopHost)
    .WaitFor(dependency: keycloak)        // ← ADD
    .WithExternalHttpEndpoints();
```

Without this, `AddKeycloakOpenIdConnect(serviceName: "keycloak", …)` has nothing to resolve.

## Gotcha 2 — the authority must be browser-reachable, and issuers must match

Service discovery yields the **container-network** address. The API never cared, because JWT validation is
back-channel only. **The BFF redirects the user's browser** to
`{authority}/protocol/openid-connect/auth`, and a browser cannot resolve a container hostname.

In Development, pin it to the fixed host port the AppHost already exposes:

```csharp
if (builder.Environment.IsDevelopment())
{
    options.Authority = "http://localhost:8080/realms/eshop";
    options.RequireHttpsMetadata = false;
}
```

**The knock-on:** tokens minted at that authority carry `iss: http://localhost:8080/realms/eshop`. The
API's `AddKeycloakJwtBearer` must accept the same issuer — if it discovered the container address, every
proxied call fails validation with a 401 that looks like a token bug. The durable fix is
`KC_HOSTNAME=localhost` + `KC_HOSTNAME_PORT=8080` on the Keycloak resource, so it mints **one canonical
issuer** both sides accept. Verify this early; it is the single most confusing failure in the task.

## Gotcha 3 — roles are in the access token only

`aspire/Tnosc.EShop.AppHost/Realms/eshop-realm.json` defines **only** the audience mapper. There is no
realm-role protocol mapper, so Keycloak's built-in `roles` client scope puts `realm_access.roles` in the
**access token** and the **ID token has no roles at all**. `<AuthorizeView Roles="admin">` silently never
matches.

Do both halves:

**(a) Read it from the access token** in `OnTokenValidated`, mirroring
`src/server/Tnosc.EShop.Server.Host/Authentication/KeycloakClaimsTransformation.cs`:

```csharp
internal static class KeycloakRoleClaimsTransformation
{
    private const string RealmAccessClaimType = "realm_access";

    public static Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        string? accessToken = context.TokenEndpointResponse?.AccessToken;
        if (accessToken is null || context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return Task.CompletedTask;
        }

        var token = new JsonWebToken(jwtEncodedString: accessToken);
        if (!token.TryGetPayloadValue(claimType: RealmAccessClaimType, value: out JsonElement realmAccess))
        {
            return Task.CompletedTask;
        }

        foreach (JsonElement role in realmAccess.GetProperty(propertyName: "roles").EnumerateArray())
        {
            identity.AddClaim(claim: new Claim(type: ClaimTypes.Role, value: role.GetString()!));
        }

        return Task.CompletedTask;
    }
}
```

This is a **deliberate duplicate** of the server's class — the web project cannot reference `Server.Host`,
and a shared project would couple the client to the server's composition root. Say so in the class's XML
comment.

**(b) Add the protocol mapper to the realm JSON** while you are editing it anyway — an
`oidc-usermodel-realm-role-mapper` on `eshop-web` with `"id.token.claim": "true"`, `"claim.name": "roles"`,
`"multivalued": "true"`. Then `RoleClaimType = "roles"` would work on its own. Shipping both means the
code path works against an unmodified realm *and* the realm is correct for a fresh environment.

## Gotcha 4 — the realm import is a no-op on an existing volume

`WithRealmImport` only runs when the realm does not already exist, and `keycloakdb` sits on a persisted
data volume — the AppHost's own comments say so. **Editing the JSON alone changes nothing on your machine.**

Three fixes, cheapest first:

1. **Edit in the admin console** (recommended to unblock). AppHost running → `http://localhost:8080` →
   log in with the Aspire-generated admin credentials shown on the dashboard → realm `eshop` → Clients →
   `eshop-web` → paste the new Valid redirect URIs / Web origins / Valid post logout redirect URIs → Save.
   **Still commit the JSON edit**, so a fresh environment is correct.
2. **Delete just the realm.** Admin console → realm `eshop` → Realm settings → Action ▾ → Delete. Restart
   the AppHost; `--import-realm` re-runs and re-seeds the realm, both users and the mappers. Leaves
   `eshopdb` untouched.
3. **Drop the Postgres data volume** — a clean slate that **also destroys `eshopdb`** (products, orders,
   outbox):

```bash
# Stop the AppHost first (Ctrl+C), then make sure the container is gone.
docker ps -a --filter "name=postgres" --format "{{.ID}}\t{{.Names}}"
docker rm -f <container-id>

docker volume ls --format "{{.Name}}" | grep -i postgres
docker volume rm <volume-name>

dotnet run --project aspire/Tnosc.EShop.AppHost
```

### The realm edit itself

`eshop-web` today lists only the **API host's** ports (7257/5053, added for Scalar). Add the Blazor app's
own origin, from
`src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/Properties/launchSettings.json`:

```json
"redirectUris": [
  "https://localhost:7257/*", "http://localhost:5053/*",
  "https://tnosc-eshop-client-web.dev.localhost:7278/*",
  "http://tnosc-eshop-client-web.dev.localhost:5257/*"
],
"webOrigins": [
  "https://localhost:7257", "http://localhost:5053",
  "https://tnosc-eshop-client-web.dev.localhost:7278",
  "http://tnosc-eshop-client-web.dev.localhost:5257"
],
"attributes": {
  "pkce.code.challenge.method": "S256",
  "post.logout.redirect.uris": "https://localhost:7257/*##http://localhost:5053/*##https://tnosc-eshop-client-web.dev.localhost:7278/*##http://tnosc-eshop-client-web.dev.localhost:5257/*"
}
```

Add `https://localhost:7278/*` too if you ever `dotnet run` the web project standalone.

---

## Registration

```csharp
builder.Services.AddAuthentication(configureOptions: options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(configureOptions: options =>
    {
        options.Cookie.Name = "eshop.bff";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;   // Lax, NOT Strict — the OIDC callback is a cross-site GET
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(value: 8);
        options.SlidingExpiration = false;            // refresh tokens, not sliding cookies
        options.EventsType = typeof(CookieRefreshEvents);
    })
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: oidcOptions.Realm,
        configureOptions: options =>
        {
            options.ClientId = oidcOptions.ClientId;   // "eshop-web"
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;                    // eshop-web is a PUBLIC client — there is no secret
            options.SaveTokens = true;                 // required by ServerAccessTokenHandler and the proxy
            options.MapInboundClaims = false;
            options.Scope.Clear();
            options.Scope.Add(item: "openid");
            options.Scope.Add(item: "profile");
            options.Scope.Add(item: "email");
            options.Scope.Add(item: "offline_access"); // required by the refresh flow below
            options.TokenValidationParameters.NameClaimType = "preferred_username";
            options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
            options.Events.OnTokenValidated = KeycloakRoleClaimsTransformation.OnTokenValidatedAsync;
        });
```

`SameSite=Lax` is not a weakening — `Strict` would drop the cookie on the return leg from Keycloak and the
login would loop forever.

`OidcOptions` binds via `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` and is
unwrapped to a plain singleton, per
[`.claude/rules/configuration-options.md`](../.claude/rules/configuration-options.md). No consumer takes
`IConfiguration` or `IOptions<T>`.

---

## Token refresh

Realm `accessTokenLifespan` is 1800s. Refresh in `CookieAuthenticationEvents.OnValidatePrincipal`, before
any request reaches the proxy:

- read `expires_at` from `context.Properties.GetTokenValue("expires_at")`;
- if `now + 2 minutes < expiresAt`, return;
- otherwise POST `grant_type=refresh_token` to the realm token endpoint (discovered via
  `oidcOptions.ConfigurationManager.GetConfigurationAsync`);
- on success, `context.Properties.StoreTokens(newTokens)` and `context.ShouldRenew = true`;
- on failure, `context.RejectPrincipal()` and `SignOutAsync` — a dead session must not linger.

**If refresh-token rotation turns out to be painful**, `Duende.AccessTokenManagement.OpenIdConnect`
(Apache-2.0) handles it: `AddOpenIdConnectAccessTokenManagement()` and swap `GetTokenAsync("access_token")`
for `HttpContext.GetUserAccessTokenAsync()`. That is a ~10-line change confined to `BffProxy` and
`ServerAccessTokenHandler`. Start hand-rolled; swap only if it bites.

---

## Login / logout

```csharp
// GET /bff/login — AllowAnonymous
TypedResults.Challenge(
    properties: new AuthenticationProperties { RedirectUri = GetSafeReturnUrl(returnUrl: returnUrl) },
    authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]);

// POST /bff/logout — keeps antiforgery ON
TypedResults.SignOut(
    properties: new AuthenticationProperties { RedirectUri = "/" },
    authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme,
                            OpenIdConnectDefaults.AuthenticationScheme]);
```

- **`GetSafeReturnUrl` must reject absolute URLs** — otherwise `/bff/login?returnUrl=https://evil.example`
  is an open redirect.
- **Logout is `POST` with an antiforgery token**, so a cross-site `<img src="/bff/logout">` cannot log the
  user out. `LoginDisplay.razor` renders
  `<form method="post" action="bff/logout"><AntiforgeryToken /></form>`.

---

## Flowing auth into WASM

Both halves of the standard Blazor Web App pair are required:

- **Host:** `PersistingRevalidatingAuthenticationStateProvider` — revalidates every 30 min and, on
  `RegisterOnPersisting`, writes `UserInfo(UserId, Name, Roles)` into `PersistentComponentState`.
- **WASM:** `PersistentAuthenticationStateProvider` — reads it back and builds a fixed
  `AuthenticationState` with `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, and **one `ClaimTypes.Role`
  claim per entry in `Roles`**.

```csharp
// .Client/Program.cs
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
```

> ⚠️ **Persisting the roles is what makes `<AuthorizeView Roles="admin">` work after WASM takeover.**
> Miss it and the admin nav appears during prerender, then vanishes the moment the app becomes
> interactive — which reads like a rendering bug, not a claims bug.

---

## Definition of done

- [ ] `/bff/login` redirects to Keycloak and returns to the app signed in.
- [ ] `admin@eshop.local` / `Passw0rd!` sees the "Administration" nav section; `customer@eshop.local` does not.
- [ ] **The admin nav is still correct after WASM attaches** — wait for the interactive switch and re-check.
      This is the persisted-role-claim proof.
- [ ] Logout signs out of both the cookie and Keycloak, and returns to `/`.
- [ ] An expired access token is refreshed transparently (shorten `accessTokenLifespan` in the console to
      test in minutes rather than half-hours).
- [ ] `/products` still works **anonymously** — auth must not have broken the storefront.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
