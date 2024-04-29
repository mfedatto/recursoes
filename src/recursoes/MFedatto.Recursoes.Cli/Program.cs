using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MFedatto.Recursoes.Cli;

Environment.SetEnvironmentVariable(
    "mfedatto.recursoes.benchmark.targetpath",
    @"M:\repos\github\microsoft\dotnet");

Summary summary = BenchmarkRunner.Run<BenchmarkMethods>(
    ManualConfig
        .Create(DefaultConfig.Instance)
        .WithOptions(ConfigOptions.JoinSummary | ConfigOptions.DisableLogFile));
