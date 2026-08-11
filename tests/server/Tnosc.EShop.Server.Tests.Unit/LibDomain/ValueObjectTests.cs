// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibDomain;

public sealed class ValueObjectTests
{
    [Fact]
    public void ValueObjects_Should_BeEqual_When_ValuesAreEqual()
    {
        var first = new TestMoney(Amount: 10, Currency: "EUR");
        var second = new TestMoney(Amount: 10, Currency: "EUR");

        first.ShouldBe(expected: second);
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void ValueObjects_Should_NotBeEqual_When_ValuesDiffer()
    {
        var first = new TestMoney(Amount: 10, Currency: "EUR");
        var second = new TestMoney(Amount: 20, Currency: "EUR");

        first.ShouldNotBe(expected: second);
        (first == second).ShouldBeFalse();
    }

    [Fact]
    public void ValueObjects_Should_NotBeEqual_When_SiblingTypeWithSameValues()
    {
        var money = new TestMoney(Amount: 10, Currency: "EUR");
        var discountedMoney = new TestDiscountedMoney(Amount: 10, Currency: "EUR");

        money.Equals(other: discountedMoney).ShouldBeFalse();
    }
}
