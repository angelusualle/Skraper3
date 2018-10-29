using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Skraper3 
{
    public class Skraper3Service : IHostedService  
    {  
        private IApplicationLifetime appLifetime;  
        private ILogger<Skraper3Service> logger;  
        private IHostingEnvironment environment;  
        private IConfiguration configuration;  
        private Timer timer;

        private int frequencyInMilliseconds;
        public Skraper3Service(  
            IConfiguration configuration,  
            IHostingEnvironment environment,  
            ILogger<Skraper3Service> logger,   
            IApplicationLifetime appLifetime)  
        {  
            this.configuration = configuration;  
            this.logger = logger;  
            this.appLifetime = appLifetime;  
            this.environment = environment;  
        }  
  
        public Task StartAsync(CancellationToken cancellationToken)  
        {  
            this.logger.LogInformation("StartAsync method called.");  
  
            this.appLifetime.ApplicationStarted.Register(OnStarted);  
            this.appLifetime.ApplicationStopping.Register(OnStopping);  
            this.appLifetime.ApplicationStopped.Register(OnStopped);  

            //Get configuration from appsettings.json default to 5 second
            Int32.TryParse(this.configuration["FrequencyInMilliseconds"], out this.frequencyInMilliseconds);
            if (this.frequencyInMilliseconds <= 0) this.frequencyInMilliseconds = 5000;
            
            return Task.CompletedTask;  
  
        }  
  
        private void OnStarted()  
        {  
            this.logger.LogInformation("OnStarted method called.");  
            this.logger.LogInformation("Timed Background Service is starting.");
            this.timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromMilliseconds(this.frequencyInMilliseconds));
        }

        private void DoWork(object state)
        {
            
        }

        private void OnStopping()  
        {  
            this.logger.LogInformation("OnStopping method called.");  
  
            // On-stopping code goes here  
        }  
  
        private void OnStopped()  
        {  
            this.logger.LogInformation("OnStopped method called.");  
  
            // Post-stopped code goes here  
        }  
  
  
        public Task StopAsync(CancellationToken cancellationToken)  
        {  
            this.logger.LogInformation("StopAsync method called.");  
  
            return Task.CompletedTask;  
        }  
    }  
}  