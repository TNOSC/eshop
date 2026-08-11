// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Domain.ValueObjects;

/// <summary>
/// Represents the base type for value objects: types defined entirely by their attributes,
/// with no conceptual identity, that are equal when their attributes are equal.
/// </summary>
/// <remarks>
/// <para>
/// This is deliberately a bare <c>abstract record</c> and not the classic
/// <c>GetEqualityComponents()</c> base class. Records give structural equality and
/// <c>EqualityContract</c> (so two sibling value objects with the same values, such as
/// <c>Money(10,"EUR")</c> and <c>DiscountedMoney(10,"EUR")</c>, never compare equal) for free,
/// with zero allocation. The classic pattern allocates a compiler-generated iterator and boxes
/// every struct component on every <c>Equals</c>/<c>GetHashCode</c> call — costly given that
/// value objects live in dictionaries and are compared on every EF change-tracking pass.
/// </para>
/// <para>
/// Trade-off: records compare reference-typed members by reference, not by content, so a value
/// object must never hold a mutable reference collection such as <c>List&lt;T&gt;</c> — doing so
/// would compare by reference and silently break structural equality. Derived value objects must
/// expose collections only as <c>ImmutableArray&lt;T&gt;</c> or <c>FrozenSet&lt;T&gt;</c>, both of
/// which implement value equality themselves.
/// </para>
/// <para>
/// Construction convention: a value object with invariants declares a private constructor and a
/// public static <c>Result&lt;T&gt; Create(...)</c> factory that validates the invariants before
/// constructing the instance.
/// </para>
/// </remarks>
public abstract record ValueObject;
