# CustomerSupport.Mvc — ASP.NET Core MVC Front-End (.NET 10)

This is the **ASP.NET Core MVC front-end** for the existing **CustomerSupport.API** Web API. It is an independent Visual Studio project, with Razor Views and Bootstrap styling designed to follow the same customer, support-executive, and administrator experience as the companion Angular front-end.

## Prerequisites

- Windows and Visual Studio 2026 with the **ASP.NET and web development** workload.
- .NET 10 SDK installed.
- The existing CustomerSupport.API and its SQL Server database configured and running.
- Internet access to load Bootstrap and Bootstrap Icons from their CDNs.

## Open and run in Visual Studio

1. Extract the ZIP file.
2. Open **CustomerSupport.Mvc.sln** in Visual Studio 2026.
3. Start the existing **CustomerSupport.API** solution using its **HTTPS** launch profile (default `https://localhost:7158`).
4. Open `appsettings.json` in this MVC project and check the Web API URL.
5. Set **CustomerSupport.Mvc** as Startup Project and press **F5**.
6. Navigate to `https://localhost:7265` (the project launches the page by default).

If necessary, trust development HTTPS certificates from a Windows terminal:

```powershell
dotnet dev-certs https --trust
```

You can also run from the project directory:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile CustomerSupport.Mvc
```

## Change the Web API URL in one location

Open **CustomerSupport.Mvc/appsettings.json**:

```json
"WebApi": {
  "BaseUrl": "https://localhost:7158/"
}
```

This is the **only project configuration setting** for the Web API base URL. The `ServiceCollectionExtensions` configures a named `HttpClient` using this setting; Controllers and API Services do not hardcode the host or port. The MVC application's own ports live in `Properties/launchSettings.json` and are independent of the API URL.

## Structure and responsibilities

```text
CustomerSupport.Mvc/
├── Controllers/                  MVC controllers (one class per file)
├── Extensions/
│   └── ServiceCollectionExtensions.cs  Dependency-injection registrations
├── Interfaces/
│   └── Services/                One interface per file
├── Models/
│   ├── Api/                      Typed backend request/response contracts
│   └── ViewModels/               View and form models / DataAnnotations
├── Services/                     One concrete API service per file
├── Views/
│   ├── Auth/, Account/, Dashboard/, Tickets/
│   ├── Products/, TicketCategories/, KnowledgeBase/, Faqs/
│   ├── Home/
│   └── Shared/                   Bootstrap layout, navigation, ticket table, alerts
├── Properties/launchSettings.json
├── Program.cs                    Explicit Program class and static Main()
├── appsettings.json              Single Web API URL configuration
└── CustomerSupport.Mvc.sln
```

All C# source files use **block-scoped namespaces**. Controller and Service dependencies use explicit constructor injection and `private readonly` fields. There are **no top-level statements** and no service implementations defined in interface files.

### Technical approach

- Server-rendered Razor Views use Bootstrap 5.3.8 and Bootstrap Icons from CDNs. There are no custom CSS stylesheets and no Angular scripts.
- `Controllers` receive strongly typed API service interfaces from DI, not `HttpClient` directly.
- `Services` implement their interfaces and use the centralized `IApiClient` for backend requests.
- `ApiClient` adds JWT Bearer credentials server-side and handles refresh token renewal, file upload, and file download.
- MVC uses an HttpOnly Secure authentication cookie, server-side session storage for API tokens, and automatic antiforgery validation for form submissions.
- Models and ViewModels reside in separate files so DTOs, validation, and UI-specific state are not mixed with Controllers.

### Application pages

- Customer login/registration and account profile.
- Customer, Support Executive and Administrator dashboards.
- Customer ticket creation, personal ticket list, and ticket details.
- Support and administrator ticket search, filtering, sorting, pagination, assignment, status workflow, comments, attachments, and history.
- Administrator Products and Ticket Categories create/edit/list pages.
- Knowledge Base and FAQ browsing/details, with administrator authoring and management.
- Access Denied (403), Not Found (404), and Server Error (500) pages.

> **Existing API limitation:** The backend does not expose a Support Executive lookup/listing endpoint, so ticket assignment accepts a known `SupportExecutiveId` rather than an invented dropdown. Similarly, the account profile is read-only because the existing API has no profile-editing or password-change endpoint.

## Security and deployment

This is a development/teaching front-end. For a production deployment, use a distributed cache for session data (e.g. Redis), share/persist ASP.NET Core Data Protection keys, implement token/session concurrency handling, configure secrets and reverse proxies correctly, and add integration, accessibility, and security tests. Always use HTTPS: the sample authentication and session cookies are marked Secure.

## Code style and verification

All MVC controller and service methods use block bodies with opening and closing curly braces on separate lines. A blank line separates adjacent methods. Visual Studio reads the project `.editorconfig` for brace placement and indentation preferences.

Build and validate locally in Visual Studio 2026, or from PowerShell:

```powershell
dotnet restore
dotnet build
```

Test the application end-to-end with the existing CustomerSupport.API running.
