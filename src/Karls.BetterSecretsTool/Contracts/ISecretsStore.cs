// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Karls.BetterSecretsTool.Contracts;

internal interface ISecretsStore {
    string this[string key] { get; }
    int Count { get; }
    bool ContainsKey(string key);
    IEnumerable<KeyValuePair<string, string>> AsEnumerable();
    IEnumerable<KeyValuePair<string, string>> AsSortedEnumerable();
    void Clear();
    void Set(string key, string value);
    void Remove(string key);
    void Save();
}
