# GCR-autocount-api

A REST API server for AutoCount accounting software, providing CRUD operations through HTTP endpoints.

## Why This Project Exists

Anyone who has tried to work with AutoCount's official documentation knows the pain - it's a maze of scattered information that's difficult to navigate. The original documentation by AutoCount is messy and overwhelming.

This project aims to simplify the integration process by providing a clean, ready-to-use REST API server that anyone struggling with AutoCount integration can use. Hope this helps fellow developers who face the same challenges!

## About Global Connect Resources Sdn Bhd

We are a software house specializing in business automation and custom software solutions. If you need customization work, system integration, or bespoke software development tailored to your business needs, we're here to help!

**Services we offer:**
- Custom software development
- AutoCount customization & integration
- Business process automation
- System integration services
- ERP solutions & consulting

📧 Contact us at: [globalconnect.com.my](https://globalconnect.com.my)

## Prerequisites

- Windows OS
- .NET Framework 4.8
- AutoCount Accounting software installed (version 2.2.26 tested)
- Visual Studio 2019/2022 or MSBuild

## Tested Compatibility

| Component | Version |
|-----------|---------|
| AutoCount SDK | 2.2.26 |
| AutoCount Database | 2.2.26 |
| DevExpress | 22.2 |
| .NET Framework | 4.8 |

## Planned Features

### NATS.IO with JWT Authentication

We plan to add NATS.IO messaging support with JWT-based authentication for:
- **Real-time event publishing** - Get notified when invoices are created, debtors updated, etc.
- **Secure inter-service communication** - JWT-based auth using NKeys (Ed25519)
- **Command & Control** - Remote operation execution via NATS subscriptions

See the full implementation plan at [docs/nats-jwt-plan.md](docs/nats-jwt-plan.md).

**Benefits:**
- Lightweight alternative to webhooks for real-time updates
- Decentralized authentication using NATS JWT hierarchy (Operator → Account → User)
- High-performance pub/sub messaging for enterprise integrations

---

## How to Power Up

### 1. Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/GCR-autocount-api.git
cd GCR-autocount-api
```

### 2. Restore NuGet Packages

Open the solution in Visual Studio or run:

```bash
nuget restore GCR-autocount-api.sln
```

### 3. Build the Project

Using Visual Studio:
- Open `GCR-autocount-api.sln`
- Build → Build Solution (Ctrl+Shift+B)

Using MSBuild:
```bash
msbuild GCR-autocount-api.sln /p:Configuration=Release
```

### 4. Run as Console Application (Development)

Navigate to the output directory and run:

```bash
cd MyAutocount\bin\Release
GCR-autocount-api.exe
```

### 5. Install as Windows Service (Production)

Run PowerShell as Administrator:

```powershell
# Navigate to the release folder
cd MyAutocount\bin\Release

# Install the service with delayed start
.\GCR-autocount-api.exe install --delayed start
```

### 6. Verify the Service

- Press `Win + R`, type `services.msc`
- Look for "GCR AutoCount API Server"
- Ensure startup type is "Automatic (Delayed Start)"
- Service status should be "Running"

## API Endpoints

Once running, the REST API will be available at:
- **API Base URL:** `http://localhost:8888`
- **Swagger UI:** `http://localhost:8888/swagger`

The API provides CRUD operations for:
- Sales operations
- Purchase operations
- Stock management
- AR/AP operations
- Invoicing
- And more...

### Authentication

1. POST `/login` with username/password to get JWT token
2. Click 'Authorize' button in Swagger UI
3. Enter token as: `Bearer <your-token>`
4. Now you can test all endpoints

## License

This project is open-sourced under the MIT License. See the LICENSE file for details.

## Contributing

Contributions are welcome! Feel free to submit issues and pull requests to improve this project.

## Credits

This project was originally cloned from [msf4-0/MyAutocount](https://github.com/msf4-0/MyAutocount) and has been made open source to benefit the community.

**Developer:** Kenny Koay (entrapnet)  
**Company:** Global Connect Resources Sdn Bhd (globalconnect.com.my)
