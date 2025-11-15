// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Karls.BetterSecretsTool.Contracts;

internal interface IMsBuildProjectFinder {
    MsBuildProject[] FindMsBuildProjects(string baseDirectory);
}
