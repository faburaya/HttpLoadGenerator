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

    private static List<Task<AccumulatedStats>> DropFinishedLoopsAndCollectStats(
        List<Task<AccumulatedStats>> requestLoops, ref AccumulatedStats finalStats)
    {
        List<Task<AccumulatedStats>> runningLoops = new();
        foreach (Task<AccumulatedStats> loop in requestLoops)
        {
            if (loop.IsCompleted)
            {
                finalStats += loop.Result;
            }
            else
            {
                runningLoops.Add(loop);
            }
        }
        return runningLoops;
    }

    static void Main(string[] args)
    {
        double? targetRps = GetTargetRpsFromCommandLine(args);
        if (targetRps == null)
            return;

        using RequestRateController requestRateController = new(targetRps.Value);
        using ServiceProvider serviceProvider = CreateServiceProvider(requestRateController);

        Console.WriteLine("Sending requests... (CTRL+C to stop)");

        List<Task<AccumulatedStats>> requestLoops =
            new() { StartRequestLoop(serviceProvider) };

        double peakRps = 0.0;
        AccumulatedStats finalStats = new();
        while (InterruptSignalTrap.CanContinue())
        {
            double currentRps = requestRateController.WaitAndGetNextRps();
            requestLoops = DropFinishedLoopsAndCollectStats(requestLoops, ref finalStats);
            peakRps = Math.Max(peakRps, currentRps);

            if (currentRps < targetRps * 0.95 || requestLoops.Count == 0)
            {
                requestLoops.Add(StartRequestLoop(serviceProvider));
            }

            if (InterruptSignalTrap.CanContinue())
            {
                Console.WriteLine(
                    $"Running {requestLoops.Count} request loops - Target rate = {targetRps:F3} rps / Current rate = {currentRps:F3} rps");
            }
        }

        ReportStatistics(finalStats, peakRps);
    }

    private static void ReportStatistics(AccumulatedStats stats, double peakRps)
    {
        Console.WriteLine();
        Console.WriteLine($"API test ran for {stats.TotalElapsedTime}");
        Console.WriteLine($"Average rate = {stats.AverageRequestRatePerSec} rps");
        Console.WriteLine($"Peak rate = {peakRps:F3} rps");
        Console.WriteLine($"A total of {stats.CountTotalRequests} requests have been sent:");
        Console.WriteLine($"% of HTTP status not OK = {100.0 * stats.CountNotOkayHttpResponses / stats.CountTotalRequests:F1}");
        Console.WriteLine($"% of Unsuccessful responses = {100.0 * stats.CountNotSuccessfulResponses / stats.CountTotalRequests:F1}");
    }
}