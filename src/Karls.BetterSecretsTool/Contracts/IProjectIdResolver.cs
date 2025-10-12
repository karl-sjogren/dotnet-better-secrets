// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Karls.BetterSecretsTool.Contracts;

public interface IProjectIdResolver {
    string? Resolve(string project, string configuration);
}
