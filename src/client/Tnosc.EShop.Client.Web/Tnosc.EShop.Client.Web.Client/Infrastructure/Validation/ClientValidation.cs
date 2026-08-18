// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;

/// <summary>
/// Validates a component ViewModel via <see cref="Validator"/> and merges an <see cref="ClientProblem"/>'s
/// field errors back onto an <see cref="EditContext"/> — the two halves every component service under
/// the <c>blazor-client-mvvm</c> convention shares, so client-side and server-side rejections flow
/// through one code path instead of each form reimplementing its own.
/// </summary>
internal static class ClientValidation
{
    /// <summary>
    /// The <see cref="ClientProblem.ErrorCode"/> stamped on a problem produced by <see cref="Validate{TViewModel}"/>.
    /// <see cref="ApplyFieldErrors"/> checks it to know whether <see cref="ClientProblem.Errors"/> is
    /// keyed by ViewModel property name (this class) or by server error code
    /// (<see cref="ValidationCodeFieldMap"/> resolves those).
    /// </summary>
    public const string ValidationErrorCode = "ClientValidation";

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>(comparer: StringComparer.Ordinal);

    /// <summary>Validates <paramref name="viewModel"/>'s DataAnnotations attributes.</summary>
    /// <param name="viewModel">The ViewModel instance to validate.</param>
    /// <returns>
    /// <see langword="null"/> when valid; otherwise an <see cref="ClientProblem"/> shaped exactly like a
    /// server 400 — same <c>Errors</c> field-error dictionary shape — so a component's failure
    /// handling does not need a separate branch for a client-side rejection.
    /// </returns>
    public static ClientProblem? Validate<TViewModel>(TViewModel viewModel)
        where TViewModel : notnull
    {
        List<ValidationResult> results = [];
        ValidationContext context = new(instance: viewModel);
        bool isValid = Validator.TryValidateObject(
            instance: viewModel,
            validationContext: context,
            validationResults: results,
            validateAllProperties: true);

        if (isValid)
        {
            return null;
        }

        Dictionary<string, List<string>> grouped = new(comparer: StringComparer.Ordinal);

        foreach (ValidationResult result in results)
        {
            string message = result.ErrorMessage ?? "Invalid value.";
            IReadOnlyList<string> memberNames = result.MemberNames.Any()
                ? [.. result.MemberNames]
                : [string.Empty];

            foreach (string memberName in memberNames)
            {
                if (!grouped.TryGetValue(key: memberName, value: out List<string>? messages))
                {
                    messages = [];
                    grouped[memberName] = messages;
                }

                messages.Add(item: message);
            }
        }

        Dictionary<string, string[]> errors = new(comparer: StringComparer.Ordinal);
        foreach ((string field, List<string> messages) in grouped)
        {
            errors[field] = [.. messages];
        }

        return new ClientProblem(
            Type: null,
            Title: "Validation failed",
            Status: 400,
            Detail: null,
            Instance: null,
            Errors: errors,
            ErrorCode: ValidationErrorCode,
            TraceId: null);
    }

    /// <summary>
    /// Merges <paramref name="problem"/>'s field errors onto <paramref name="editContext"/>, resolving
    /// server error codes through <see cref="ValidationCodeFieldMap"/> and passing ViewModel property
    /// names straight through. Unresolvable codes are appended to <paramref name="unmappedMessages"/>
    /// instead of being dropped.
    /// </summary>
    /// <param name="problem">The failure to merge — from <see cref="Validate{TViewModel}"/> or a server response.</param>
    /// <param name="editContext">The form's <see cref="EditContext"/>.</param>
    /// <param name="messageStore">The store backing <see cref="ValidationMessage"/> rendering.</param>
    /// <param name="unmappedMessages">Collects messages whose code could not be resolved to a field.</param>
    public static void ApplyFieldErrors(
        ClientProblem problem,
        EditContext editContext,
        ValidationMessageStore messageStore,
        ICollection<string> unmappedMessages)
    {
        IReadOnlyDictionary<string, string[]> errors = problem.Errors
            ?? (problem.Title is { } singleCode
                ? new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
                {
                    [singleCode] = [problem.Detail ?? singleCode],
                }
                : EmptyErrors);

        bool keysAreFieldNames = string.Equals(a: problem.ErrorCode, b: ValidationErrorCode, comparisonType: StringComparison.Ordinal);

        foreach ((string key, string[] messages) in errors)
        {
            string? field;

            if (keysAreFieldNames)
            {
                field = key;
            }
            else if (ValidationCodeFieldMap.TryResolveField(errorCode: key, fieldName: out string? mapped))
            {
                field = mapped;
            }
            else
            {
                field = null;
            }

            if (field is not null)
            {
                messageStore.Add(fieldIdentifier: editContext.Field(fieldName: field), messages: messages);
            }
            else
            {
                foreach (string message in messages)
                {
                    unmappedMessages.Add(item: message);
                }
            }
        }

        editContext.NotifyValidationStateChanged();
    }
}
