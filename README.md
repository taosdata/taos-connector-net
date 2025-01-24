<!-- omit in toc -->
# TDengine C# Connector
<!-- omit in toc -->

| Github Action Tests                                                                                 | CodeCov                                                                                                                                                   |
|-----------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| ![actions](https://github.com/taosdata/taos-connector-dotnet/actions/workflows/linux.yml/badge.svg) | [![codecov](https://codecov.io/gh/taosdata/taos-connector-dotnet/graph/badge.svg?token=U30JZYDGMS)](https://codecov.io/gh/taosdata/taos-connector-dotnet) |

English | [简体中文](README-CN.md)

<!-- omit in toc -->
## Table of Contents
<!-- omit in toc -->

- [1. Introduction](#1-introduction)
    - [1.1 Connection Methods](#11-connection-methods)
    - [1.2 .NET Version Compatibility](#12-net-version-compatibility)
    - [1.3 Supported Platforms](#13-supported-platforms)
- [2. Get the Driver](#2-get-the-driver)
- [3. Documentation](#3-documentation)
- [4. Prerequisites](#4-prerequisites)
- [5. Build](#5-build)
- [6. Testing](#6-testing)
    - [6.1 Test Execution](#61-test-execution)
    - [6.2 Test Case Addition](#62-test-case-addition)
    - [6.3 Performance Testing](#63-performance-testing)
- [7. Submitting Issues](#7-submitting-issues)
- [8. Submitting PRs](#8-submitting-prs)
- [9. References](#9-references)
- [10. License](#10-license)


## 1. Introduction

`TDengine.Connector` is the C# language connector provided by TDengine. C# developers can use it to develop C# application software that accesses TDengine cluster data.

### 1.1 Connection Methods

- Native Connection: Establishes a connection directly with the server program taosd through the client driver taosc.
  This method requires the client driver taosc and the server taosd to be of the same version.
- WebSocket Connection: Establishes a connection with taosd through the WebSocket API provided by the taosAdapter
  component, without relying on the TDengine client driver.

We recommend using the WebSocket connection method. For detailed instructions, please refer
to: [Connection Methods](https://docs.tdengine.com/developer-guide/connecting-to-tdengine/#connection-methods).

### 1.2 .NET Version Compatibility

- .NET Framework 4.6 and above
- .NET 5.0 and above

### 1.3 Supported Platforms

- The platforms supported by the native connection are consistent with those supported by the TDengine client driver.
- WebSocket connections support all platforms that can run .NET.

## 2. Get the Driver

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

## 3. Documentation

- For development examples, see [Developer Guide](https://docs.tdengine.com/developer-guide/), which includes examples
  of data writing, querying, schemaless writing, parameter binding, and data subscription.
- For other reference information,
  see [Reference Manual](https://docs.tdengine.com/tdengine-reference/client-libraries/csharp/), which includes version
  history, data types, example programs, API descriptions, and FAQs.

## 4. Prerequisites

* Install [.NET SDK](https://dotnet.microsoft.com/download)
* [Nuget Client](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools) (optional installation)
* Install the TDengine client driver. For specific steps, please refer
  to [Installing the client driver](https://docs.tdengine.com/develop/connect/#install-client-driver-taosc)

## 5. Build

1. `dotnet restore` Restore the project's dependencies.
2. `dotnet build --no-restore` Build the project.

## 6. Testing

### 6.1 Test Execution

1. Before running tests, ensure that the TDengine server is installed and that `taosd` and `taosAdapter` are running.
   The database should be empty.
2. In the project directory, run `dotnet test` to execute the tests. The tests will connect to the local TDengine
   server and taosAdapter for testing.
3. If the tests pass, `Test Run Successful` will be printed. If the tests fail, the failure information
   `Test Run Failed` will be printed.

### 6.2 Test Case Addition

Add test cases in the `test` directory. Add ADO.NET test cases to `test/Data.Tests` and client driver test cases to
`test/Driver.Test/Client`.
The test cases use the xunit framework.

### 6.3 Performance Testing

Performance testing is in progress.

## 7. Submitting Issues

We welcome the submission
of [GitHub Issue](https://github.com/taosdata/taos-connector-dotnet/issues/new?template=Blank+issue). When
submitting, please provide the following information:

- Description of the issue and whether it is consistently reproducible
- Driver version
- Connection parameters (excluding server address, username, and password)
- TDengine version

## 8. Submitting PRs

We welcome developers to contribute to this project. Please follow the steps below to submit a PR:

1. Fork this project. Please refer
   to [how to fork a repo](https://docs.github.com/en/get-started/quickstart/fork-a-repo).
2. Create a new branch from the main branch with a meaningful branch name (`git checkout -b my_branch`).
3. Modify the code, ensure all unit tests pass, and add new unit tests to verify the changes.
4. Push the changes to the remote branch (`git push origin my_branch`).
5. Create a Pull Request on GitHub. Please refer
   to [how to create a pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request).
6. After submitting the PR, if the CI passes, you can find your PR on
   the [codecov](https://app.codecov.io/gh/taosdata/taos-connector-dotnet/pulls) page to check the coverage.

## 9. References

- [TDengine Official Website](https://tdengine.com/)
- [TDengine GitHub](https://github.com/taosdata/TDengine)

## 10. License

[MIT License](./LICENSE)