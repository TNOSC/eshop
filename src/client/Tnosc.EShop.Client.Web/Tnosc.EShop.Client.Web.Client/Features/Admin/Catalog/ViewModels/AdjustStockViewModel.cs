// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;

/// <summary>The adjust-stock dialog's ViewModel.</summary>
public sealed class AdjustStockViewModel : IValidatableObject
{
    /// <summary>Gets or sets the signed stock delta.</summary>
    public int Delta { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Delta == 0)
        {
            yield return new ValidationResult(
                errorMessage: "A non-zero stock adjustment is required.",
                memberNames: [nameof(Delta)]);
        }
    }
}
