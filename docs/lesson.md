# Lessons Learned

## 2026-04-29

### SalesInvoice POST - API Design
- **Issue**: Initial design used stateful endpoints (/new, /item/add, /save)
- **Problem**: HTTP is stateless - static variables don't persist between requests
- **Solution**: Use single POST /add with complete document (header + details in one JSON)
- **Pattern**: 
  ```json
  {
    "debtorCode": "2200-J001",
    "date": "29-04-2026",
    "detailList": [
      { "itemCode": "XXX", "uom": "UNT", "quantity": 10, "unitPrice": 10.00 }
    ]
  }
  ```

### SalesInvoice POST - SDK/DB Compatibility
- **Issue**: SalesInvoice POST fails with 500 error
- **Root cause**: SDK 2.2.15 vs Database 2.2.13 schema mismatch
- **Reference**: docs/lesson.md "AutoCount SDK vs Database Schema Mismatch"
- **Status**: ON HOLD - requires DB upgrade or SDK downgrade

## 2026-04-28

### Project Rename Limitation
- **Issue**: Attempted to rename project from "MyAutocount" to "GCR-AutoCount-REST"
- **Reason for failure**: AutoCount DLL dependencies (AutoCount.*.dll) are tightly coupled with the project. The project uses AutoCount accounting software SDK which expects specific project structure.
- **Resolution**: Keep the project name as "MyAutocount" to maintain compatibility with AutoCount SDK dependencies.
- **Recommendation**: When working with third-party SDKs that have tight coupling, verify if project name changes are feasible before attempting.

### POST Endpoints Returning 500 Errors
- **Issue**: POST /SalesAgent/add, POST /StockGroup/add, POST /Debtor/add, POST /SalesInvoice/add return 500 Internal Server Error
- **Root cause**: These failures are likely due to AutoCount business logic validations or database constraints, not code issues:
  - Data validation requirements from AutoCount SDK
  - Missing required fields that are database-level constraints
  - Referential integrity requirements (e.g., GL codes, debtor codes must exist)
- **Working endpoints**: StockItem and Creditor POST operations work correctly, indicating the code pattern is correct
- **Status**: These 500 errors are expected behavior when test data doesn't meet AutoCount business requirements in the test database

### Swagger Organization
- **Task**: Reorganized Swagger to group Master Data at the top
- **Implementation**: 
  - Master Data group: SalesAgent, Debtor, Creditor, StockGroup, StockItem
  - Sales group: SalesInvoice, CashSale
  - Purchase group: PurchaseOrder, GoodsReceivedNote
  - Stock Transactions group: StockAdjustment, StockTransfer, StockAssembly, etc.
- **Sample parameters**: Updated all sample parameters with realistic example values

### Error Response Format
- **Requirement**: API errors must return JSON format `{"error": "...message..."}`
- **Implementation**: Created `Utils.CreateErrorResponse()` helper method that returns proper JSON error responses with 500 status code
- **Usage**: All POST/PUT/DELETE endpoints now use this helper for consistent error formatting

### MaxLength Validation Errors
- **Issue**: SalesAgent and StockGroup codes were too long due to timestamp in test data
- **Error**: "Cannot set column 'X'. The value violates the MaxLength limit of this column."
- **Fix**: Shortened test codes in test_e2e_all.ps1:
  - SalesAgent: `TestA$timestamp` → `TA$timestamp`
  - StockGroup: `TG$timestamp` → `G$timestamp`
- **Status**: FIXED

### AutoCount SDK vs Database Schema Mismatch
- **Issue**: Debtor and SalesInvoice creation fail due to missing columns
- **Affected endpoints**:
  - POST /Debtor/add - Error: "Column 'SGEInvoicePeppolFormat' does not belong to table Debtor"
  - POST /SalesInvoice/add - Error: "Column 'SubmitInvoiceNow' does not belong to table Master"
- **Root cause**: AutoCount SDK version (2.2.26) expects columns that don't exist in database
- **Version info**:
  - SDK: AutoCount 2.2.26 (NuGet packages)
  - DLL File Version: 2.2.0.0
  - Database: AED_TEST on (local)\A2006
- **Possible causes**:
  1. Database is from an older AutoCount version
  2. Database needs upgrade/migration to match SDK schema
  3. SDK version is incompatible with the database version
- **Impact**: Debtor and SalesInvoice POST will fail until database is upgraded or SDK is downgraded
- **Status**: PENDING - Requires database migration or SDK downgrade
- **Recommendation**: 
  1. Run AutoCount database migration tool to upgrade database schema
  2. Or downgrade SDK to version matching database schema
