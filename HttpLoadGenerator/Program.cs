using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HttpLoadGenerator.Config;
using HttpLoadGenerator.DTO;
using HttpLoadGenerator.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpLoadGenerator;

internal class Program
{
    private const string _usageMessage = "Usage: executable target_request_rate_per_sec";

    private static double? GetTargetRpsFromCommandLine(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Wrong number of command line arguments!");
            Console.WriteLine(_usageMessage);
            return null;
        }

        if (!double.TryParse(args[0], out double targetRps))
        {
            Console.WriteLine("Could not parse target rate of requests per second!");
            Console.WriteLine(_usageMessage);
            return null;
        }

        if (targetRps <= 0.0)
        {
            Console.WriteLine("Invalid target RPS!");
            Console.WriteLine(_usageMessage);
            return null;
        }

        return targetRps;
    }

    private static ServiceProvider CreateServiceProvider(
        RequestRateController requestRateController)
    {
        IConfigurationRoot configurationRoot =
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

        ServiceCollection services = new();
        services
            .AddSingleton<IRequestRateController>(requestRateController)
            .AddSingleton<IRequestRateReader>(requestRateController)
            .AddScoped<ApiRequestLoop>()
            .AddScoped<IApiServiceClient, ApiServiceClient>()
            .AddHttpClient<ApiServiceClient>();

        services
            .AddOptions<ApiClientConfig>()
            .Bind(configurationRoot.GetSection(ApiClientConfig.SectionName));

        return services.BuildServiceProvider();
    }

    private static async Task<AccumulatedStats> StartRequestLoop(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApiRequestLoop loop = serviceProvider.GetRequiredService<ApiRequestLoop>();
        return await loop.TestApi();
    }

    private static AccumulatedStats DropFinishedLoopsAndCollectStats(
        ref List<Task<AccumulatedStats>> requestLoops)
    {
        List<Task<AccumulatedStats>> runningLoops = new();
        AccumulatedStats stats = new();

        foreach (Task<AccumulatedStats> loop in requestLoops)
        {
            if (loop.IsCompleted)
            {
                stats += loop.Result;
            }
            else
            {
                runningLoops.Add(loop);
            }
        }

        requestLoops = runningLoops;
        return stats;
    }

    static void Main(string[] args)
    {
        double? targetRps = GetTargetRpsFromCommandLine(args);
        if (targetRps == null)
            return;

        using RequestRateController requestRateController = new(targetRps.Value);
        using ServiceProvider serviceProvider = CreateServiceProvider(requestRateController);

        DateTime startTime = DateTime.Now;

        List<Task<AccumulatedStats>> requestLoops =
            new() { StartRequestLoop(serviceProvider) };

        Console.WriteLine("Sending requests... (CTRL+C to stop)");

        InterruptSignalTrap.OnSignalInterception(() =>
        {
            requestRateController.StopTicketDistribution();
            Console.WriteLine("\nProgram interruption signal captured: stopping...");
        });

        AccumulatedStats finalStats = new();

        while (InterruptSignalTrap.CanContinue())
        {
            double evaluatedRps = requestRateController.WaitAndGetNextRps();
            int loopCountAtEvaluation = requestLoops.Count;
            finalStats += DropFinishedLoopsAndCollectStats(ref requestLoops);

            if (evaluatedRps < targetRps * 0.95 || requestLoops.Count == 0)
            {
                int estimatedNecessaryLoops =
                    (int)Math.Floor(0.8 * loopCountAtEvaluation * targetRps.Value / evaluatedRps);

                int countExtraLoops = Math.Max(1, estimatedNecessaryLoops - requestLoops.Count);
                for (int n = 0; n <  countExtraLoops; ++n)
                {
                    requestLoops.Add(StartRequestLoop(serviceProvider));
                }
            }

            if (InterruptSignalTrap.CanContinue())
            {
                Console.WriteLine(
                    $"Running {loopCountAtEvaluation} request loops - Target rate = {targetRps:F3} rps / Current rate = {evaluatedRps:F3} rps");
            }
        }

        Task.WaitAll(requestLoops.ToArray());
        TimeSpan elapsedTime = DateTime.Now - startTime;
        finalStats += DropFinishedLoopsAndCollectStats(ref requestLoops);

        ReportStatistics(finalStats, elapsedTime);
    }

    private static void ReportStatistics(
        AccumulatedStats stats, TimeSpan elapsedTime)
    {
        double averageRps = stats.CountTotalRequests / elapsedTime.TotalSeconds;

        double percentageHttpNotOkay =
            100.0 * stats.CountNotOkayHttpResponses / stats.CountTotalRequests;

        double percentageNotSuccessful =
            100.0 * stats.CountNotSuccessfulResponses / stats.CountTotalRequests;

        Console.WriteLine();
        Console.WriteLine($"API test ran for {elapsedTime}");
        Console.WriteLine($"Average rate = {averageRps:F3} rps");
        Console.WriteLine($"A total of {stats.CountTotalRequests} requests have been sent:");
        Console.WriteLine($"% of HTTP status not OK = {percentageHttpNotOkay:F1}");
        Console.WriteLine($"% of Unsuccessful responses = {percentageNotSuccessful:F1}");
        Console.WriteLine();
    }
}