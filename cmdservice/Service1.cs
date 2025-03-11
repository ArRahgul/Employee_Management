using cmdservice.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace cmdservice
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer _timer;
        public static readonly HttpClient httpClient = new HttpClient();
        private static IConfiguration Configuration;
        private static string BaseUrl;

        private static string SmtpServer;
        private static int SmtpPort;
        private static string SenderEmail;
        private static string SenderPassword;
        private static string[] RecipientEmails;

        public Service1()
        {
            try
            {
                InitializeComponent();

                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();

                //Email Settings
                SmtpServer = Configuration["EmailSettings:SmtpServer"];
                SmtpPort = int.Parse(Configuration["EmailSettings:SmtpPort"]);
                SenderEmail = Configuration["EmailSettings:SenderEmail"];
                SenderPassword = Configuration["EmailSettings:SenderPassword"];
                RecipientEmails = Configuration["EmailSettings:RecipientEmails"].Split(',');

                // Use "Staging" or "Live" as per the requirement
                bool isLive = true;
                string environment = isLive ? "Live" : "Staging";
                //string environment = "Staging";
                BaseUrl = Configuration[$"DeviceApi:{environment}:BaseUrl"];
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in the constructor: {ex.Message}");
            }
        }

        protected override async void OnStart(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
                LogStatus("Service started.");
                //SendEmail("Service Started", "The service started.");
                var (ipAddress, macAddress) = await SendStatusToApi("start", "username");
                if (ipAddress != null && macAddress != null)
                {
                    await SendStartStopRequest(ipAddress, macAddress, "start");
                }
                CheckAndLogActiveState();
                SetTimer();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in OnStart: {ex.Message}");
            }
        }

        protected override async void OnStop()
        {
            try
            {
                LogStatus("Service stopped.");
                var (ipAddress, macAddress) = await SendStatusToApi("stop", "username");
                if (ipAddress != null && macAddress != null)
                {
                    await SendStartStopRequest(ipAddress, macAddress, "stop");
                }

                _timer?.Stop();
                _timer?.Dispose();  
                //SendEmail("Service Stopped", "The service has been stopped.");
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in OnStop: {ex.Message}");
            }
        }

        protected override async void OnShutdown()
        {
            try
            {
                LogStatus("Service shutting down.");
                var (ipAddress, macAddress) = await SendStatusToApi("shutdown", "username");
                if (ipAddress != null && macAddress != null)
                {
                    await SendStartStopRequest(ipAddress, macAddress, "shutdown");
                }

                StopService();
                //SendEmail("Service Shutting Down", "The service is shutting down.");
                base.OnShutdown();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in OnShutdown: {ex.Message}");
            }
        }

        public async void Start()
        {
            try
            {
                LogStatus("Service started.");
                //SendEmail("Service started", "The service started.");
                var (ipAddress, macAddress) = await SendStatusToApi("start", "username");
                if (ipAddress != null && macAddress != null)
                {
                    await SendStartStopRequest(ipAddress, macAddress, "start");
                }
                CheckAndLogActiveState();
                SetTimer();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in Start: {ex.Message}");
            }
        }

        public void Stopp()
        {
            try
            {
                LogStatus("Service stopped.");
                _timer?.Stop();
                _timer?.Dispose();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in Stopp: {ex.Message}");
            }
        }

        private void SetTimer()
        {
            try
            {
                var pollInterval = GetPoll();
                _timer = new System.Timers.Timer(pollInterval);
                _timer.Elapsed += OnTimedEvent;
                _timer.AutoReset = true;
                _timer.Enabled = true;
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in SetTimer: {ex.Message}");
            }
        }

        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                await CheckAndLogActiveState();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in OnTimedEvent: {ex.Message}");
            }
        }

        private async Task CheckAndLogActiveState()
        {
            try
            {
                var (isLoggedOn, username) = IsUserLoggedOn();
                string status = isLoggedOn ? "Active" : "Inactive";
                LogStatus($"User: {username}, Status: {status}");
                await SendStatusToApi(status, username);
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in CheckAndLogActiveState: {ex.Message}");
                await SendStatusToApi("Error", "Unknown");
            }
        }

        private (bool isLoggedOn, string username) IsUserLoggedOn()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "C:\\Windows\\sysnative\\query.exe",
                    Arguments = "user",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string output = reader.ReadToEnd();
                        LogStatus(output);
                        string username = ParseUsername(output);
                        bool isLoggedOn = ParseOutput(output);
                        return (isLoggedOn, username);
                    }
                }
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in IsUserLoggedOn: {ex.Message}");
                return (false, string.Empty);
            }
        }

        private bool ParseOutput(string output)
        {
            try
            {
                return output.Contains("Active");
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in ParseOutput: {ex.Message}");
                return false;
            }
        }

        private void LogStatus(string message)
        {
            string logFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\MachineStatusLog.txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("MachineStatusService", $"Failed to write log: {ex.Message}", EventLogEntryType.Error);
            }
        }

        public int GetPoll()
        {
            try
            {
                var client = new RestClient();
                var request = new RestRequest(BaseUrl + "Settings", Method.Get);
                request.AddHeader("accept", "text/plain");
                RestResponse response = client.Execute(request);

                if (!response.IsSuccessful)
                {
                    throw new Exception("Failed to retrieve settings.");
                }

                List<PollModel.Root> myDeserializedClass = JsonConvert.DeserializeObject<List<PollModel.Root>>(response.Content);
                var pollValueString = myDeserializedClass?.FirstOrDefault(c => c.name == "Poll")?.value;

                return int.TryParse(pollValueString, out int pollValue) ? pollValue * 60000 : 600000; // Default to 10 minutes if parsing fails
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in GetPoll: {ex.Message}");
                return 300000; // Default to 5 minutes
            }
        }

        public async Task<(string ipAddress, string macAddress)> SendStatusToApi(string status, string username)
        {
            string ipAddress = null;
            string macAddress = null;

            try
            {
                ipAddress = IpAddress.GetLocalIPAddress();
                List<string> macAddresses = MacAddress.GetMacAddresses();
                macAddress = await CheckMacAddress(macAddresses);
                string mac = macAddress.Replace(":", "-");
                int pollInterval = GetPoll();
                int minutes = pollInterval / 60000;

                var client = new RestClient();
                var request = new RestRequest(BaseUrl + "Device", Method.Post);
                request.AddHeader("accept", "text/plain");
                request.AddHeader("Content-Type", "application/json");

                var body = $@"{{
                                ""id"": 0,
                                ""macAddress"": ""{mac}"",
                                ""ipAddress"": ""{ipAddress}"",
                                ""name"": ""{username}"",
                                ""created"": ""{DateTime.UtcNow:O}"",
                                ""updated"": ""{DateTime.UtcNow:O}"",
                                ""status"": ""{status}"",
                                ""minutes"": {minutes}
                           }}";

                request.AddStringBody(body, DataFormat.Json);
                try
                {
                    var response = await client.ExecuteAsync(request);
                    if (response.IsSuccessful)
                    {
                        LogStatus($"Status sent to API successfully. Data: {body}. Response: {response.Content}");
                    }
                    else
                    {
                        LogStatus($"Failed to send status to API. Data: {body}. Response: {response.Content}");
                    }
                }
                catch (Exception ex)
                {
                    LogStatus($"Failed to send status to API. Data: {body}. Exception: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in SendStatusToApi: {ex.Message}");
            }
            return (ipAddress, macAddress);
        }

        private string ParseUsername(string output)
        {
            try
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 3 && parts[3] == "Active")
                    {
                        string username = parts[0];
                        LogStatus($"Parsed username: {username}");
                        return username;
                    }
                }
                return "No active user found.";
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in ParseUsername: {ex.Message}");
                return "No active user found.";
            }
        }

        private void StopService()
        {
            try
            {
                _timer?.Stop();
                _timer?.Dispose();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in StopService: {ex.Message}");
            }
        }

        public static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                Service1 serviceInstance = new Service1();
                serviceInstance.LogStatus($"Unhandled exception: {ex.Message}");
                EventLog.WriteEntry("MachineStatusService", $"Unhandled exception: {ex.Message}", EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("MachineStatusService", $"An error occurred in UnhandledExceptionHandler: {ex.Message}", EventLogEntryType.Error);
            }
        }

        public async Task<string> CheckMacAddress(List<string> macAddresses)
        {
            try
            {
                var checkMacApiUrl = Configuration["DeviceApi:CheckMacApi:Url"];
                var authorizationToken = await GetHrmsAuthToken();

                var client = new RestClient();
                var request = new RestRequest(checkMacApiUrl, Method.Get);
                request.AddHeader("accept", "*/*");
                request.AddHeader("Authorization", $"Bearer {authorizationToken}");

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    throw new Exception("Failed to retrieve MAC address details.");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response.Content);

                foreach (var macAddress in macAddresses)
                {
                    string mac = macAddress.Replace(":", "-");
                    var macAddressDetail = apiResponse.Data?.FirstOrDefault(c => c.MacAddress == mac);
                    if (macAddressDetail != null)
                    {
                        return macAddressDetail.MacAddress;
                    }
                }

                return macAddresses.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred while checking MAC address: {ex.Message}");
                return macAddresses.FirstOrDefault();
            }
        }

        public async Task<string> GetHrmsAuthToken()
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://hrms-api.dsrc.in/Authentication");
                request.Headers.Add("accept", "*/*");
                var content = new StringContent("{\r\n \"UserName\": \"aravind.a@dsrc.co.in\",\r\n \"Password\": \"D5rc$002\",\r\n \"Version\": \"\",\r\n \"Platform\": \"\"\r\n}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var hrmsApiResponse = await response.Content.ReadAsStringAsync();

                var tokenObj = JsonConvert.DeserializeObject<HrmsApiResponse>(hrmsApiResponse);
                if (tokenObj != null && tokenObj.Data != null && !string.IsNullOrEmpty(tokenObj.Data.Token))
                {
                    return tokenObj.Data.Token;
                }
                else
                {
                    throw new Exception("Failed to retrieve token from HRMS API.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred while fetching the HRMS auth token: {ex.Message}");
                throw;
            }
        }

        public class MachineDetail
        {
            public string IPAddress { get; set; }
            public string MacAddress { get; set; }
        }

        public class ApiResponse
        {
            public List<MachineDetail> Data { get; set; }
        }

        public class HrmsApiResponse
        {
            public Data Data { get; set; }
        }

        public class Data
        {
            public string Token { get; set; }
        }


        private void SendEmail(string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(SmtpServer, SmtpPort))
                {
                    client.Credentials = new System.Net.NetworkCredential(SenderEmail, SenderPassword);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(SenderEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };

                    foreach (var recipient in RecipientEmails)
                    {
                        mailMessage.To.Add(recipient.Trim());
                    }

                    client.Send(mailMessage);
                    LogStatus("Email sent successfully.");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred while sending email: {ex.Message}");
            }
        }


        public async Task SendStartStopRequest(string ip, string mac, string startStop)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://devopscommonapi.dsrc.in/api/StartStop");
                request.Headers.Add("accept", "*/*");

                var body = $@"{{
                        ""id"": 0,
                        ""ip"": ""{ip}"",
                        ""mac"": ""{mac}"",
                        ""startStop1"": ""{startStop}"",
                        ""date"": ""{DateTime.UtcNow:O}""
                      }}";

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Content = content;

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);

                LogStatus($"Start/Stop request successful. Response: {responseContent}");
            }
            catch (Exception ex)
            {
                LogStatus($"An error occurred in SendStartStopRequest: {ex.Message}");
            }
        }



    }
}
