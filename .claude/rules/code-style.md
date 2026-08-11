# Rule — code style

Formatting and naming conventions applied to every `.cs` file in this repo, checked nowhere except
code review — there is no analyzer for most of these, so they rely on being followed deliberately.

## File header and layout

Every `.cs` file opens with this header, then explicit `using`s (System first), then a **file-scoped
namespace**. One public type per file, named after the file.

```csharp
// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------
```

## Types and members

- **Every class uses a primary constructor.** No classic constructor + field-assignment boilerplate
  unless the primary-constructor syntax genuinely cannot express what's needed (e.g. multiple
  constructor overloads with different parameter sets).
- **Explicit types, not `var`** — except where the type is apparent (`new Product { … }`, `new()`).
- Braces always, even one-line `if`. `static` lambdas where nothing is captured. CRLF, 4 spaces.
- Expression-bodied properties/accessors/operators/lambdas yes; multi-statement methods use blocks.

## Call sites and method signatures

- **Name every argument at every call site** — each parameter's name is written out on every method,
  constructor and factory call. No positional arguments, tests included; only `params` arrays are
  exempt: `Money.Create(amount: x, currency: y)`, `ShouldBe(expected: "Product.NotFound")`.
- **More than two parameters ⇒ one parameter per line**, both in the declaration and at every call
  site. Two or fewer stay on one line.

  ```csharp
  // 2 params — one line
  public static Money Create(decimal amount, string currency)

  // 3+ params — one per line
  public static Result<Product> Create(
      ProductId id,
      Sku sku,
      Money price,
      string name)
  ```

## Naming

- **Async method names end in `Async`** — `GetProductAsync`, `SaveChangesAsync`. No exceptions for
  handler `Handle`/`HandleAsync` methods either — if it returns a `Task`/`ValueTask`, it is named
  `…Async`.
- Error codes are `Aggregate.Reason`: `Product.NotFound`, `Sku.InvalidFormat`.
- `ErrorType` → HTTP: `Validation` 400 · `Unauthorized` 401 · `Forbidden` 403 · `NotFound` 404 ·
  `Conflict` 409 · `Failure`/`Unexpected` 500 · `Custom` → its `NumericType`.

## Checklist

- [ ] File header present, `using`s explicit and System-first, file-scoped namespace.
- [ ] Class uses a primary constructor unless there's a concrete reason it can't.
- [ ] Every call-site argument is named; no positional arguments.
- [ ] Any method/constructor/factory with 3+ parameters has one parameter per line.
- [ ] Every method returning `Task`/`ValueTask` is named `…Async`.
