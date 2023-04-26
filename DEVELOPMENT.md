# How to run the application in developer machine

[Back to the main page](/README.md)

These client application was written with .NET 7 [Blazor WebAssembly](https://docs.microsoft.com/aspnet/core/blazor/?view=aspnetcore-6.0#blazor-webassembly), .NET 7 C# [Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview), and a C# class library with shared code.

> Note: Azure Functions only supports .NET 7 in the isolated process execution model

## Pre-requisites

- GigHub account which you can sign up for one free here: <https://github.com/signup>.
- Azure account which you can sign up for a free account here: <https://azure.microsoft.com/en-us/free>. Some of the services in the demo are free (with limits of course) but your new account will give you $200 credit to try out the services that are not free for the first 30 from account creation. That’s more than enough to experiment on all services in the demo.

## Getting Started

1. In the **Api** folders, copy `local.settings.example.json` to `local.settings.json`

1. Continue using either Visual Studio or Visual Studio Code.

### Visual Studio 2022

Open the solution in the latest release of [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the Azure workload installed, and follow these steps:

1. Right-click on the solution and select **Configure Startup Projects...**.

1. Select **Multiple startup projects** and set the following actions for each project:

    a. For Option 1:

        - *API* - None
        - *Shared* - None
        - *SWA1API* - **Start**
        - *SWA1Client* - **Start**
        - *SWA2API* - None
        - *SWA2Client* - None

    b. For Option 2:

        - *API* - **Start**
        - *Shared* - None
        - *SWA1API* - None
        - *SWA1Client* - None
        - *SWA2API* - **Start**
        - *SWA2Client* - **Start**

1. Press **F5** to launch both the client application and the Functions API apps.

### Visual Studio Code with Azure Static Web Apps CLI for a better development experience (Optional)

1. Install the [Azure Static Web Apps CLI](https://www.npmjs.com/package/@azure/static-web-apps-cli) and [Azure Functions Core Tools CLI](https://www.npmjs.com/package/azure-functions-core-tools).

1. Open the folder in Visual Studio Code.

1. In the VS Code terminal, run the following command to start the Static Web Apps CLI, along with the Blazor WebAssembly client application and the Functions API app:

    ```bash
    swa start http://localhost:5000 --api-location http://localhost:7071
    ```

    The Static Web Apps CLI (`swa`) starts a proxy on port 4280 that will forward static site requests to the Blazor server on port 5000 and requests to the `/api` endpoint to the Functions server. 

1. Open a browser and navigate to the Static Web Apps CLI's address at `http://localhost:4280`. You'll be able to access both the client application and the Functions API app in this single address. When you navigate to the "Fetch Data" page, you'll see the data returned by the Functions API app.

1. Enter Ctrl-C to stop the Static Web Apps CLI.
