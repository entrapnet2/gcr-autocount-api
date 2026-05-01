param(
    [string]$ServerUrl = "http://localhost:8888",
    [string]$Username = "KENNY",
    [string]$Password = "1111",
    [switch]$SkipServerStart
)

$ErrorActionPreference = "Stop"
$jsonContentType = "application/json"

$testResults = @()
$passCount = 0
$failCount = 0

function Test-ApiEndpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$ExpectedType = "string"
    )
    
    $result = @{
        Name = $Name
        Method = $Method
        Endpoint = $Endpoint
        Status = "FAIL"
        Error = $null
    }
    
    try {
        $params = @{
            Uri = "$ServerUrl$Endpoint"
            Method = $Method
            Headers = $Headers
            ContentType = $jsonContentType
        }
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json -Depth 10
        }
        
        $response = Invoke-RestMethod @params
        
        $result.Status = "PASS"
        $result.Response = $response
        $script:passCount++
    }
    catch {
        $result.Error = $_.Exception.Message
        $script:failCount++
    }
    
    $script:testResults += $result
    return $result
}

function Get-AuthToken {
    $body = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$ServerUrl/login" -Method Post -Body $body -ContentType $jsonContentType
    return $response.token
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AutoCount E2E Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (-not $SkipServerStart) {
    Write-Host "`nStarting API server..." -ForegroundColor Yellow
    
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    if ([string]::IsNullOrEmpty($scriptDir)) { $scriptDir = $PWD.Path }
    $exePath = Join-Path $scriptDir "MyAutocount\bin\Release\net48\MyAutocount.exe"
    
    if (Test-Path $exePath) {
        if ([Environment]::OSVersion.Platform -eq "Unix" -or [Environment]::OSVersion.Platform -eq "MacOSX") {
            Write-Host "Running on non-Windows OS. Skipping Start-Process for .exe" -ForegroundColor Yellow
        } else {
            Start-Process $exePath -PassThru | Out-Null
            Start-Sleep -Seconds 3
        }
    } else {
        Write-Host "ERROR:exe not found at $exePath" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n1. Testing Login..." -ForegroundColor Yellow
$token = Get-AuthToken
$headers = @{"Authorization" = "Bearer $token"}
Write-Host "   Login OK, token received" -ForegroundColor Green

$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "ROOT DATA TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n2. Testing SalesAgent..." -ForegroundColor Yellow
$testAgentCode = "SA$($timestamp.Substring(8))"

$result = Test-ApiEndpoint -Name "GET SalesAgent/getAll" -Method Get -Endpoint "/SalesAgent/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /SalesAgent/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST SalesAgent/add" -Method Post -Endpoint "/SalesAgent/add" -Headers $headers -Body @{
    agentCode = $testAgentCode
    agentName = "Test Agent $timestamp"
}
Write-Host "   $($result.Status): POST /SalesAgent/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS") {
    $result = Test-ApiEndpoint -Name "GET SalesAgent/getSingle" -Method Get -Endpoint "/SalesAgent/getSingle/$testAgentCode" -Headers $headers
    Write-Host "   $($result.Status): GET /SalesAgent/getSingle/$testAgentCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "PUT SalesAgent/edit" -Method Put -Endpoint "/SalesAgent/edit" -Headers $headers -Body @{
        agentCode = $testAgentCode
        agentName = "Updated Agent $timestamp"
    }
    Write-Host "   $($result.Status): PUT /SalesAgent/edit" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE SalesAgent/delete" -Method Delete -Endpoint "/SalesAgent/delete/$testAgentCode" -Headers $headers
    Write-Host "   $($result.Status): DELETE /SalesAgent/delete/$testAgentCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n3. Testing StockGroup..." -ForegroundColor Yellow
$testGroupCode = "SG$($timestamp.Substring(8))"
$glCode = "5100-0001"

$result = Test-ApiEndpoint -Name "GET StockGroup/getAll" -Method Get -Endpoint "/StockGroup/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /StockGroup/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST StockGroup/add" -Method Post -Endpoint "/StockGroup/add" -Headers $headers -Body @{
    itemGroup = $testGroupCode
    description = "Test Group $timestamp"
    stockCodes = @{
        SalesCode = $glCode
        CashSalesCode = $glCode
        SalesReturnCode = $glCode
        SalesDiscountCode = $glCode
        PurchaseCode = $glCode
        CashPurchaseCode = $glCode
        PurchaseReturnCode = $glCode
        PurchaseDiscountCode = $glCode
        BalanceStockCode = $glCode
    }
}
Write-Host "   $($result.Status): POST /StockGroup/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS") {
    $result = Test-ApiEndpoint -Name "GET StockGroup/getSingle" -Method Get -Endpoint "/StockGroup/getSingle/$testGroupCode" -Headers $headers
    Write-Host "   $($result.Status): GET /StockGroup/getSingle/$testGroupCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "PUT StockGroup/edit" -Method Put -Endpoint "/StockGroup/edit" -Headers $headers -Body @{
        itemGroup = $testGroupCode
        description = "Updated Group $timestamp"
        stockCodes = @{
            SalesCode = $glCode
            CashSalesCode = $glCode
            SalesReturnCode = $glCode
            SalesDiscountCode = $glCode
            PurchaseCode = $glCode
            CashPurchaseCode = $glCode
            PurchaseReturnCode = $glCode
            PurchaseDiscountCode = $glCode
            BalanceStockCode = $glCode
        }
    }
    Write-Host "   $($result.Status): PUT /StockGroup/edit" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE StockGroup/delete" -Method Delete -Endpoint "/StockGroup/delete/$testGroupCode" -Headers $headers
    Write-Host "   $($result.Status): DELETE /StockGroup/delete/$testGroupCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n4. Testing StockItem..." -ForegroundColor Yellow
$testItemCode = "TI$timestamp"

$result = Test-ApiEndpoint -Name "GET StockItem/getAll" -Method Get -Endpoint "/StockItem/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /StockItem/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST StockItem/add" -Method Post -Endpoint "/StockItem/add" -Headers $headers -Body @{
    itemCode = $testItemCode
    description = "Test Item $timestamp"
    uom = "UNT"
    unitCost = "10.00"
    price = "20.00"
    costingMethod = "0"
    itemGroup = "01"
    leadTime = "0"
    dutyRate = "0"
}
Write-Host "   $($result.Status): POST /StockItem/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS") {
    $result = Test-ApiEndpoint -Name "GET StockItem/getSingle" -Method Get -Endpoint "/StockItem/getSingle/$testItemCode" -Headers $headers
    Write-Host "   $($result.Status): GET /StockItem/getSingle/$testItemCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "PUT StockItem/edit" -Method Put -Endpoint "/StockItem/edit" -Headers $headers -Body @{
        itemCode = $testItemCode
        description = "Updated Item $timestamp"
        uom = "UNT"
        unitCost = "15.00"
        price = "25.00"
        costingMethod = "0"
        itemGroup = "01"
        leadTime = "0"
        dutyRate = "0"
    }
    Write-Host "   $($result.Status): PUT /StockItem/edit" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE StockItem/delete" -Method Delete -Endpoint "/StockItem/delete/$testItemCode" -Headers $headers
    Write-Host "   $($result.Status): DELETE /StockItem/delete/$testItemCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n5. Testing Debtor..." -ForegroundColor Yellow
$testDebtorCode = "2200-T$($timestamp.Substring(10))"

$result = Test-ApiEndpoint -Name "GET Debtor/getAll" -Method Get -Endpoint "/Debtor/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /Debtor/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST Debtor/add" -Method Post -Endpoint "/Debtor/add" -Headers $headers -Body @{
    debtorCode = $testDebtorCode
    companyName = "Test Company $timestamp"
    billingAddress1 = "Test Address 1"
    billingAddress2 = "Test Address 2"
    billingAddress3 = "Test City"
    billingAddress4 = "12345"
    deliveryAddress1 = "Delivery Address 1"
    deliveryAddress2 = "Delivery Address 2"
    deliveryAddress3 = "Delivery City"
    deliveryAddress4 = "12345"
    phone = "0123456789"
    mobile = "0123456789"
    fax = "0123456789"
    emailAddress = "test@test.com"
    attention = "Test Person"
    businessNature = "Trading"
    creditTerm = "C.O.D"
    statementType = "O"
    agingOn = "I"
    creditLimit = 10000
    overdueLimit = 1000
}
Write-Host "   $($result.Status): POST /Debtor/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS") {
    $result = Test-ApiEndpoint -Name "GET Debtor/getSingle" -Method Get -Endpoint "/Debtor/getSingle/$testDebtorCode" -Headers $headers
    Write-Host "   $($result.Status): GET /Debtor/getSingle/$testDebtorCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "PUT Debtor/edit" -Method Put -Endpoint "/Debtor/edit" -Headers $headers -Body @{
        debtorCode = $testDebtorCode
        companyName = "Updated Company $timestamp"
        billingAddress1 = "Updated Address 1"
        billingAddress2 = "Updated Address 2"
        billingAddress3 = "Updated City"
        billingAddress4 = "54321"
        deliveryAddress1 = "Updated Delivery 1"
        deliveryAddress2 = "Updated Delivery 2"
        deliveryAddress3 = "Updated Delivery City"
        deliveryAddress4 = "54321"
        phone = "9876543210"
        mobile = "9876543210"
        fax = "9876543210"
        emailAddress = "updated@test.com"
        attention = "Updated Person"
        businessNature = "Services"
        creditTerm = "C.O.D"
        statementType = "O"
        agingOn = "I"
        creditLimit = 20000
        overdueLimit = 2000
    }
    Write-Host "   $($result.Status): PUT /Debtor/edit" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE Debtor/delete" -Method Delete -Endpoint "/Debtor/delete/$testDebtorCode" -Headers $headers
    Write-Host "   $($result.Status): DELETE /Debtor/delete/$testDebtorCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n6. Testing Creditor..." -ForegroundColor Yellow
$testCreditorCode = "3100-Test$timestamp"

$result = Test-ApiEndpoint -Name "GET Creditor/getAll" -Method Get -Endpoint "/Creditor/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /Creditor/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST Creditor/add" -Method Post -Endpoint "/Creditor/add" -Headers $headers -Body @{
    creditorCode = $testCreditorCode
    companyName = "Test Supplier $timestamp"
    billingAddress1 = "Test Address 1"
    billingAddress2 = "Test Address 2"
    billingAddress3 = "Test City"
    billingAddress4 = "12345"
    phone = "0123456789"
    mobile = "0123456789"
    fax = "0123456789"
    emailAddress = "supplier@test.com"
    attention = "Test Person"
    businessNature = "Trading"
    creditTerm = "C.O.D"
    statementType = "O"
    agingOn = "I"
    creditLimit = 10000
    overdueLimit = 1000
}
Write-Host "   $($result.Status): POST /Creditor/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS") {
    $result = Test-ApiEndpoint -Name "GET Creditor/getSingle" -Method Get -Endpoint "/Creditor/getSingle/$testCreditorCode" -Headers $headers
    Write-Host "   $($result.Status): GET /Creditor/getSingle/$testCreditorCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "PUT Creditor/edit" -Method Put -Endpoint "/Creditor/edit" -Headers $headers -Body @{
        creditorCode = $testCreditorCode
        companyName = "Updated Supplier $timestamp"
        billingAddress1 = "Updated Address 1"
        billingAddress2 = "Updated Address 2"
        billingAddress3 = "Updated City"
        billingAddress4 = "54321"
        phone = "9876543210"
        mobile = "9876543210"
        fax = "9876543210"
        emailAddress = "updated@supplier.com"
        attention = "Updated Person"
        businessNature = "Services"
        creditTerm = "Net 60"
        statementType = "O"
        agingOn = "I"
        creditLimit = 20000
        overdueLimit = 2000
    }
    Write-Host "   $($result.Status): PUT /Creditor/edit" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE Creditor/delete" -Method Delete -Endpoint "/Creditor/delete/$testCreditorCode" -Headers $headers
    Write-Host "   $($result.Status): DELETE /Creditor/delete/$testCreditorCode" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TRANSACTION TESTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n7. Testing SalesInvoice..." -ForegroundColor Yellow
$testInvDocNo = "INV-$timestamp"

$result = Test-ApiEndpoint -Name "GET SalesInvoice/getAll" -Method Get -Endpoint "/SalesInvoice/getAll" -Headers $headers
Write-Host "   $($result.Status): GET /SalesInvoice/getAll" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

$result = Test-ApiEndpoint -Name "POST SalesInvoice/add (simple)" -Method Post -Endpoint "/SalesInvoice/add" -Headers $headers -Body @{
    debtorCode = "2200-J001"
    date = (Get-Date).ToString("dd-MM-yyyy")
    shipInfo = "Test Ship Info"
    detailList = @(
        @{
            itemCode = "01Z01Z01001BLU"
            uom = "UNT"
            quantity = 10
            unitPrice = 10.00
            discount = "0"
        }
    )
}
Write-Host "   $($result.Status): POST /SalesInvoice/add" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})

if ($result.Status -eq "PASS" -and $result.Response) {
    $savedDocNo = $result.Response -replace ".*added: ", ""
    
    $result = Test-ApiEndpoint -Name "GET SalesInvoice/getSingle" -Method Get -Endpoint "/SalesInvoice/getSingle/$savedDocNo" -Headers $headers
    Write-Host "   $($result.Status): GET /SalesInvoice/getSingle/$savedDocNo" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "GET SalesInvoice/getDetail" -Method Get -Endpoint "/SalesInvoice/getDetail/$savedDocNo" -Headers $headers
    Write-Host "   $($result.Status): GET /SalesInvoice/getDetail/$savedDocNo" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
    
    $result = Test-ApiEndpoint -Name "DELETE SalesInvoice/delete" -Method Delete -Endpoint "/SalesInvoice/delete/$savedDocNo" -Headers $headers
    Write-Host "   $($result.Status): DELETE /SalesInvoice/delete/$savedDocNo" -ForegroundColor $(if($result.Status -eq "PASS"){"Green"}else{"Red"})
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nTotal Tests: $($passCount + $failCount)" -ForegroundColor White
Write-Host "Passed: $passCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor Red

if ($failCount -eq 0) {
    Write-Host "`nAll tests PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nSome tests FAILED!" -ForegroundColor Red
    Write-Host "`nFailed tests:" -ForegroundColor Yellow
    foreach ($r in $testResults) {
        if ($r.Status -eq "FAIL") {
            Write-Host "  - $($r.Method) $($r.Endpoint): $($r.Error)" -ForegroundColor Red
        }
    }
    exit 1
}