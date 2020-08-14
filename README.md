# GitHub Webhook with SmartThings Integration

A C# Azure Function for a GitHub webhook with SmartThings integration

Invoke your home automation through GitHub based on the build status of a repository.  Limitless home automation capability provided throught SmartThings integration.  For example, start the house alarm, open windows, turn off the tv when the build breaks.  Dim the lights and play soothing music when the build succeeds.

As a starting point to configuring this Azure Function and GitHub settings, see the related blog post: https://jamied.me/posts/2020/07/github-webhooks-with-azure-functions/

## SmartThings NET
All SmartThings integration is via my [SmartThings NET](https://www.nuget.org/packages/SmartThingsNet/) nuget package.  Which provides the ability to execute a `Scene` within your SmartThings ecoystem. This component requires a SmartThings OAuth token to run. 

### SmartThings Personal Access Token

To generate a SmartThings PAT token go to the following link: https://account.smartthings.com/tokens, ensuring that the token has permission to see and control scenes.

### SmartThings Scenes

This Azure Function requires two different SmartThings Scenes to differentiate, when the build succeeds and fails.  You can use existing scenes, or create new ones.  Each scene can do whatever you want to automate or control within your SmartThings environment. Make a note of the scene names as you'll need them to configure the Azure Function.

### Azure Function configuration

In additional to the guidance provided in the [blog post](https://jamied.me/posts/2020/07/github-webhooks-with-azure-functions/) you'll need to set the following Environment variables eg:

    "SmartThingsAccessToken" : "{your-smartthings-pat}"
    "SuccessSceneName": "Build Success",
    "FailureSceneName": "Build Failure"

## Testing

You can test the Azure Function locally using Postman to post a json payload to your local endpoint, simulating what GitHub will send on a build completion:

```json
{
  "action": "completed",
  "check_suite": {
    "id": 941142538,
    "head_branch": "master",
    "status": "completed",
    "conclusion": "failure",
    "app": {
      "id": 15368,
      "slug": "github-actions",
      "name": "GitHub Actions",
      "description": "Automate your workflow from idea to production",
      "external_url": "https://help.github.com/en/actions",
      "html_url": "https://github.com/apps/github-actions"
    }
  }
}
```