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

    static void Main(string[] args)
    {
        bool keepRunning = true;
        Console.CancelKeyPress += delegate { keepRunning = false; };

        double? targetRps = GetTargetRpsFromCommandLine(args);
        if (targetRps == null)
            return;

        using RequestRateController requestRateController = new(targetRps.Value);
        using ServiceProvider serviceProvider = CreateServiceProvider(requestRateController);

        List<Task<AccumulatedStats>> requestLoops =
            new() { StartRequestLoop(serviceProvider) };

        double peakRps = 0.0;
        AccumulatedStats finalStats = new();
        while (keepRunning)
        {
            double currentRps = requestRateController.WaitAndGetNextRps();
            peakRps = Math.Max(peakRps, currentRps);
            Console.Write(
                $"\rTarget rate of requests = {targetRps:F3} rps / Current rate = {currentRps} rps");

            if (currentRps < targetRps * 0.95)
            {
                requestLoops.Add(StartRequestLoop(serviceProvider));
            }

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
            requestLoops = runningLoops;
        }

        Console.WriteLine('\r');
        ReportStatistics(finalStats);
        Console.WriteLine($"Peak rate was {peakRps} rps");
    }

    private static void ReportStatistics(AccumulatedStats stats)
    {
        Console.WriteLine($"API test ran for {stats.TotalElapsedTime}");
        Console.WriteLine($"A total of {stats.CountTotalRequests} have been sent");
        Console.WriteLine($"{100.0 * stats.CountNotOkayHttpResponses / stats.CountTotalRequests} % of HTTP status not OK");
        Console.WriteLine($"{100.0 * stats.CountNotSuccessfulResponses / stats.CountTotalRequests} % of unsuccessful responses");
        Console.WriteLine($"Average rate was {stats.AverageRequestRatePerSec} rps");
    }
}