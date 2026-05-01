# NATS.IO with JWT Support - Implementation Plan

## Overview
Add NATS.IO messaging support with JWT authentication to MyAutocount, providing an alternative/pub-sub transport layer for real-time updates and inter-service communication.

## Prerequisites
- NATS Server (nats-server) installed or accessible
- Understanding of NATS JWT authentication flow (Operator → Account → User hierarchy)

## Architecture

### NATS JWT Authentication Model
```
Operator (trusted by server)
    └── Account (issued by operator)
            └── User (issued by account) ← We use this
```

### Integration Points
1. **Publish events** when AutoCount operations occur (invoice created, debtor updated, etc.)
2. **Subscribe to commands** for remote operation execution
3. **JWT-based auth** for secure client connections

---

## Implementation Steps

### Phase 1: Infrastructure Setup

#### 1.1 Install NATS Packages
```bash
dotnet add MyAutocount/MyAutocount.csproj package NATS.Net
```

#### 1.2 Create NATS Configuration Class
File: `NATS/NatsConfig.cs`
```csharp
public class NatsConfig
{
    public string Url { get; set; } = "nats://localhost:4222";
    public string CredsFile { get; set; } = "";
    public string UserJwt { get; set; } = "";
    public string UserSeed { get; set; } = "";
    public bool Enabled { get; set; } = false;
}
```

#### 1.3 Create NATS Client Service
File: `NATS/NatsService.cs`
```csharp
public class NatsService : IDisposable
{
    private NatsClient _client;
    private NatsConfig _config;
    
    public async Task Connect(NatsConfig config);
    public async Task Publish(string subject, string data);
    public async Task Subscribe(string subject, Action<string> handler);
    public void Dispose();
}
```

---

### Phase 2: JWT Authentication

#### 2.1 Option A: Using Credentials File
Pre-generate using `nsc` tool:
```bash
nsc add operator --name GCR
nsc add account --name myaccount
nsc add user --name myautocount
nsc generate creds --account myaccount --user myautocount
```

Then use in code:
```csharp
var opts = NatsOpts.Default;
opts.Url = "nats://localhost:4222";
opts.Authentication = NatsAuthOpts.FromCredsFile("/path/to/user.creds");
var client = new NatsClient(opts);
```

#### 2.2 Option B: Using JWT and Seed Directly
```csharp
var opts = NatsOpts.Default;
opts.Url = "nats://localhost:4222";
opts.Authentication = NatsAuthOpts.FromJwtAndSeed(userJwt, userSeed);
var client = new NatsClient(opts);
```

#### 2.3 Option C: Dynamic JWT Generation (Advanced)
For on-the-fly user JWT creation (requires `NATS.Jwt` package):
```csharp
// Note: NATS.Jwt package is experimental
var userJwt = GenerateUserJwt(accountSigningKey, userPublicKey);
```

---

### Phase 3: Event Publishing

#### 3.1 Define Event Subjects (NATS naming convention)
```
autocount.<entity>.<action>.<docType>
Examples:
- autocount.sales.invoice.created
- autocount.debtor.updated
- autocount.stock.item.added
```

#### 3.2 Publish Events After Operations
In `SalesInvoice.cs` Add method:
```csharp
private void PublishInvoiceCreated(Invoice doc)
{
    if (_natsService.Enabled)
    {
        var eventData = JsonConvert.SerializeObject(new
        {
            docNo = doc.DocNo,
            debtorCode = doc.DebtorCode,
            amount = doc.Total,
            timestamp = DateTime.UtcNow
        });
        _natsService.Publish("autocount.sales.invoice.created", eventData);
    }
}
```

Apply similar pattern to:
- Debtor (add/edit/delete)
- Creditor (add/edit/delete)  
- StockItem (add/edit/delete)
- All other entities

---

### Phase 4: Command Subscription (Optional - Future)

#### 4.1 Subscribe to Remote Commands
```csharp
_natsService.Subscribe("autocount.command.salesinvoice.add", (data) =>
{
    // Parse command
    // Execute Add operation
    // Publish response
});
```

---

### Phase 5: Configuration

#### 5.1 Update App.config
```xml
<appSettings>
    <add key="NatsEnabled" value="false"/>
    <add key="NatsUrl" value="nats://localhost:4222"/>
    <add key="NatsCredsFile" value=""/>
    <add key="NatsUserJwt" value=""/>
    <add key="NatsUserSeed" value=""/>
</appSettings>
```

#### 5.2 Update MyService.cs to Initialize NATS
```csharp
public class MyService : ServiceControl
{
    private NatsService _natsService;
    
    public bool Start(HostControl hostControl)
    {
        // ... existing code ...
        
        // Initialize NATS
        var natsConfig = LoadNatsConfig();
        if (natsConfig.Enabled)
        {
            _natsService = new NatsService();
            _natsService.Connect(natsConfig);
            Log("NATS connected");
        }
    }
}
```

---

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `NATS/NatsConfig.cs` | Create | Configuration class |
| `NATS/NatsService.cs` | Create | NATS client wrapper |
| `NATS/NatsEvents.cs` | Create | Event definitions and helpers |
| `MyAutocount.csproj` | Modify | Add NATS.Net package |
| `MyService.cs` | Modify | Initialize NATS on startup |
| `App.config` | Modify | Add NATS settings |
| All Doctypes/*.cs | Modify | Add event publishing |

---

## Testing Plan

### 6.1 Unit Tests
- Test NATS connection with JWT auth
- Test event publishing
- Test connection failure handling

### 6.2 Integration Tests
```bash
# Start NATS server with JWT auth
nats-server -c nats-jwt.conf

# Run MyAutocount
# Execute operations via REST API
# Verify events received via NATS subscriber
```

### 6.3 Manual Testing
```bash
# Subscribe to all autocount events
nats sub "autocount.>"

# Then trigger operations via REST API and observe events
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| NATS.Net | 2.x | Main NATS client |
| NATS.Jwt | (optional) | JWT generation (experimental) |

---

## Security Considerations

1. **Protect seed files** - NKey seeds are sensitive, store securely
2. **Use TLS** - Configure `nats://` with TLS for production
3. **JWT expiration** - Set appropriate expiration on user JWTs
4. **Least privilege** - Only grant necessary permissions in JWT claims

---

## References

- [NATS .NET Client](https://github.com/nats-io/nats.net)
- [NATS JWT Documentation](https://docs.nats.io/running-a-nats-service/configuration/securing_nats/auth_intro/jwt)
- [nsc Tool](https://github.com/nats-io/nsc) - For managing JWT credentials
- [NATS.Jwt .NET](https://github.com/nats-io/jwt.net) - JWT library (experimental)

---

## Next Steps

1. Install NATS server locally for testing
2. Generate JWT credentials using `nsc`
3. Implement Phase 1 & 2 (infrastructure + auth)
4. Test basic connectivity
5. Implement Phase 3 (event publishing)
6. Update documentation
