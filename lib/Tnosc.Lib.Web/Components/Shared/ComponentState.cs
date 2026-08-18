// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Web.Components.Shared;

/// <summary>
/// The lifecycle state of a component that loads or submits data, rendered through
/// <see cref="StatefulBoundary"/>. A business/API failure (a <c>ClientProblem</c> from a failed
/// <c>ClientResult</c>) is not represented here — it is a normal outcome of a load and is displayed
/// with an <see cref="ErrorPanel"/> inside <see cref="Content"/>. <see cref="Error"/> means the
/// component itself crashed while rendering.
/// </summary>
public enum ComponentState
{
    /// <summary>The component's initial data fetch (or submit) has not yet completed.</summary>
    Loading,

    /// <summary>
    /// An unhandled exception was thrown while rendering the component's content and was caught by
    /// its <see cref="StatefulBoundary"/>.
    /// </summary>
    Error,

    /// <summary>
    /// The component's load or submit call has completed and its content is rendered — including a
    /// business failure shown via <see cref="ErrorPanel"/>, which is still "content", not "error".
    /// </summary>
    Content
}
