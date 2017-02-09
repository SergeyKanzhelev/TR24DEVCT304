using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
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
    class Demo6
    {
        public static void Run()
        {

            TelemetryConfiguration configuration = new TelemetryConfiguration();
            configuration.InstrumentationKey = "fb8a0b03-235a-4b52-b491-307e9fd6b209";

            var telemetryChannel = new ServerTelemetryChannel();
            telemetryChannel.Initialize(configuration);
            configuration.TelemetryChannel = telemetryChannel;

            // automatically track dependency calls
            var dependencies = new DependencyTrackingTelemetryModule();
            dependencies.Initialize(configuration);

            // automatically correlate all telemetry data with request
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            // set default properties for all telemetry items
            configuration.TelemetryInitializers.Add(new DefaultTelemetryInitializer());

            // send all event telemetry to a different iKey
            configuration.TelemetryInitializers.Add(new BusinessTelemetryInitializer());

            // configure all telemetry to be sent to a single node
            configuration.TelemetryInitializers.Add(new NodeNameTelemetryInitializer());

            // initialize price calculation logic
            var state = new State();
            state.Initialize();

            QuickPulseTelemetryProcessor processor = null;

            // enable sampling
            configuration.TelemetryProcessorChainBuilder

                //adding LiveMetrics telemetry processor
                .Use((next) =>
                {
                    processor = new QuickPulseTelemetryProcessor(next);
                    return processor;
                })

                // this telemetry processor will be executed first for all telemetry items to calculate the size and # of items
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, state.Collected); })

                // sample all telemetry to 10%
                .Use((next) =>
                {
                    return new SamplingTelemetryProcessor(next)
                    {
                        SamplingPercentage = 1
                    };
                })

                // this telemetry processor will be execuyted ONLY when telemetry is sampled in
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, state.Sent); })
                .Build();

            var QuickPulse = new QuickPulseTelemetryModule();
            QuickPulse.Initialize(configuration);
            QuickPulse.RegisterTelemetryProcessor(processor);

            TelemetryClient client = new TelemetryClient(configuration);

            var iterations = 0;

            // configure metrics collection
            MetricManager metricManager = new MetricManager(client);
            var itemsProcessed = metricManager.CreateMetric("Items processed");
            var processingFailed = metricManager.CreateMetric("Failed processing");
            var processingSize = metricManager.CreateMetric("Processing size");

            while (!state.IsTerminated)
            {

                iterations++;

                using (var operaiton = client.StartOperation<RequestTelemetry>("Process item"))
                {
                    client.TrackEvent("test");
                    client.TrackTrace("Something happened", SeverityLevel.Information);

                    try
                    {
                        HttpClient http = new HttpClient();
                        var task = http.GetStringAsync("http://bing.com");
                        task.Wait();

                        // metrics aggregation. Metrics are aggregated and sent once per minute
                        itemsProcessed.Track(1);
                        processingSize.Track(task.Result.Length);
                        processingFailed.Track(0);

                        // raw metric telemetry. Each call represents a document.
                        client.TrackMetric("[RAW] Response size", task.Result.Length);
                        client.TrackMetric("[RAW] Successful responses", 1);

                    }
                    catch (Exception exc)
                    {
                        // raw metric telemetry
                        client.TrackMetric("[RAW] Successful responses", 0);

                        // metrics aggregation:
                        processingFailed.Track(1);

                        client.TrackException(exc);
                        operaiton.Telemetry.Success = false;
                    }

                    //                    client.StopOperation(operaiton);
                    //                    Console.WriteLine($"Iteration {iterations}. Elapesed time: {operaiton.Telemetry.Duration}");

                }
            }

            // send all metrics before exiting the program
            metricManager.Flush();

            Console.WriteLine($"Program sent 100K of telemetry in {iterations} iterations!");
            Console.ReadLine();
        }
    }
}
