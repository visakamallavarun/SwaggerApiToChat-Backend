# SwaggerApiToChat - API Intelligence Application

## Overview

The **SwaggerApiToChat Application** is a .NET 8-based web application that leverages Azure OpenAI services to provide intelligent API interaction capabilities. It enables users to interact with APIs using natural language, dynamically analyzing Swagger/OpenAPI specifications to simplify complex API operations.

---

## Features

- **Swagger-Based API Assistant**: Processes and interacts with APIs using their OpenAPI specifications.
- **Natural Language API Interaction**: Converts user requests into appropriate API calls.
- **Response Summarization**: Transforms raw JSON responses into human-readable summaries.
- **Action Generation**: Creates actionable prompts based on available API operations.
- **Context-Aware Conversations**: Maintains chat history for coherent multi-turn interactions.

---

## Technology Stack

- **.NET 8** application framework
- **Azure AI Inference Client** for OpenAI model integration
- **Azure OpenAI Service** for natural language processing
- **Entity Framework Core** for data persistence
- **Swagger/OpenAPI** for API documentation and analysis

---

## Prerequisites

To run the application, ensure the following are installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (with ASP.NET and web development workloads)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for Azure resource management)
- **Azure Subscription** (for deploying Azure services)
- **Pipedrive API Token** (if using Pipedrive integration)

---

## Configuration

The application requires the following configuration settings in either **User Secrets** (for local development) or **Environment Variables** (for production):

```json
{
  "AzureAI": {
    "ModelApiKey": "<your-azure-openai-key>",
    "ModelEndpointUrl": "<your-azure-openai-endpoint>",
    "ModelName": "<your-azure-openai-model-name>",
    "SpeechApiKey": "<your-azure-speech-key>",
    "Region": "<your-azure-region>"
  },
  "Pipedrive": {
    "ApiToken": "<your-pipedrive-api-token>"
  }
}
