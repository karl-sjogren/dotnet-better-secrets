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

## License

The project is released under the [MIT](LICENSE) license. Parts of the code
are derived from the [ASP.Net Core](https://github.com/dotnet/aspnetcore)
repository which is licensed by the .NET Foundation under the MIT license.
See individual file headers for details.
