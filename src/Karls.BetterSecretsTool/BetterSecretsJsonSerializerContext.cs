using System.Text.Json.Serialization;

namespace Karls.BetterSecretsTool;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class BetterSecretsJsonSerializerContext : JsonSerializerContext {
}
