# Enterprise IIS & ASP.NET Core EdTech Platform Architecture Guide

## 1. System Overview & Technology Stack
- **Framework**: ASP.NET Core 10 Web API
- **Database / ORM**: Entity Framework Core 9 (Sqlite / SqlServer provider with DbContext Pooling)
- **Logging & Auditing**: Serilog (Console & File Sinks + Database Audit Store)
- **Hosting & Deployment**: Internet Information Services (IIS) 10+ with `AspNetCoreModuleV2`
- **Frontend Portal**: Glassmorphic SPA (HTML5 / Vanilla JS / CSS Design Tokens)

---

## 2. Solution Structure
```
edupulse-edtech-platform/
├── deploy-to-iis.ps1            # Automated PowerShell Deployment Script for IIS
├── IIS_DEPLOYMENT_GUIDE.md      # Architecture & Operations Guide
└── EduPulse.Api/                # ASP.NET Core API Project
    ├── Controllers/             # RESTful API Controllers
    │   ├── CoursesController.cs
    │   ├── EnrollmentsController.cs
    │   ├── AssessmentsController.cs
    │   ├── AnalyticsController.cs
    │   └── AuditLogsController.cs
    ├── Data/                    # EF Core DbContext & Seed Script
    │   ├── EduPulseDbContext.cs
    │   └── DbInitializer.cs
    ├── Middleware/              # Serilog Audit Logging Middleware
    │   └── SerilogRequestLoggingMiddleware.cs
    ├── Models/                  # Domain Entities & DTOs
    │   ├── Domain/Entities.cs
    │   └── DTOs/Dtos.cs
    ├── Services/                # Business Logic Services
    │   ├── CourseService.cs
    │   ├── EnrollmentService.cs
    │   ├── AssessmentService.cs
    │   ├── AnalyticsService.cs
    │   └── AuditService.cs
    ├── wwwroot/                 # Embedded Glassmorphic Web UI
    │   ├── index.html
    │   ├── css/styles.css
    │   └── js/app.js
    ├── Program.cs               # Application Host Builder & DI
    └── web.config               # IIS AspNetCoreModuleV2 Handler Configuration
```

---

## 3. IIS Deployment & Production Setup

### Prerequisites on IIS Server:
1. **Windows Server 2019 / 2022** or **Windows 10 / 11** with IIS enabled.
2. **ASP.NET Core Hosting Bundle 10.x** installed on the IIS host machine ([Download Link](https://dotnet.microsoft.com/download/dotnet)).

### Deployment Option A: Automated PowerShell Deployment
Run PowerShell as Administrator in the project directory:
```powershell
.\deploy-to-iis.ps1 -SiteName "EduPulseEdTech" -Port 8088 -PublishDirectory "C:\inetpub\wwwroot\EduPulseApi"
```

### Deployment Option B: Manual IIS Setup
1. Publish the release payload:
   ```bash
   dotnet publish -c Release -o C:\inetpub\wwwroot\EduPulseApi
   ```
2. Open **IIS Manager** (`inetpub.msc`).
3. Add a new **Application Pool**:
   - Name: `EduPulseAppPool`
   - .NET CLR Version: **No Managed Code** (Required for ASP.NET Core Out-of-Process / In-Process model).
4. Add a new **Web Site**:
   - Site Name: `EduPulseEdTech`
   - Application Pool: `EduPulseAppPool`
   - Physical Path: `C:\inetpub\wwwroot\EduPulseApi`
   - Binding Port: `8088`
5. Grant permissions to Application Pool Identity (`IIS AppPool\EduPulseAppPool`) on `C:\inetpub\wwwroot\EduPulseApi`.

---

## 4. Serilog Audit Logging Configuration
Serilog logs are stored dynamically under `logs/edupulse-audit-yyyyMMdd.log` as well as streamed live to the `/api/v1/auditlogs` endpoint and UI tab.

Sample Structured Log Entry:
```json
{
  "Timestamp": "2026-09-25T11:30:00Z",
  "Level": "Information",
  "Category": "Certification",
  "Message": "Student 'Marcus Vance' completed course 'Enterprise ASP.NET Core & EF Core Microservices Architecture'. Certificate issued: EDUPULSE-CERT-89214",
  "ActionBy": "Marcus Vance",
  "HttpMethod": "POST",
  "Path": "/api/v1/enrollments/complete-lesson",
  "StatusCode": 200,
  "ElapsedMs": 42.0
}
```
