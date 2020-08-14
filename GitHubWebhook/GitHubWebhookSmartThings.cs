 Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubWebhook
{
    public static class GitHubWebhookSmartThings
    {
        [FunctionName("GitHubWebhookSmartThings")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# Webhook for GitHub with SmartThings.");
            string requestBody = null;

            if (!Debugger.IsAttached)
            {
                if (!req.Headers.ContainsKey("X-Hub-Signature"))
                {
                    log.LogError("Missing signature");
                    return new BadRequestObjectResult("Missing signature");
                }

                string signature = req.Headers["X-Hub-Signature"].FirstOrDefault();
                requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string secret = Environment.GetEnvironmentVariable("Secret");
                if (!CheckSignature.Validate(signature, requestBody, secret))
                {
                    log.LogError($"Signatures don't match");
                    return new BadRequestObjectResult("Signatures don't match");
                }
                log.LogInformation($"Signature check success");
            }

            requestBody ??= await new StreamReader(req.Body).ReadToEndAsync();
            CheckSuiteStatus suiteStatus = JsonConvert.DeserializeObject<CheckSuiteStatus>(requestBody);

            string responseMessage = $"The GitHub Check Suite status for {suiteStatus.check_suite.id} {suiteStatus.action} with {suiteStatus.check_suite.conclusion}";
            await RunSmartThingsScene(suiteStatus, log);

            log.LogInformation(responseMessage);
            return new OkObjectResult(responseMessage);
        }

        private static async Task RunSmartThingsScene(CheckSuiteStatus suiteStatus, ILogger log)
        {
            SmartThingsNet.Client.Configuration config = new SmartThingsNet.Client.Configuration();
            config.AccessToken = Environment.GetEnvironmentVariable("SmartThingsAccessToken", EnvironmentVariableTarget.Process);
            SmartThingsNet.Api.ScenesApi scenesApi = new SmartThingsNet.Api.ScenesApi(config);

            string sceneName = null;
            switch (suiteStatus.check_suite.conclusion.ToLower())
            {
                case "success":
                    sceneName = Environment.GetEnvironmentVariable("SuccessSceneName", EnvironmentVariableTarget.Process);
                    break;
                case "failure":
                    sceneName = Environment.GetEnvironmentVariable("FailureSceneName", EnvironmentVariableTarget.Process);
                    break;
                default:
                    // ignore cancelled, neutral, timed_out, action_required or stale builds
                    break;
            }

            if (sceneName != null)
            {
                try
                {
                    log.LogInformation($"Retrieving scenes");
                    var allScenes = await scenesApi.ListScenesAsync();
                    string sceneId = allScenes.Items?.Where(s => s.SceneName == sceneName).FirstOrDefault().SceneId;
                    log.LogInformation($"Found SceneId: {sceneId}");
                    var result = await scenesApi.ExecuteSceneAsync(sceneId);
                    log.LogInformation($"Execute success scene: {result.Status}");
                }
                catch (Exception exp)
                {
                    log.LogError($"Error executing scene: {exp.Message}");
                }
            }
        }
    }
}
