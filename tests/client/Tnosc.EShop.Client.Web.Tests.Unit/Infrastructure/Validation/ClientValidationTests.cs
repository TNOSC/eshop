// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Infrastructure.Validation;

public sealed class ClientValidationTests
{
    private sealed class SampleModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Validate_Should_ReturnNull_When_TheInstanceIsValid()
    {
        // Arrange
        SampleModel model = new() { Name = "Widget" };

        // Act
        ClientProblem? result = ClientValidation.Validate(viewModel: model);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Validate_Should_ReturnA400ProblemKeyedByPropertyName_When_ARequiredFieldIsMissing()
    {
        // Arrange
        SampleModel model = new() { Name = string.Empty };

        // Act
        ClientProblem? result = ClientValidation.Validate(viewModel: model);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(expected: 400);
        result.ErrorCode.ShouldBe(expected: ClientValidation.ValidationErrorCode);
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContainKey(key: nameof(SampleModel.Name));
    }

    [Fact]
    public void ApplyFieldErrors_Should_AddTheMessageToTheField_When_TheProblemKeyIsAPropertyName()
    {
        // Arrange
        SampleModel model = new();
        EditContext editContext = new(model: model);
        ValidationMessageStore messageStore = new(editContext: editContext);
        List<string> unmappedMessages = [];

        ClientProblem problem = new(
            Type: null,
            Title: "Validation failed",
            Status: 400,
            Detail: null,
            Instance: null,
            Errors: new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
            {
                [nameof(SampleModel.Name)] = ["Name is required."],
            },
            ErrorCode: ClientValidation.ValidationErrorCode,
            TraceId: null);

        // Act
        ClientValidation.ApplyFieldErrors(
            problem: problem,
            editContext: editContext,
            messageStore: messageStore,
            unmappedMessages: unmappedMessages);

        // Assert
        editContext.GetValidationMessages(fieldIdentifier: editContext.Field(fieldName: nameof(SampleModel.Name)))
            .ShouldContain(expected: "Name is required.", comparer: StringComparer.Ordinal);
        unmappedMessages.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyFieldErrors_Should_AppendToUnmappedMessages_When_TheErrorCodeCannotBeResolvedToAField()
    {
        // Arrange
        SampleModel model = new();
        EditContext editContext = new(model: model);
        ValidationMessageStore messageStore = new(editContext: editContext);
        List<string> unmappedMessages = [];

        ClientProblem problem = new(
            Type: null,
            Title: "Failed",
            Status: 409,
            Detail: null,
            Instance: null,
            Errors: new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
            {
                ["Some.UnknownServerCode"] = ["Something went wrong on the server."],
            },
            ErrorCode: "Some.UnknownServerCode",
            TraceId: null);

        // Act
        ClientValidation.ApplyFieldErrors(
            problem: problem,
            editContext: editContext,
            messageStore: messageStore,
            unmappedMessages: unmappedMessages);

        // Assert
        unmappedMessages.ShouldContain(expected: "Something went wrong on the server.", comparer: StringComparer.Ordinal);
    }
}
