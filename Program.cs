﻿using System.IO;
using System.Net.Http;
using System.Threading.Tasks;  
using Microsoft.Extensions.Configuration;  
using Microsoft.Extensions.DependencyInjection;  
using Microsoft.Extensions.Hosting;  
using Microsoft.Extensions.Logging;
using Serilog;  
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Skraper3 
{  
    class Program  
    {  
        static async Task Main(string[] args)  
        {  
            IHost host = new HostBuilder()  
                 .ConfigureHostConfiguration(configHost =>  
                 {  
                     configHost.SetBasePath(Directory.GetCurrentDirectory());  
                     configHost.AddEnvironmentVariables(prefix: "ASPNETCORE_");  
                     configHost.AddCommandLine(args);  
                 })  
                 .ConfigureAppConfiguration((hostContext, configApp) =>  
                 {  
                     configApp.SetBasePath(Directory.GetCurrentDirectory());  
                     configApp.AddEnvironmentVariables(prefix: "ASPNETCORE_");  
                     configApp.AddJsonFile($"appsettings.json", true);
                     configApp.AddJsonFile($"AWSConfig.json", true);
                     configApp.AddCommandLine(args);  
                 })  
                .ConfigureServices((hostContext, services) =>  
                {  
                    services.AddLogging();  
                    services.AddHostedService<Skraper3Service>();  
                    var dataSrc = hostContext.Configuration["DBFilePath"];
                    services.AddDbContext<SubscriptionsContext>(options =>
                        options.UseSqlite($"Data Source={dataSrc}"));
                })  
                .ConfigureLogging((hostContext, configLogging) =>  
                {  
                    configLogging.AddSerilog(new LoggerConfiguration()  
                              .ReadFrom.Configuration(hostContext.Configuration)  
                              .CreateLogger());  
                    configLogging.AddConsole();  
                    configLogging.AddDebug();  
                })  
                .Build();  
  
            await host.RunAsync();  
        }  
  
  
    }  
} 