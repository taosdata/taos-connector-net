# 简介

| Github Action Tests                                                                          | CodeCov                                                                                                                                                   |
|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|
| ![actions](https://github.com/taosdata/taos-connector-dotnet/actions/workflows/linux.yml/badge.svg) | [![codecov](https://codecov.io/gh/taosdata/taos-connector-dotnet/graph/badge.svg?token=U30JZYDGMS)](https://codecov.io/gh/taosdata/taos-connector-dotnet) |

[English](README.md) | 简体中文

`TDengine.Connector` 是 TDengine 提供的 C# 语言连接器。C# 开发人员可以通过它开发存取 TDengine 集群数据的 C# 应用软件。

`TDengine.Connector` 提供了两种连接方式：

- 原生连接：通过客户端驱动程序 taosc 直接与服务端程序 taosd 建立连接。这种方式需要保证客户端的驱动程序 taosc 和服务端的 taosd 版本保持一致。
- Websocket 连接： 通过 taosAdapter 组件提供的 WebSocket API 建立与 taosd 的连接，不依赖 TDengine 客户端驱动。

## 支持的平台

- 原生连接支持的平台和 TDengine 客户端驱动支持的平台一致。
- WebSocket 连接支持所有能运行 .NET 运行时的平台。

# 获取驱动

可以在当前 .NET 项目的路径下，通过 dotnet CLI 添加 Nuget package `TDengine.Connector` 到当前项目。

``` bash
dotnet add package TDengine.Connector
```

也可以修改当前项目的 `.csproj` 文件，添加如下 ItemGroup。

``` XML
  <ItemGroup>
    <PackageReference Include="TDengine.Connector" Version="3.1.*" />
  </ItemGroup>
```

# 文档

- 开发示例请见[开发指南](https://docs.taosdata.com/develop/)
- 版本历史、TDengine 对应版本以及 API 说明请见[参考手册](https://docs.taosdata.com/reference/connector/csharp/)

# 贡献

鼓励每个人帮助改进这个项目，以下是这个项目的开发测试流程：

## 前置条件

* 安装 [.NET SDK](https://dotnet.microsoft.com/download)
* [Nuget 客户端](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools) （可选安装）
* 安装 TDengine 客户端驱动，具体步骤请参考[安装客户端驱动](https://docs.taosdata.com/develop/connect/#%E5%AE%89%E8%A3%85%E5%AE%A2%E6%88%B7%E7%AB%AF%E9%A9%B1%E5%8A%A8-taosc)

## 构建

1. `dotnet restore` 还原项目依赖。
2. `dotnet build --no-restore` 构建项目。

## 测试

1. 执行测试前确保已经安装 TDengine 服务端，并且已经启动 taosd 与 taosAdapter，数据库干净无数据
2. 项目目录下执行 `dotnet test` 运行测试，测试会连接到本地的 TDengine 服务器与 taosAdapter 进行测试

# 引用

- [TDengine 官网](https://www.taosdata.com/)
- [TDengine GitHub](https://github.com/taosdata/TDengine)

# 许可证

[MIT License](./LICENSE)