# Introduction

| Github Action Tests                                                                          | CodeCov                                                                                                                                                   |
|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| ![actions](https://github.com/taosdata/taos-connector/actions/workflows/linux.yml/badge.svg) | [![codecov](https://codecov.io/gh/taosdata/taos-connector-dotnet/graph/badge.svg?token=U30JZYDGMS)](https://codecov.io/gh/taosdata/taos-connector-dotnet) |

English | [简体中文](README-CN.md)

`TDengine.Connector` is the C# language connector provided by TDengine. C# developers can use it to develop C# application software that accesses TDengine cluster data.

# Get the Driver

Nuget package `TDengine.Connector` can be added to the current project through dotnet CLI under the path of the current .NET project.

```bash
dotnet add package TDengine.Connector
```

You can also modify the `.csproj` file of the current project and add the following ItemGroup.

``` XML
   <ItemGroup>
     <PackageReference Include="TDengine.Connector" Version="3.1.*" />
   </ItemGroup>
```

# Documentation

- For development examples, see the [Developer Guide](https://docs.tdengine.com/developer-guide/).
- For version history, TDengine version compatibility, and API documentation, see
  the [Reference Manual](https://docs.tdengine.com/tdengine-reference/client-libraries/csharp/).

# Contributing

We encourage everyone to help improve this project. Below is the development and testing process for this project:

## Prerequisites

* Install [.NET SDK](https://dotnet.microsoft.com/download)
* [Nuget Client](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools) (optional installation)
* Install the TDengine client driver. For specific steps, please refer
  to [Installing the client driver](https://docs.tdengine.com/develop/connect/#install-client-driver-taosc)

## Building

1. `dotnet restore` Restore the project's dependencies.
2. `dotnet build --no-restore` Build the project.

## Testing

1. Before running tests, ensure that the TDengine server is installed and that `taosd` and `taosAdapter` are running.
   The database should be empty.
2. In the project directory, run `dotnet test` to execute the tests. The tests will connect to the local TDengine
   server and taosAdapter for testing.

# References

- [TDengine Official Website](https://tdengine.com/)
- [TDengine GitHub](https://github.com/taosdata/TDengine)

# License

[MIT License](./LICENSE)