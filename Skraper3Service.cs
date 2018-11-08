using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

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
        private string SubscriptionsFileAndPath;

        private Dictionary<string, string> previousWebData;

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
            this.client = new HttpClient();
            this.previousWebData = new Dictionary<string, string>();
        }  
  
        public Task StartAsync(CancellationToken cancellationToken)  
        {  
            this.logger.LogInformation("StartAsync method called.");  
  
            this.appLifetime.ApplicationStarted.Register(OnStarted);  
            this.appLifetime.ApplicationStopping.Register(OnStopping);  
            this.appLifetime.ApplicationStopped.Register(OnStopped);  

            //Get configuration from appsettings.json default to 15 second
            Int32.TryParse(this.configuration["FrequencyInMilliseconds"], out this.frequencyInMilliseconds);
            if (this.frequencyInMilliseconds <= 0) this.frequencyInMilliseconds = 15000;
            this.SubscriptionsFileAndPath = this.configuration["SubscriptionsFileAndPath"] ?? "Subscriptions.JSON"; 
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
            List<Subscription> subs = null;
            //Get all subscriptions
            try {
                var jsonStr = System.IO.File.ReadAllText(this.SubscriptionsFileAndPath);
                subs = JsonConvert.DeserializeObject<List<Subscription>>(jsonStr);
            }
            catch (Exception e) {
                var errormsg = $"Exception In getting subscriptions from {this.SubscriptionsFileAndPath}. Service will stop.";
                errormsg += $"\n + {e}";
                this.logger.LogCritical(errormsg);
                await AlertAdminAsync(e);
                this.appLifetime.StopApplication();
            }

            //Loop through detect changes
            await DetectChangesInSubscriptions(subs);

            //Alert users of changes
            AlertUsersOfChange(subs.Where(s => s.Changed).ToList<Subscription>());

            //Set next iteration.
            this.timer = new Timer(DoWork, null, this.frequencyInMilliseconds, -1);


        }
        private async Task DetectChangesInSubscriptions(List<Subscription> subs)
        {
            try {
                foreach (var sub in subs)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, sub.URL);
                    request.Headers.Add("User-Agent", "HttpRequestMessage");
                    sub.response = client.SendAsync(request); 
                }
                Task.WaitAll(subs.Select(s => s.response).ToArray());
                foreach (var sub in subs){
                    var response = await sub.response;
                    if ((int)response.StatusCode != 200){
                        this.logger.LogWarning($"Bad request at {sub.URL} for email {sub.Email}. Will skip this time.");
                        continue;
                    }
                    if (!previousWebData.ContainsKey(sub.URL)){
                        previousWebData[sub.URL] = await response.Content.ReadAsStringAsync();
                        continue;
                    }
                    var newStr = await response.Content.ReadAsStringAsync();
                    sub.Changed = (previousWebData[sub.URL] != newStr);
                    previousWebData[sub.URL] = newStr;
                }
            }
            catch (Exception e){
                var errormsg = $"Exception In fetching web page. Service will stop.";
                errormsg += $"\n + {e}";
                this.logger.LogCritical(errormsg);
                await AlertAdminAsync(e);
                this.appLifetime.StopApplication();
            }
        }
        
        private async void AlertUsersOfChange(List<Subscription> subsToAlert)
        {
            try {
                foreach (var sub in subsToAlert){
                    //SMS
                    var smsClient = new AmazonSimpleNotificationServiceClient(this.configuration["AWSAccessKey"],
                                                this.configuration["AWSSecretKey"], RegionEndpoint.USEast1);
                    PublishRequest publishRequest = new PublishRequest();
                    publishRequest.Message = $"Skraper3: The website you asked me to watch changed. See: {sub.URL}";
                    publishRequest.PhoneNumber = sub.MobileNumber;
                    await smsClient.PublishAsync(publishRequest);
                    //Email
                    using (var client = new AmazonSimpleEmailServiceClient(this.configuration["AWSAccessKey"],
                                                this.configuration["AWSSecretKey"], RegionEndpoint.USEast1))
                    {
                        var sendRequest = new SendEmailRequest
                        {
                            Source = "angelusualle@gmail.com",
                            Destination = new Destination
                            {
                                ToAddresses =
                                new List<string> { sub.Email }
                            },
                            Message = new Message
                            {
                                Subject = new Content($"Skraper3: The website you asked me to watch changed. See: {sub.URL}"),
                                Body = new Body
                                {
                                    Html = new Content
                                    {
                                        Charset = "UTF-8",
                                        Data = $"Skraper3: The website you asked me to watch changed. See: {sub.URL}"
                                    },
                                    Text = new Content
                                    {
                                        Charset = "UTF-8",
                                        Data = $"Skraper3: The website you asked me to watch changed. See: {sub.URL}"
                                    }
                                }
                            }};
                            await client.SendEmailAsync(sendRequest);
                        }
                    }
            }
            catch (Exception e){
                    var errormsg = $"Exception In sending message. Service will continue.";
                    errormsg += $"\n + {e}";
                    this.logger.LogCritical(errormsg);
                    await this.AlertAdminAsync(e);
                    this.appLifetime.StopApplication();
            }
        }

        private async Task AlertAdminAsync(Exception e)
        {
            try {
                //SMS
                var smsClient = new AmazonSimpleNotificationServiceClient(this.configuration["AWSAccessKey"],
                                            this.configuration["AWSSecretKey"], RegionEndpoint.USEast1);
                PublishRequest publishRequest = new PublishRequest();
                publishRequest.Message = $"Skraper3: I stopped, heres what happened:{e}";
                publishRequest.PhoneNumber = this.configuration["AdminPhoneNumber"];
                await smsClient.PublishAsync(publishRequest);
            }
            catch{
                //nothing if cant cry for help

            }
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