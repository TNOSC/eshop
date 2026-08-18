# Task 01 — Design tokens

**Goal:** the `:root` token layer and the `.eshop-page` container in `app.css`, plus the one existing
scoped file that hard-codes colours. **No component markup changes in this task** — nothing should look
different afterwards except the reconnect modal in dark mode.

**Depends on:** nothing. This is the first task in the series.

---

## Files to edit

| File | Change |
|---|---|
| `src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/wwwroot/app.css` | 20 lines → the token layer, the container, the shared primitives |
| `src/client/Tnosc.EShop.Client.Web/Tnosc.EShop.Client.Web/Components/Layout/ReconnectModal.razor.css` | Replace `#6b9ed2` / `white` with tokens |

Nothing is created and nothing is deleted.

---

## Step 1 — the token block

Prepend to `app.css`. Keep the existing `#blazor-error-ui` block at the bottom, but re-express its
`lightyellow` / `rgba(...)` on tokens too.

```css
/* ---------------------------------------------------------------------------------
   Design tokens.

   Every value routes through a Fluent neutral token so the IThemeService toggle keeps
   working and one ruleset serves light and dark. The comment on each colour is the
   literal the eShop reference design used, kept for traceability only — never inline it.
   --------------------------------------------------------------------------------- */
:root {
    /* colour */
    --eshop-ink: var(--colorNeutralForeground1);            /* #000    */
    --eshop-on-ink: var(--colorNeutralBackground1);         /* #FFF    */
    --eshop-surface: var(--colorNeutralBackground1);        /* #FFF    */
    --eshop-muted: var(--colorNeutralForeground3);          /* #444    */
    --eshop-rule: var(--colorNeutralStroke2);               /* #D2D2D2 */
    --eshop-panel: var(--colorNeutralBackground2);          /* #F7F7F7 */
    --eshop-hover: var(--colorNeutralBackground3);          /* #ddd    */
    --eshop-status-neutral: var(--colorNeutralForeground4); /* #A3A3A3 */
    --eshop-status-good: var(--colorStatusSuccessForeground1); /* #2A9E01 */
    --eshop-status-bad: var(--colorStatusDangerForeground1);   /* #FF4E4E */

    /* type — the reference scale on Fluent's family */
    --eshop-font: var(--fontFamilyBase);
    --eshop-hero-size: 3.5rem;
    --eshop-hero-line: 100%;
    --eshop-sub-size: 2rem;
    --eshop-sub-line: 125%;
    --eshop-price-size: 1.6rem;
    --eshop-h2-size: 1.25rem;
    --eshop-h2-line: 140%;
    --eshop-body-size: 1rem;
    --eshop-body-line: 150%;
    --eshop-chip-size: 0.75rem;
    --eshop-weight-regular: 400;
    --eshop-weight-semibold: 600;
    --eshop-weight-bold: 700;

    /* geometry */
    --eshop-max: 120rem;
    --eshop-gutter: 10rem;
    --eshop-col-gap: 6rem;
    --eshop-grid-gap: 2.5rem;
    --eshop-hero-tall: 38rem;
    --eshop-hero-short: 15rem;
    --eshop-navbar-height: 5rem;
    --eshop-radius-pill: 1.25rem;
    --eshop-radius-badge: 0.75rem;
    --eshop-shadow-menu: 0 0.25rem 0.5rem 0 rgb(0 0 0 / 0.2);
}

@media only screen and (max-width: 480px) {
    :root {
        --eshop-gutter: 1rem;
        --eshop-col-gap: 2rem;
        --eshop-hero-tall: 22rem;
        --eshop-hero-short: 12rem;
        --eshop-hero-size: 2rem;
        --eshop-sub-size: 1.25rem;
    }
}

@media only screen and (min-width: 481px) and (max-width: 1024px) {
    :root {
        --eshop-gutter: 3rem;
        --eshop-col-gap: 3rem;
        --eshop-hero-tall: 28rem;
        --eshop-hero-size: 2.5rem;
        --eshop-sub-size: 1.5rem;
    }
}
```

> The reference has no responsive type scale at all — a `3.5rem` h1 inside `padding: 0 1rem` on a phone.
> The two overrides above are a deliberate improvement, not an omission.

## Step 2 — the page container

The reference repeats `padding: 0 10rem` verbatim in six page CSS files, with the two breakpoint
overrides copy-pasted into each. One class replaces all of it:

```css
body {
    font-family: var(--eshop-font);
    background-color: var(--eshop-surface);
    color: var(--eshop-ink);
}

/* Horizontal gutter + max width for every page's content. Applied once, by StoreHero's
   sibling wrapper and by each page's root element. */
.eshop-page {
    max-width: var(--eshop-max);
    margin-inline: auto;
    padding-inline: var(--eshop-gutter);
    box-sizing: border-box;
}

/* Full-bleed band (hero, footer): edge to edge, but its inner content respects the gutter. */
.eshop-band {
    width: 100%;
    max-width: var(--eshop-max);
    margin-inline: auto;
    position: relative;
}
```

## Step 3 — shared primitives

Three things the reference defines globally and every page reuses. Put them in `app.css`, not in a
scoped file, because more than one component needs each.

```css
/* Square, flat buttons. Fluent's own buttons stay Fluent — this is for the anchor-styled
   calls to action the reference renders as <a class="button button-primary">. */
.eshop-button {
    display: inline-flex;
    padding: 1rem 0.75rem;
    justify-content: center;
    align-items: center;
    gap: 0.25rem;
    border: none;
    text-decoration: none;
    font-family: var(--eshop-font);
    font-size: var(--eshop-body-size);
    cursor: pointer;
}

.eshop-button-primary {
    background: var(--eshop-ink);
    color: var(--eshop-on-ink);
}

.eshop-button-secondary {
    border: 1px solid var(--eshop-muted);
    background: var(--eshop-surface);
    color: var(--eshop-ink);
}

/* Section heading with the underline the reference uses on checkout and cart. */
.eshop-h2 {
    color: var(--eshop-ink);
    font-size: var(--eshop-h2-size);
    font-weight: var(--eshop-weight-semibold);
    line-height: var(--eshop-h2-line);
    border-bottom: 1px solid var(--eshop-rule);
    width: 100%;
    padding-bottom: 0.5rem;
    margin: 0;
}

/* Outlined status pill — colour supplied by the caller via --eshop-pill-color. */
.eshop-pill {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem 1rem;
    border-radius: var(--eshop-radius-pill);
    border: 1px solid var(--eshop-pill-color, var(--eshop-status-neutral));
    color: var(--eshop-pill-color, var(--eshop-status-neutral));
    font-size: var(--eshop-chip-size);
    font-weight: var(--eshop-weight-regular);
    line-height: 1.25rem;
}
```

## Step 4 — retire the hard-coded colours in `ReconnectModal.razor.css`

That file uses `#6b9ed2` and `white` literally, so the reconnect overlay does not follow dark mode.
Replace with `var(--eshop-ink)` / `var(--eshop-surface)` / `var(--eshop-panel)` as the role of each
occurrence dictates. Read the file first — it is animation-heavy, and only the colour declarations
change.

It lives in the **host** project (`Components/Layout/`), not `.Client`, and is rendered from
`App.razor` outside `<Routes />`. It is therefore always static SSR — that is fine, it is CSS-driven.

## Step 5 — do not touch `#blazor-error-ui` semantics

It sets `color-scheme: light only` on purpose: it is the last-resort error bar and must stay legible
even if theming is what broke. Re-express `lightyellow` on a token **only if** it still reads as a
warning in both themes; otherwise leave the literal and add a one-line comment saying why. This is the
sanctioned exception to the no-literals rule, and task [10](10-verification.md)'s grep excludes
`app.css`.

---

## Definition of done

- [ ] `app.css` carries the `:root` block, the two breakpoint overrides, `.eshop-page`, `.eshop-band`
      and the four primitives.
- [ ] Every colour in the file is a `var(--color…)` Fluent token, except the documented
      `#blazor-error-ui` exception.
- [ ] `ReconnectModal.razor.css` contains no `#rrggbb` literal and no bare `white`.
- [ ] Nothing else changed — `git diff --stat` shows exactly two files.
- [ ] The app still renders as before, and **the theme toggle still flips light/dark** (it is inert on
      click today — that is defect 2, fixed in [03](03-store-shell.md); verify here by switching the OS
      colour scheme instead).
- [ ] `dotnet build Tnosc.EShop.slnx` is clean.
