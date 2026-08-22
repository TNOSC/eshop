// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="AgentOutputContract"/> can break its invariant.
/// </summary>
public static class AgentOutputContractErrors
{
    /// <summary>
    /// Gets the error returned when the declared output type cannot carry a structured answer.
    /// </summary>
    public static Error OutputTypeNotSupported => Error.Validation(
        code: "AgentOutputContract.OutputTypeNotSupported",
        description: "An agent's output type must be a closed class or struct with bindable members.");
}
