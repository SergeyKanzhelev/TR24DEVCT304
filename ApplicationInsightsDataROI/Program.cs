using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ApplicationInsightsDataROI
{
    class Program
    {

        static void Main(string[] args)
        {
            var state = new State();
            state.IsTerminated = false;

            // Create a timer with a two second interval.
            state._timer = new System.Timers.Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
            // Hook up the Elapsed event for the timer. 
            state._timer.Elapsed += state.OnTimedEvent;
            state._timer.AutoReset = true;
            state._timer.Enabled = true;

            TelemetryConfiguration configuration = new TelemetryConfiguration();
            configuration.InstrumentationKey = "DEMO_KEY";

            var telemetryChannel = new ServerTelemetryChannel();
            telemetryChannel.Initialize(configuration);
            configuration.TelemetryChannel = telemetryChannel;
            

            // data collection modules
            var dependencies = new DependencyTrackingTelemetryModule();
            dependencies.Initialize(configuration);

            // telemetry initializers
            configuration.TelemetryInitializers.Add(new MyTelemetryInitializer());

            // telemetry processors
            configuration.TelemetryProcessorChainBuilder
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, state.Sent); })
                .Use((next) => { return new MyTelemetryProcessor(next); })
                .Use((next) => { return new AdaptiveSamplingTelemetryProcessor(next) {
                    InitialSamplingPercentage = 10,
                    SamplingPercentageIncreaseTimeout = TimeSpan.FromSeconds(5),
                    SamplingPercentageDecreaseTimeout = TimeSpan.FromSeconds(5),
                    MaxTelemetryItemsPerSecond = 5,
                    EvaluationInterval = TimeSpan.FromSeconds(10) };})
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, state.Collected); })
                .Build();

            TelemetryClient client = new TelemetryClient(configuration);

            var iterations = 0;

            while (!state.IsTerminated)
            {
                client.TrackEvent("test");
                iterations++;

                try
                {
                    HttpClient http = new HttpClient();
                    var task = http.GetStringAsync("http://bing.com");
                    task.Wait();

                    //client.TrackMetric("Response size", task.Result.Length);
                    //client.TrackMetric("Successful responses", 1);
                }
                catch (Exception exc)
                {
                    //client.TrackMetric("Successful responses", 0);
                }
            }

            Console.WriteLine($"Finished in {iterations} iterations!");
            Console.ReadLine();
        }
    }
}
