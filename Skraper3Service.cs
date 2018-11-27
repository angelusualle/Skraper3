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
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

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

        private Dictionary<string, string> previousWebData;

        private SubscriptionsContext context; 

        public Skraper3Service(  
            IConfiguration configuration,  
            IHostingEnvironment environment,  
            ILogger<Skraper3Service> logger,   
            IApplicationLifetime appLifetime,
            SubscriptionsContext context)  
        {  
            this.configuration = configuration;  
            this.logger = logger;  
            this.appLifetime = appLifetime;  
            this.environment = environment;  
            this.client = new HttpClient();
            this.previousWebData = new Dictionary<string, string>();
            this.context = context;
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
            List<Subscriptions> subs = null;
            //Get all subscriptions
            try {
                subs = await context.Subscriptions.ToListAsync();
            }
            catch (Exception e) {
                var errormsg = $"Exception In getting subscriptions from Database. Service will stop.";
                errormsg += $"\n + {e}";
                this.logger.LogCritical(errormsg);
                await AlertAdminAsync(e.ToString());
                this.appLifetime.StopApplication();
            }

            //Loop through detect changes
            await DetectChangesInSubscriptions(subs);

            //Alert users of changes
            AlertUsersOfChange(subs.Where(s => s.Changed == 1).ToList());

            //Set next iteration.
            this.timer = new Timer(DoWork, null, this.frequencyInMilliseconds, -1);


        }
        private async Task DetectChangesInSubscriptions(List<Subscriptions> subs)
        {
            try {
                foreach (var sub in subs)
                {
                    HttpClient client = new HttpClient();
                    try {
                        var response = await client.GetAsync(sub.Url);
                        if ((int)response.StatusCode != 200){
                            this.logger.LogWarning($"Bad request at {sub.Url} for email {sub.Email}. Will skip this time.");
                            continue;
                        }
                        if (!previousWebData.ContainsKey(sub.Url)){
                            var baseStr = await response.Content.ReadAsStringAsync();
                            var webdata = getWebdataFromSub(baseStr, sub);
                            previousWebData[sub.Url] = webdata;
                            sub.Changed = 0;
                            continue;
                        }
                        var newWebResponse = await response.Content.ReadAsStringAsync();
                        var newStr = getWebdataFromSub(newWebResponse, sub);
                        sub.Changed = (previousWebData[sub.Url] != newStr) ? 1:0;
                        previousWebData[sub.Url] = newStr;
                        sub.NumberOfErrors = 0;
                    }
                    catch (Exception e){
                        ++sub.NumberOfErrors;
                        if (sub.NumberOfErrors > 5){
                            RemoveSubscription(sub);
                            await AlertUserOfCancel(sub);
                        }
                        sub.Changed = 0;
                    }
                }
            }
            catch (Exception e){
                var errormsg = $"Exception In processing. Service will stop.";
                errormsg += $"\n + {e}";
                this.logger.LogCritical(errormsg);
                await AlertAdminAsync(e.ToString());
                this.appLifetime.StopApplication();
            }
            finally{
                await context.SaveChangesAsync();
            }
        }

        private string getWebdataFromSub(string baseStr, Subscriptions sub)
        {
            var webdata = baseStr;
            if (sub?.Xpath != ""){
                var dom = new HtmlDocument();
                dom.LoadHtml(baseStr);
                webdata = dom.DocumentNode.SelectSingleNode(sub.Xpath).InnerText;
            }
            return webdata;
        }

        private void RemoveSubscription(Subscriptions sub)
        {
            context.Subscriptions.Remove(sub);
        }

        private async void AlertUsersOfChange(List<Subscriptions> subsToAlert)
        {
            try {
                foreach (var sub in subsToAlert){
                    var changes = previousWebData[sub.Url];
                    await SendEmailAndText($"Skraper3: The website you asked me to watch changed. To:{changes}  \nOriginal Site:{sub.Url}", sub);
                }
            }
            catch (Exception e){
                    var errormsg = $"Exception In sending message. Service will continue.";
                    errormsg += $"\n + {e}";
                    this.logger.LogCritical(errormsg);
                    await this.AlertAdminAsync(e.ToString());
                    this.appLifetime.StopApplication();
            }
        }

        private async Task AlertUserOfCancel(Subscriptions sub){
            await SendEmailAndText($"Skraper3: The website you asked me to look at errored out too many times. URL: {sub.Url}", sub);
        }

        private async Task SendEmailAndText(string message, Subscriptions sub){
            if (sub.MobileNumber != null){
                //SMS
                var smsClient = new AmazonSimpleNotificationServiceClient(this.configuration["AWSAccessKey"],
                                            this.configuration["AWSSecretKey"], RegionEndpoint.USEast1);
                PublishRequest publishRequest = new PublishRequest();
                publishRequest.Message = message;
                publishRequest.PhoneNumber = sub.MobileNumber;
                await smsClient.PublishAsync(publishRequest);
            }
            //Email
            using (var client = new AmazonSimpleEmailServiceClient(this.configuration["AWSAccessKey"],
                                        this.configuration["AWSSecretKey"], RegionEndpoint.USEast1))
            {
                var sendRequest = new SendEmailRequest
                {
                    Source = "skraper3@outlook.com",
                    Destination = new Destination
                    {
                        ToAddresses =
                        new List<string> { sub.Email }
                    },
                    Message = new Message
                    {
                        Subject = new Content("Skraper3"),
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Charset = "UTF-8",
                                Data = message
                            },
                            Text = new Content
                            {
                                Charset = "UTF-8",
                                Data = message
                            }
                        }
                }};
                await client.SendEmailAsync(sendRequest);
            }
        }

        private async Task AlertAdminAsync(string errmsg)
        {
            try {
                //SMS
                var smsClient = new AmazonSimpleNotificationServiceClient(this.configuration["AWSAccessKey"],
                                            this.configuration["AWSSecretKey"], RegionEndpoint.USEast1);
                PublishRequest publishRequest = new PublishRequest();
                publishRequest.Message = $"Skraper3: I stopped, heres what happened: {errmsg}";
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
        }  
  
  
        public Task StopAsync(CancellationToken cancellationToken)  
        {  
            this.logger.LogInformation("StopAsync method called.");  
            return Task.CompletedTask;  
        }  
    }  
}  