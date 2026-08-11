// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Indicates that a message handler or component should be executed within a transaction.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TransactionalAttribute : Attribute { }
