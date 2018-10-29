using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
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
        private readonly HttpClient client;

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
            //Intialize HttpClient we will use
            this.client = new HttpClient();
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
            this.timer = new Timer(DoWork, null, 0, -1);
        }

        private async void DoWork(object state)
        {
            //TODO: make 15 second default
            //TODO: Loop through json file with endpoints and emails and make requests async
            //Send emails + texts, file to loop through in appsettings.json as a config item.
            //Save last in dictionary



            var request = new HttpRequestMessage(HttpMethod.Get, 
                "https://gist.githubusercontent.com/iagox86/4554283/raw/48dac9e2b6ca22f06785b1b49eae11fc81314955/tests.txt");
            request.Headers.Add("User-Agent", "HttpClientFactory-Sample");
            HttpResponseMessage response = await client.SendAsync(request);  
            var responseStr = await response.Content.ReadAsStringAsync();  
            Console.Write(responseStr);


            
            this.timer = new Timer(DoWork, null, this.frequencyInMilliseconds, -1);
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