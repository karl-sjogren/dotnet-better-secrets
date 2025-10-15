// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Karls.BetterSecretsTool.Vendor;

namespace Karls.BetterSecretsTool.Contracts;

public interface IProjectIdResolver {
    ResolveResult Resolve(string project, string configuration);
}
