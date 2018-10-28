using Microsoft.Extensions.Configuration;  
using Microsoft.Extensions.Hosting;  
using Microsoft.Extensions.Logging;  
using System.Threading;  
using System.Threading.Tasks;  
  
namespace Skraper3 
{
    public class Skraper3Service : IHostedService  
    {  
        IApplicationLifetime appLifetime;  
        ILogger<Skraper3Service> logger;  
        IHostingEnvironment environment;  
        IConfiguration configuration;  
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
  
            return Task.CompletedTask;  
  
        }  
  
        private void OnStarted()  
        {  
            this.logger.LogInformation("OnStarted method called.");  
  
            this.logger.LogInformation("Hey I am new");
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