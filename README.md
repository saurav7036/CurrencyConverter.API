# 💱 Currency Converter API

A .NET 8-based RESTful API that provides real-time and historical currency conversion capabilities. It follows a clean, modular architecture with support for dependency injection, caching, JWT token-based authentication (RBAC), retry policies, and circuit breakers.

---

## 1️⃣ Setup Instructions

1. **Clone the Repository**
   ```bash
   git clone https://github.com/saurav7036/CurrencyConverter.API.git
   cd CurrencyConverter.API
   ```

2. **Build and Run**
   ```bash
   dotnet build
   dotnet run --project CurrencyConverter.API
   ```

3. **Access Swagger UI**
   - Default URL: `https://localhost:7225/swagger`
   - Use the `/api/v1/dev/token` endpoint to generate a temporary JWT token for testing purposes

4. **Token Endpoint Example Payload**
   You can use the following sample payload to generate a JWT token via the `/api/v1/dev/token` endpoint:

   ```json
   {
     "username": "test-user",
     "permissions": {
       "ExchangeRate.ViewLatest": true,
       "ExchangeRate.ConvertAmount": false,
       "ExchangeRate.ViewHistory": true
     },
     "expirationInSeconds": 100
   }
   ```

   **Note:** The **local token provider is only for testing** purposes and should not be used in production.

---

## 2️⃣ Assumptions Made

- ✅ **Frankfurter API is Free:**  
  The external exchange rate provider (Frankfurter) is assumed to be **free and does not require an API key**.

- ✅ **Token Endpoint is for Testing Only:**  
  A dedicated endpoint (`/api/v1/dev/token`) is used to generate JWT tokens **only for local development**, as no external identity provider is currently integrated.

- ✅ **Clean Dependency Injection Setup:**  
  The architecture uses DI to ensure testability and maintainability. Each responsibility (service, provider, cache) is cleanly separated.

- ✅ **Retry and Circuit Breaker Logic is Config-Driven:**  
  Implemented using **Polly**, and all settings are read from `appsettings.json`. This allows for easy overrides and environment-specific behaviors.

- ✅ **Rate Limiter in API Layer:**  
  A fixed rate limiter is implemented within the API. In the future, this should be migrated to an API Gateway layer for better scalability and control.

- ✅ **Deployment & Environment Setup:**  
  Dockerfiles for both **Test** and **Production** environments are provided:
  - `Dockerfile.Test`
  - `Dockerfile.Production`

  Each environment has its own configuration file:
  - `appsettings.Development.json`
  - `appsettings.Test.json`
  - `appsettings.Production.json`

- ✅ **CI/CD Integration with GitHub Actions:**  
  GitHub Actions workflow (`ci.yml`) is included to:
  - Restore dependencies
  - Build the project
  - Run tests and generate test coverage
  - Enforce a **coverage threshold gate**
  - Build Docker images for Test and Production environments

- ✅ **Behavioral Testing for End-to-End Flow:**  
  Added **behavioral tests** that closely mimic integration tests by mocking only **external API calls**, while using **real implementations** of internal services. These tests are:
  - Fast and reliable
  - Cover **happy paths and negative scenarios**
  - Help validate the **complete behavior of API endpoints end-to-end**

- ✅ **Unit and Integration Tests:**  
  The project includes comprehensive **unit tests** for isolated components and **integration tests** for full-stack validation using real data flows and dependency setups.

---

## 3️⃣ Possible Future Enhancements

- 🔄 **Redis / Distributed Caching:**  
  Replace in-memory cache with **Redis** or other distributed cache solutions to support scalable and stateless deployments.

- 🔍 **Test Coverage for Resilience Logic:**  
  Extend tests to explicitly validate **retry behavior and circuit breaker transitions**.

- 🔐 **Replace Local Token Provider:**  
  Integrate with a **real identity provider** such as **Auth0**, **Azure AD**, or **AWS Cognito** to issue and validate tokens securely.

- ⚙️ **Pluggable Configuration Source:**  
  Migrate configuration (e.g., Polly settings) to **external config providers** like **AWS AppConfig** or **Azure App Configuration** for runtime updates.

- 🔧 **Multiple Exchange Rate Providers Support:**  
  The system is designed to support **multiple external exchange rate providers** by implementing the `IExchangeRateProvider` interface. Additional providers can be added with minimal configuration.

- 🚦 **Rate Limiter Relocation to API Gateway:**  
  While a rate limiter is currently implemented within the API, in future it should be moved to an **API Gateway layer** for centralized throttling and protection.

- 📈 **Replica Scaling via Deployment YAMLs:**  
  Extend deployment configurations to support **min/max replica scaling** using Kubernetes or other orchestrator-specific YAML definitions.

- 📁 **Enhanced Logging & Exception Handling:**  
  Middleware is added for global exception handling. Logs are written locally using **Serilog**, and can be extended to use cloud-based or file-based sinks such as Seq, ELK, or AWS CloudWatch.