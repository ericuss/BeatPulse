[![Build status](https://ci.appveyor.com/api/projects/status/fqcak0q6q83f730c?svg=true)](https://ci.appveyor.com/project/Xabaril/beatpulse) [![NuGet](https://img.shields.io/nuget/v/BeatPulse.svg)](https://www.nuget.org/packages/BeatPulse/)

[![Build history](https://buildstats.info/appveyor/chart/xabaril/beatpulse)](https://ci.appveyor.com/project/xabaril/beatpulse/history)

# Beat Pulse

Health checking is the process where load balancers or application delivery controller does periodic check on our applications to make sure that they are up and responding without any problems. If our applications are down for every reason or any of the system that our applications depends on (A database, a distributed cache, web service, ect) are down, the load balancer, k8s... should detect this and stop sending traffic its way and try to restore the services manually or automatically in case of k8s.

*BeatPulse* is a simple liveness, readiness library for .NET Core Applications. 

## What is the motivation behind it

The [Microsoft HealthCheck](https://github.com/dotnet-architecture/HealthChecks) library is not an active project right now and is not included  in ASP.NET Core 2.1.

## Getting Started

1. Install the Nuget Package into your ASP.NET Core application.

``` PowerShell
Install-Package BeatPulse
```

2. Install the liveness libraries that you need on your project. At this moment *BeatPulse* contains libraries for *Redis, SqlServer, MongoDb, Postgress Sql, Azure Storage (Blobs, Tables and Queues), Azure Service Bus (Event Hub, Service Bus queues and topics), DocumentDb, MySQL, SqLite, Kafka, RabbitMQ, Identity Server and custom lambda liveness*.

``` PowerShell
Install-Package BeatPulse.SqlServer
Install-Package BeatPulse.MongoDb
Install-Package BeatPulse.Npgsql
Install-Package BeatPulse.Redis
Install-Package BeatPulse.AzureStorage
Install-Package BeatPulse.AzureServiceBus
Install-Package BeatPulse.MySql
Install-Package BeatPulse.DocumentDb
Install-Package BeatPulse.SqLite
Install-Package BeatPulse.Kafka
Install-Package BeatPulse.RabbitMQ
Install-Package BeatPulse.IdSvr
```

3. Add *BeatPulse* into your ASP.NET Core project. *UseBeatPulse* is a new IWebHostBuilder extension method to register and configure BeatPulse.

``` csharp
 public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
               .UseBeatPulse(options=>
                {
                   options.SetAlternatePath("health") //default hc
                        .SetTimeout(milliseconds:1500) // default -1 infinitely
                        .EnableDetailedOutput(); //default false
                }).UseStartup<Startup>().Build();
```

4. Add *BeatPulseService* and set the liveness libraries to be used.

``` csharp
    services.AddBeatPulse(setup =>
    {
        //add sql server liveness
        setup.AddSqlServer("your-connection-string");
    });
```

5. Request *BeatPulse* to get liveness results.

``` bash
curl http://your-domain/hc 
GET /hc HTTP/1.1
Host: your-domain
User-Agent: curl/7.49.0
Accept: */*
HTTP/1.1 200 OK
OK
```

For more information about *BeatPulse* configuration and other features ( cache, authentication, etc ) see the [specific documentation section](./doc/beatpulse.md).

## UI

The project BeatPulse.UI is a minimal UI interface that store and shows the liveness results from the configured liveness uri's. To integrate BeatPulse.UI in your project you just need to add the BeatPulse.UI services and middlewares.

```csharp
    public class Startup
    {       
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBeatPulseUI();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseBeatPulseUI();
        }
    }
```

This automatically registers a new interface on **/beatpulse-ui**. 

> Optionally, *UseBeatPulseUI* can be configured with different UI response path.

![BeatPulseUI](./doc/BeatPulseUI-1.PNG)

**BeatPulseUI** exist also as *docker image*  You can read more about [beat pulse ui docker image](./doc/ui-docker.md).

### Configuration

The liveness to be used on BeatPulse-UI are configured using the **BeatPulse-UI** settings.

```json
{
  "BeatPulse-UI": {
    "Liveness": [
      {
        "Name": "HTTP-Api-Basic",
        "Uri": "http://localhost:6457/health"
      }
    ],
    "Webhooks": [
      {
        "Name": "",
        "Uri": "",
        "Payload": ""
      }
    ],
    "EvaluationTimeOnSeconds": 10,
    "MinimumSecondsBetweenFailureNotifications":60
  }
}
```

    1.- Liveness: The collection of liveness uris to watch.
    2.- EvaluationTimeOnSeconds: Number of elapsed seconds between liveness checks.
    3.- Webhooks: If any liveness return a *Down* result, this collections will be used to notify the error status. (Payload is the json payload and must be scape. For mor information see Notifications section)
    4.- MinimumSecondsBetweenFailureNotifications: The minimun seconds between failure notifications in order not flooding the notification receiver.

All liveness results are stored into a SqLite database persisted to disk with *livenessdb* name.

### Failure Notifications

If the **WebHooks** section is configured, BeatPulse-UI automatically post a new notification into the webhook collection. BeatPulseUI uses a simple replace method for values in the webhook's **Payload** property. At this moment we support two bookmarks:

    1.- [[LIVENESS]] The name of the liveness that returns *Down*.
    2.- [[FAILURE]] A detail message with the failure.

The [web hooks section](./doc/webhooks.md) contain more information and webhook samples for Microsoft Teams, Azure Functions, Slack and more.

## Tracking pulses

Additionally you can save liveness information in external services. Currently we have developed a tracker for Application Insights you can use. To install this package:

``` Powershell
Install-Package BeatPulse.Tracker.ApplicationInsights
```

### Application Insights tracker configuration

In the configuration section where you configured your *BetPulseService* add the tracker:
``` csharp
 services.AddBeatPulse(setup =>
    {
        //Add Application Insights tracker
        setup.AddTracker(new ApplicationInsightsTracker());
    });
``` 
It will use your current Application Insights resource by default.

If you were not using previously Application Insights in your project, or if you prefer to use a distinct Application Insights resource for liveness, you can use a different Instrumentation Key in the constructor.
``` csharp
 services.AddBeatPulse(setup =>
    {
        //Add Application Insights tracker
        setup.AddTracker(new ApplicationInsightsTracker("instrumentation-key"));
    });
``` 
The information will be saved to Application Insights as *custom events* using event name *BeatPulse*, and will store this information:
- *Name*: configured liveness name.
- *Message*: message from liveness.
- *IsHealthy*: boolean with healthy info from liveness.
- *Run*: boolean indicator if liveness has run.
- *Milliseconds*: milliseconds with the liveness call duration.

## Contributing 

BeatPulse wouldn't be possible without the time and effort of its contributors. The team is made up of Unai Zorrilla Castro @unaizorrilla, Luis Ruiz Pavón @lurumad, Carlos Landeras @carloslanderas and Eduard Tomás @eiximenis.

*Our valued committers are*: Hugo Biarge @hbiarge, Matt Channer @mattchanner.

If you want to contribute to a project and make it better, your help is very welcome. You can contribute with helpful bug reports, feature request and also new features with pull requests.

1. Read and follow the [Don't push your pull requests](https://www.igvita.com/2011/12/19/dont-push-your-pull-requests/)
2. Build.ps1 is working on local and AppVeyor.
3. Follow the code guidelines and conventions.
4. New features is not only code, tests and documentation is also mandatory.
