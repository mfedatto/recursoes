using System.Collections;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Newtonsoft.Json;

namespace MFedatto.Recursoes.Cli;

[MemoryDiagnoser]
[DisassemblyDiagnoser]
[ThreadingDiagnoser]
[HardwareCounters(
    HardwareCounter.BranchMispredictions,
    HardwareCounter.BranchInstructions)]
public class BenchmarkMethods
{
    private static readonly SHA256 Sha256 = SHA256.Create();
    private readonly string _targetPath;

    public BenchmarkMethods()
    {
        _targetPath = Environment.GetEnvironmentVariable("mfedatto.recursoes.benchmark.targetpath");
    }
    
    [Benchmark(Baseline = true)]
    public void Recursion()
    {
        WriteJsonFile(
            RecursionChecksum(_targetPath!)
                .OrderBy(iteration => iteration.Path)
                .ToArray(),
            "./recursion-checksums.json");

        return;

        IEnumerable<FileChecksum> RecursionChecksum(
            string iterationPath)
        {
            List<FileChecksum> fileChecksumsList = [];

            fileChecksumsList.AddRange(
                Directory.GetFiles(_targetPath!)
                    .Select(filePath =>
                        new FileChecksum(
                            Path.GetFileName(filePath),
                            Path.GetFullPath(filePath),
                            ChecksumFile(filePath))));

            foreach (
                string directory
                in Directory.GetDirectories(iterationPath))
            {
                fileChecksumsList.AddRange(
                    RecursionChecksum(directory));
            }

            return fileChecksumsList.ToArray();
        }
    }

    [Benchmark]
    public void SimulatedRecursion()
    {
        List<FileChecksum> fileChecksumsList = [];
        List<ChecksumPathIteration> checksumPathIterationsList =
        [
            new ChecksumPathIteration(_targetPath!, 0)
        ];
        int currentIterationDepth = 0;

        while (ChecksumPathIterationsQuery(currentIterationDepth).Any())
        {
            List<ChecksumPathIteration> currentIterationPathsList = [];

            foreach (
                ChecksumPathIteration iterationPath
                in ChecksumPathIterationsQuery(currentIterationDepth))
            {
                currentIterationPathsList.AddRange(
                    Directory.GetDirectories(iterationPath.Path)
                        .Select(dirPath => new ChecksumPathIteration(dirPath, iterationPath.Depth + 1)));
            }

            checksumPathIterationsList.AddRange(currentIterationPathsList);

            currentIterationDepth++;
        }

        foreach (
            ChecksumPathIteration pathIteration
            in checksumPathIterationsList)
        {
            fileChecksumsList.AddRange(
                Directory.GetFiles(pathIteration.Path)
                    .Select(filePath =>
                        new FileChecksum(
                            Path.GetFileName(filePath),
                            Path.GetFullPath(filePath),
                            ChecksumFile(filePath))));
        }

        WriteJsonFile(
            fileChecksumsList
                .OrderBy(iteration => iteration.Path)
                .ToArray(),
            "./simulated-recursion-checksums.json");

        return;

        IQueryable<ChecksumPathIteration> ChecksumPathIterationsQuery(int depth) =>
            checksumPathIterationsList.AsQueryable()
                .Where(iteration => iteration.Depth == depth);
    }

    private static string ChecksumFile(
        string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);

        return BitConverter.ToString(
                Sha256.ComputeHash(stream))
            .Replace("-", string.Empty);
    }

    private static void WriteJsonFile(
        IEnumerable fileChecksums,
        string jsonFilePath)
    {
        File.WriteAllText(
            jsonFilePath,
            JsonConvert.SerializeObject(
                fileChecksums,
                Formatting.Indented));
    }

    private record ChecksumPathIteration(
        string Path,
        int Depth);

    private record FileChecksum(
        string Name,
        string Path,
        string Checksum);
}
