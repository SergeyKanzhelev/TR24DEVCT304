﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;


namespace ApplicationInsightsDataROI
{
    class Demo4
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

            // initialize state for the telemetry size calculation
            ProcessedItems CollectedItems = new ProcessedItems();
            ProcessedItems SentItems = new ProcessedItems();

            // enable sampling
            configuration.TelemetryProcessorChainBuilder
                // this telemetry processor will be executed first for all telemetry items to calculate the size and # of items
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, CollectedItems); })

                // filter telemetry that is faster than 100 msec
                //.Use((next) => { return new DependencyFilteringTelemetryProcessor(next); })

                // filter telemetry that is faster than 100 msec
                // extract metrics
                .Use((next) => { return new DependencyFilteringWithMetricsTelemetryProcessor(next, configuration); })

                // this telemetry processor will be execuyted ONLY when telemetry is sampled in
                .Use((next) => { return new PriceCalculatorTelemetryProcessor(next, SentItems); })
                .Build();


            TelemetryClient client = new TelemetryClient(configuration);

            var iteration = 0;


            while (true)
            {
                using (var operaiton = client.StartOperation<RequestTelemetry>("Process item"))
                {
                    client.TrackEvent("IterationStarted", new Dictionary<string, string>() { { "iteration", iteration.ToString() } });
                    client.TrackTrace($"Iteration {iteration} started", SeverityLevel.Information);

                    try
                    {
                        var task = (new HttpClient()).GetStringAsync("http://bing.com");
                        task.Wait();
                    }
                    catch (Exception exc)
                    {
                        client.TrackException(exc);
                        operaiton.Telemetry.Success = false;
                    }

                    client.StopOperation(operaiton);
                    Console.WriteLine($"Iteration {iteration}. Elapesed time: {operaiton.Telemetry.Duration}. Collected Telemetry: {CollectedItems.size}/{CollectedItems.count}. Sent Telemetry: {SentItems.size}/{SentItems.count}. Ratio: {1.0 * CollectedItems.size / SentItems.size}");
                    iteration++;
                }
            }
        }
    }
}
