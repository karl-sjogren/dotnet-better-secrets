using Karls.BetterSecretsTool.Contracts;

namespace Karls.BetterSecretsTool.Tests;

public class InMemorySecretsStore : ISecretsStore {
    private readonly IDictionary<string, string> _secrets;

    public InMemorySecretsStore(IDictionary<string, string>? initialSecrets = null) {
        _secrets = new Dictionary<string, string>();

        if(initialSecrets is not null) {
            foreach(var kvp in initialSecrets) {
                _secrets[kvp.Key] = kvp.Value;
            }
        }
    }

    public string this[string key] {
        get {
            return _secrets[key];
        }
    }

    public int Count => _secrets.Count;

    public bool ContainsKey(string key) => _secrets.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string>> AsEnumerable() => _secrets;

    public IEnumerable<KeyValuePair<string, string>> AsSortedEnumerable() => _secrets.OrderBy(kvp => kvp.Key);

    public void Clear() => _secrets.Clear();

    public void Set(string key, string value) => _secrets[key] = value;

    public void Remove(string key) {
        _secrets.Remove(key);
    }

    public virtual void Save() {
        // No-op for in-memory store.
    }
}
