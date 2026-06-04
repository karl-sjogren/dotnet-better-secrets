# Karls Better Secrets Tool [![NuGet Version](https://img.shields.io/nuget/v/Karls.BetterSecretsTool) ![NuGet Downloads](https://img.shields.io/nuget/dt/Karls.BetterSecretsTool)](https://www.nuget.org/packages/Karls.BetterSecretsTool/) [![codecov](https://codecov.io/gh/karl-sjogren/dotnet-better-secrets/graph/badge.svg?token=xPOEUpNYRd)](https://codecov.io/gh/karl-sjogren/dotnet-better-secrets)

An easier way to manage your [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
from the command line.

![Better Secrets Tool Screenshot](https://raw.githubusercontent.com/karl-sjogren/dotnet-better-secrets/develop/assets/start-screen.png)

## Installation

As a local tool:

```bash
dotnet new tool-manifest # if you don't have a manifest already
dotnet tool install Karls.BetterSecretsTool
```

As a global tool:

```bash
dotnet tool install -g Karls.BetterSecretsTool
```

As a NativeAOT binary (no .NET runtime required):

1. Download the archive for your platform from the [GitHub Releases](https://github.com/karl-sjogren/dotnet-better-secrets/releases) page.
2. Extract the archive.
3. Run the executable from the extracted folder:
   - Linux/macOS: `./Karls.BetterSecretsTool --help`
   - Windows: `.\Karls.BetterSecretsTool.exe --help`

> [!NOTE]
> While a global tool is convenient since it is always available, a local
> tool ensures that everyone working on the project uses the same version of
> the tool and that new versions are picked up automatically by tools such
> as Dependabot/Renovatebot.
>
> The NuGet .NET tool remains the primary distribution format. NativeAOT
> binaries are published as an additional option.

### NativeAOT support

NativeAOT binaries are currently published for:

- `linux-x64`
- `win-x64`
- `osx-x64`

NativeAOT support is delivered as release artifacts in parallel with the NuGet
tool package. Pre-release tags (for example `1.2.0-preview.1`) are published as
GitHub pre-releases for rollout and validation before broader adoption.

Local NativeAOT validation command:

```bash
dotnet publish ./src/Karls.BetterSecretsTool/Karls.BetterSecretsTool.csproj --configuration Release --runtime <RID> /p:PublishNativeAot=true
```

## Usage

Run `better-secrets` in the directory of your project to manage its user secrets.

As a local tool:

```bash
dotnet better-secrets
```

As a global tool:

```bash
dotnet-better-secrets
```

Using `dnx` if on .NET 10 SDK:

```bash
dnx Karls.BetterSecretsTool
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
