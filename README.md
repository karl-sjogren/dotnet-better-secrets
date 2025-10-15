# Karls Better Secrets Tool

An easier way to manage your [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
from the command line.

## Installation

As a global tool:

```bash
dotnet tool install -g Karls.BetterSecretsTool
```

As a local tool:

```bash
dotnet new tool-manifest # if you don't have a manifest already
dotnet tool install Karls.BetterSecretsTool
```

## Usage

Run `better-secrets` in the directory of your project to manage its user secrets.

As a global tool:

```bash
better-secrets
```

As a local tool:

```bash
dotnet better-secrets
```

### Azure Key Vault Integration

You can download secrets from an Azure Key Vault into your user secrets store.
This can be very useful to easily get a bunch of default secrets into your
development environment.

To do this you need to add a new property to your `.csproj` file,
`<UserSecretsKeyVault>`:

```xml
  <PropertyGroup>
    <UserSecretsId>b6a435f3-371e-4719-bd15-d257df8962c4</UserSecretsId>
    <UserSecretsKeyVault>local-dev-key-vault</UserSecretsKeyVault>
  </PropertyGroup>
```

The value of the property is the name of your key vault. The tool will then
try to access the key vault using [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential).
This means that if you are logged in with the Azure CLI (`az login`) and have
access to the key vault, it should just work. If not it might prompt you to
login in a browser.

You need to have at least the `Key Vault Secrets User` role assigned in the
key vault.

## License

The project is released under the [MIT](LICENSE) license. Parts of the code
are derived from the [ASP.Net Core](https://github.com/dotnet/aspnetcore)
repository which is licensed by the .NET Foundation under the MIT license.
See individual file headers for details.
