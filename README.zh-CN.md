<div align="center">

# Majo.Command

**一个轻量、支持类型化参数的 .NET 命令树类库。**

通过流畅的 Builder API 定义命令、解析位置参数、执行异步处理函数，并根据命令树自动生成帮助信息。

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

[English](./README.md) · **简体中文**

</div>

---

## 项目简介

`Majo.Command` 为 .NET 应用提供命令注册、解析和执行能力。通过命令关键字与类型化位置参数构建命令树，即可在控制台、聊天应用或其他输入来源中复用同一套命令定义。

使用流程非常简单：**注册命令、构建命令树、传入执行上下文并执行命令。**

## 主要特性

- 使用 `Literal(...)`、`Argument(...)` 和 `Executes(...)` 声明命令
- 不区分大小写的命令关键字与类型化位置参数
- 通过 `ISpanParsable<T>` 解析常见类型
- 自定义参数解析器与正则表达式校验
- 支持包含空格的引号参数，以及接收剩余输入的末尾参数
- 支持取消令牌的异步命令处理函数
- 通过执行上下文配置命令输出回调
- 明确的匹配结果与按名称读取的类型化参数
- 自动生成帮助信息，并紧凑展示可选的末尾参数
- 可在独立请求之间共享的冻结命令树

## 环境要求

- **.NET 10**

## 快速开始

注册一条命令，构建命令树并执行：

```csharp
using Majo.Command;

var builder = new CommandTreeBuilder<CommandContext>();

builder.Literal("say", say =>
{
    say.Argument("message", message =>
    {
        message.Executes(PrintAsync, "Prints a message.");
        message.Argument("tag", tag => tag.Executes(PrintAsync));
    });
});

var tree = builder.Build();
var context = new CommandContext((level, text, tag) => Console.WriteLine($"[{level}] [{tag}] {text}"));

var result = await tree.ExecuteAsync("say \"Hello world\" Greeting", context);
if (!result.Success)
    Console.Error.WriteLine(result.ErrorMessage);

Console.WriteLine(tree.GetHelp("say"));

static Task PrintAsync(CommandInvocation<CommandContext> call, CancellationToken ct)
{
    string message = call.Get<string>("message");
    string tag = call.TryGet<string>("tag", out var suppliedTag) ? suppliedTag : "Command";
    call.Context.Output(OutputLevel.Information, message, tag);
    return Task.CompletedTask;
}
```

示例输出：

```text
[Information] [Greeting] Hello world
say <message> [tag] - Prints a message.
```

也可以执行 `say "Hello world"`，此时使用默认 Tag `Command`。

## 注册命令

### 命令关键字与类型化参数

`Literal(...)` 用于添加命令关键字。`Argument(...)` 添加字符串参数；`Argument<T>(...)` 使用 `ISpanParsable<T>` 和固定区域性（Invariant Culture）解析指定类型。

例如，命令可以组合多个关键字和数值参数：

```csharp
builder.Literal("user", user =>
{
    user.Literal("show", show =>
    {
        show.Argument<long>("id", id =>
        {
            id.Executes((call, ct) =>
            {
                long userId = call.Get<long>("id");
                call.Context.Output(OutputLevel.Information, $"User ID: {userId}", "User");
                return Task.CompletedTask;
            }, "Shows a user ID.");
        });
    });
});
```

所有命令分支应在调用 `Build()` 之前完成注册。Literal 名称匹配不区分大小写。一个节点可以拥有多个 Literal 子分支和一个位置参数子分支，匹配时优先选择 Literal。

### 可选末尾参数

在注册了 `Executes(...)` 的节点处，命令即可结束。为连续的参数节点注册处理函数，就能同时支持较短和较长的命令格式：

```text
say "Hello world"
say "Hello world" Greeting
```

帮助信息会将这种末尾参数显示为可选形式：`say <message> [tag]`。

### 自定义解析与校验

通过 `CommandValueParser<T>` 可以解析自定义类型。对于字符串参数，可以传入 `Regex` 校验完整参数值：

```csharp
using System.Text.RegularExpressions;

builder.Literal("lookup", lookup =>
{
    lookup.Argument("code", code =>
    {
        code.Executes((call, ct) =>
        {
            call.Context.Output(OutputLevel.Information, call.Get<string>("code"), "Lookup");
            return Task.CompletedTask;
        });
    }, regex: new Regex("[A-Z]{3}[0-9]{2}"));
});
```

若需要将命令末尾的全部文本作为一个参数，可以为最后一个参数设置 `consumeRemaining: true`。

## 命令匹配与执行

`Match(...)` 用于解析命令，并返回参数及匹配状态：

```csharp
CommandMatch<CommandContext> match = tree.Match("say hello");

if (match.Success)
    Console.WriteLine(match.Get<string>("message"));
else
    Console.Error.WriteLine($"{match.Error}: {match.ErrorMessage}");
```

`Get<T>(name)` 按名称读取已解析参数，`TryGet<T>(name, out value)` 适合读取可能省略的参数。

通过 `ExecuteAsync(input, context, cancellationToken)` 匹配并执行已注册的异步处理函数。返回的 `CommandMatch<TContext>` 提供命令匹配结果。处理函数抛出的异常会传递给调用方，以便应用按自身规则处理。

匹配失败可以通过 `CommandError` 区分，例如未知命令、参数无效、命令不完整、额外参数和语法错误。

## 帮助信息

`GetHelp()` 根据已注册的可执行命令和描述自动生成帮助内容：

```csharp
tree.GetHelp();
tree.GetHelp("user");
```

可选的筛选参数按 Literal 关键字路径选择命令。连续可执行的参数节点会被紧凑地显示为：

```text
say <message> [tag] - Prints a message.
```

如果希望用户通过 `help` 命令查看帮助，只需注册一个调用 `GetHelp()` 的处理函数。

## 执行上下文

`CommandContext` 提供接收 `OutputLevel`、消息文本和 Tag 的输出回调：

```csharp
var context = new CommandContext((level, text, tag) =>
{
    Console.WriteLine($"[{level}] [{tag}] {text}");
});
```

应用可以将回调接入日志或消息处理设施。需要更多业务数据时，可以继承 `CommandContext`，让处理函数获取自定义上下文信息。

命令树构建完成后可以被多个请求复用。每次匹配都会产生独立的参数数据，执行时也可以传入各自的上下文，适合处理多个互相独立的命令请求。

## 许可证

参阅 [LICENSE.txt](./LICENSE.txt)。

---

<div align="center">

作为 Majo 系列项目中一个专注的命令处理组件。

[English](./README.md)

</div>
