# Customer Support — Angular 21 (Feature-first architecture)

A standalone Angular 21 front-end for the existing **CustomerSupport.API** (.NET 10) project.
This is the same full Customer/Support Executive/Administrator application, reorganized for long-term maintenance.

## Prerequisites

- Windows 10/11; VS Code or Visual Studio 2026 (Open Folder)
- Node.js version compatible with Angular 21 (for example Node.js 22.12+ or 24+)
- An accessible SQL Server-backed CustomerSupport.API, serving HTTPS, with CORS enabled for http://localhost:4200

## Run

1. Extract the archive and open `CustomerSupport.Angular` (the folder containing `package.json`).
2. Open `src/app/core/config/api.config.ts`, update `API_BASE_URL` for your Web API (one setting only).
3. Run `npm install` (important: the ZIP intentionally does **not** contain node_modules).
4. Run `npm start` or `npx ng serve --open`.
5. Browse `http://localhost:4200` and sign in using a backend account.

To verify locally: `npm run build` (requires Angular packages downloaded by npm).

> `@angular/build:dev-server` missing? Run `npm install --include=dev` in the folder containing this file. Don't launch `ng serve` before dependencies install successfully.

## Where code lives

```text
src/app/
├── app.component.ts / app.component.html       # shell and navigation
├── app.routes.ts / app.config.ts                # lazy routes and DI setup
├── core/
│   ├── config/api.config.ts                     # ONLY change API base URL here
│   ├── http/models/                             # ApiResponse<T>
│   ├── http/services/api-client.service.ts     # HTTP transport / error envelope
│   └── auth/
│       ├── models/auth.model.ts                 # login/register/session models
│       ├── services/auth.service.ts             # authentication state and token refresh
│       ├── guards/auth.guard.ts                 # permission checks
│       └── interceptors/auth.interceptor.ts     # Bearer token + retry logic
├── shared/
│   ├── components/                              # reusable notice/heading/empty-state
│   ├── models/                                  # pagination / search
│   └── utils/                                   # error and label utilities
└── features/
    ├── auth/pages/                             # login + customer registration
    ├── account/pages/                          # current account
    ├── dashboard/pages/                        # role-specific dashboard
    ├── tickets/models,services,pages/          # ticket workflows
    ├── products/models,services/               # product API
    ├── ticket-categories/models,services/      # category API
    ├── master-data/models,services/            # priorities, statuses
    ├── knowledge-base/models,services/         # articles
    ├── faqs/models,services/                   # FAQs
    ├── catalog/pages/                          # reusable product/category admin screens
    ├── content/pages/                          # reusable article/FAQ screens
    └── errors/pages/                           # 403 / 404 / 500
```

Every page component has its **own `.component.ts` and `.component.html`** file.
No custom CSS file or custom CSS class is used. Layouts use Bootstrap 5.3.8 from CDN, Bootstrap utility classes, and Bootstrap Icons from CDN.
All routes are lazy loaded using `loadComponent`; Angular's `@if` / `@for` template control flow is used.

## Separation of responsibilities

- **Components**: UI state, forms, navigation and user interactions; no direct HttpClient requests.
- **Feature services**: endpoint paths, typed request/response contracts, and business-specific APIs.
- **Core HttpClient wrapper**: consistent API response envelope and download handling.
- **Models**: domain-specific response and request interfaces.
- **Auth service and guards**: centralized session state and page permissions; backend remains the security authority.
- **Reusable shared components**: headings, notices and empty states.

## Existing backend limitations

- There is no API for listing Support Executives, so the ticket assignment screen accepts a valid user ID.
- No API for editing the authenticated user's profile or password; Account page is read-only.
- Dashboards use existing ticket-list APIs, not a nonexistent dashboard/analytics endpoint.

## Security and deployment notes

This training implementation keeps tokens in **sessionStorage** to remain compatible with the existing
JSON access/refresh-token API; JavaScript-accessible storage is vulnerable to XSS and is not the preferred
production refresh-token architecture. For production, consider server-supported HttpOnly Secure cookie-based
refresh-token handling, a restrictive Content Security Policy, and a backend CORS allow-list.
Only the Web API determines real user permissions. The UI's guards are not a security boundary.

This ZIP contains source files but not `node_modules`, browser builds, or secrets.

## Updated professional interface

The October 2026 design revision provides a responsive dark workspace sidebar, mobile offcanvas navigation, account dropdown, cohesive page headings, dashboard summaries, modern forms, filter panels, readable data tables, prominent status badges, polished empty/error states, and a unified presentation of ticket conversations, history, and file attachments.

The page bindings and existing Web API operations remain unchanged. Two visually distinct layouts are used: a full-width authentication layout for public login/registration, and a navigation workspace for authenticated customers, support executives, and administrators.

Bootstrap CSS and Icons require internet access in the browser because they are loaded from CDNs. No custom stylesheets, Sass, Tailwind, Angular Material, or third-party component libraries are installed.
