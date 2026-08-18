# Task 09 — Error pages

**Goal:** the three surfaces still carrying stock template markup — `/Error`, `/not-found` and the
`#blazor-error-ui` bar — brought into the design system.

**Depends on:** [01](01-design-tokens.md), [03](03-store-shell.md) (both pages render inside
`StoreLayout`). Independent of [04](04-catalog-grid.md)–[08](08-admin-console.md).

Small task, but these are the pages a user sees on the worst day, and right now they look like a
scaffold.

---

## Files to edit

| File | Change |
|---|---|
| `Tnosc.EShop.Client.Web/Components/Pages/Error.razor` | Rewrite content; delete the template's dev-mode block |
| `Tnosc.EShop.Client.Web/Components/Pages/NotFound.razor` | `<h3>` → hero + page |

**Created:** `Error.razor.css`, `NotFound.razor.css`.

Both live in the **host** project, not `.Client`.

---

## Both pages are static SSR — and stay that way

Neither declares a `@rendermode`, and neither should. They render under static SSR, which means **no
`OnClick`, no `IDialogService`, no `INotificationService`**. Anything interactive here would silently do
nothing, exactly like the theme toggle did before [03](03-store-shell.md).

`StoreHero` and everything in [01](01-design-tokens.md) are pure markup and CSS, so both work fine.
`Error.razor` has no `@layout`, so it picks up `DefaultLayout="typeof(StoreLayout)"` from `Routes.razor`
— it already gets the hero-capable shell, and `NotFound.razor` names `StoreLayout` explicitly. Leave
both as they are.

## `Error.razor`

Two problems beyond styling:

1. **`class="text-danger"` is a Bootstrap class and there is no Bootstrap in this solution.** It has
   never done anything. Delete it rather than defining a `.text-danger` rule.
2. **The whole "Development Mode" block ships to production users.** It is stock template text telling
   the *developer* how to set `ASPNETCORE_ENVIRONMENT`, rendered to whoever hit the error. Delete it —
   the environment is not something an end user acts on, and the paragraph reads as a leak.

Keep the `RequestId` mechanism exactly as it is: the `[CascadingParameter] HttpContext`, the
`Activity.Current?.Id ?? HttpContext?.TraceIdentifier` fallback and the `ShowRequestId` guard. It is the
one genuinely useful thing on the page — it is what a user reads back to support, and it correlates with
the `Correlation-Id` the request context already logs.

```razor
@page "/Error"
@using System.Diagnostics
@using Microsoft.AspNetCore.Http

<PageTitle>Error — Tnosc EShop</PageTitle>

<StoreHero Title="Something went wrong" Subtitle="We could not complete that request." />

<div class="eshop-page eshop-error">
    <p>Try again in a moment. If it keeps happening, quote the reference below.</p>

    @if (ShowRequestId)
    {
        <p class="eshop-error-ref">
            <span>Reference</span>
            <code>@RequestId</code>
        </p>
    }

    <a class="eshop-button eshop-button-primary" href="/">Back to the store</a>
</div>
```

`StoreHero` lives in `.Client` (`Layout.Store` namespace); the host project references `.Client`, so the
`@using` resolves. Add it to `Components/_Imports.razor` if it is not already reachable.

```css
.eshop-error { display: flex; flex-direction: column; align-items: flex-start; gap: 1.5rem; }

.eshop-error-ref {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    background: var(--eshop-panel);
    padding: 0.75rem 1rem;
    color: var(--eshop-muted);
    font-size: var(--eshop-body-size);
}

.eshop-error-ref code { color: var(--eshop-ink); }
```

## `NotFound.razor`

```razor
@page "/not-found"
@layout Tnosc.EShop.Client.Web.Client.Layout.Store.StoreLayout

<PageTitle>Not found — Tnosc EShop</PageTitle>

<StoreHero Title="Not found" Subtitle="That page does not exist." />

<div class="eshop-page eshop-notfound">
    <p>The link may be out of date, or the item may have been removed.</p>
    <div class="eshop-notfound-actions">
        <a class="eshop-button eshop-button-primary" href="/products">Browse the catalogue</a>
        <a class="eshop-button eshop-button-secondary" href="/">Back to the store</a>
    </div>
</div>
```

It currently has **no `<PageTitle>`**, so the browser tab shows the raw URL. Add it.

Keep the explicit `@layout` line — this page is reached through `NotFoundPage="typeof(Pages.NotFound)"`
on the `Router`, which does not go through `AuthorizeRouteView`'s `DefaultLayout`.

## `#blazor-error-ui`

The bar is declared identically in both `StoreLayout` and `AdminLayout` and styled in `app.css`. Leave
its `color-scheme: light only` and its background alone unless [01](01-design-tokens.md) already
tokenised it — that decision is made there, not here. What this task does change: **the copy**. `🗙` as
the dismiss control and a bare "An unhandled error has occurred." are template defaults. Make the two
layouts' markup identical to each other and give the reload/dismiss links accessible text.

Also confirm the two layouts have not drifted — the same block, copy-pasted twice, is exactly where a
fix lands in one and not the other.

---

## Definition of done

- [ ] `/Error` renders inside the shell, with a hero, the reference chip and a link home.
- [ ] The "Development Mode" paragraph and every `text-danger` class are gone.
- [ ] `RequestId` still resolves from `Activity.Current?.Id` with the `TraceIdentifier` fallback, and is
      hidden when empty.
- [ ] `/not-found` renders a hero, two actions and a real `<PageTitle>`.
- [ ] Neither page declares a `@rendermode`, and neither contains an `OnClick`.
- [ ] The `#blazor-error-ui` markup is identical in both layouts and its controls have accessible text.
- [ ] No hard-coded colour in either new CSS file.
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
