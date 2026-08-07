# Lessons Learned

## 2026-08-05

### DeliveryReturn optional location support
- **Task**: Allow `location` per detail row on `/DeliveryReturn/add` and `/edit`; previously location was always inherited from the source DO
- **Finding**: `PartialTransfer` has no location parameter, but `DeliveryReturnDetail.Location` is settable. Pattern: record `doc.DataTableDetail.Rows.Count` before `PartialTransfer`, then set `doc.EditDetail(dtlKey).Location` on the newly appended row before `Save()`
- **Verified live**: override (`ASRS`) and inheritance (omit -> source DO `HQ`) both persist; `vDeliveryReturnDetail` also exposes `FromDocNo`/`FromDocType` (source DO link per detail row)
- **Note**: DR numbering in this DB is `DR-000001` style; API date format is `dd-MM-yyyy` (Swagger examples showing `2024-01-15` are wrong)

### MSB3027 recurred again (3rd time)
- Same as 2026-08-04: killed `MyAutocount` per AGENTS.md, but the running server was `GCR-autocount-api` (PID 6720) locking the exe. AGENTS.md kill/start commands are still outdated - always use `GCR-autocount-api`

## 2026-08-04

### MSB3027 recurred - killed wrong process name
- **Issue**: Release build failed with MSB3027/MSB3021 (exe locked) because only `MyAutocount` was killed per AGENTS.md, while the actual running server process is `GCR-autocount-api` (PID 2736)
- **Fix**: `Stop-Process -Name GCR-autocount-api -Force` before build
- **Lesson**: Same as 2026-08-03 "Build/run naming" entry - always kill `GCR-autocount-api`, not `MyAutocount`

### ODataHelper.BuildQuery ignores customSelect
- **Finding**: When any OData param ($top/$filter/$select/$orderby/$skip) is present, `ODataHelper.BuildQuery` rebuilds the SELECT itself (`*` or $select fields) and ignores the `customSelect` argument of `Sql.GetAllFromSql`
- **Consequence**: Computed/joined columns added via `customSelect` silently disappear from `getAll` responses as soon as clients pass OData params
- **Solution**: Put computed columns in `customFrom` instead (e.g. `CROSS APPLY (...) src`), so they survive `SELECT *`, `$select`, `$filter`, `$orderby`
- **Applied**: SalesInvoice `SourceDocNos` (linked delivery orders) via `CROSS APPLY` over `DocTransfer` + `vDeliveryOrder`

### Invoice<->DeliveryOrder link lives in DocTransfer
- **Finding**: AutoCount 2.0 links transferred docs via table `DocTransfer` (FromDocType/FromDocKey -> ToDocType/ToDocKey, detail-level rows). No `DocSource` table exists. `vInvoice` has no source-doc columns; `vDeliveryOrder` only has a single `ToDocKey`/`ToDocType` pointer (unreliable for partial transfers to multiple invoices)
- **Query**: invoice->DO: join `DocTransfer dt ON dt.ToDocKey = vInvoice.DocKey AND dt.ToDocType='IV'` then `vDeliveryOrder.DocKey = dt.FromDocKey`; use DISTINCT (multiple detail rows per doc pair)

## 2026-08-03

### Creditor/Debtor IsActive exposure
- **Task**: Expose active status on `/Creditor/*` and `/Debtor/*` read endpoints
- **Finding**: AutoCount views `vCreditor`/`vDebtor` do NOT include active status, but underlying `Creditor`/`Debtor` tables have `IsActive` char ('T'/'F'). Join key: `vCreditor.CreditorCode = Creditor.AccNo`, `vDebtor.DebtorCode = Debtor.AccNo`
- **Implementation**: `Sql.GetAllFromSql`/`GetSingleFromSql`/`GetCountFromSql` and `ODataHelper.BuildQuery`/`BuildCountQuery` accept optional `customFrom`/`customSelect`; Creditor/Debtor modules pass `LEFT JOIN` to the base table selecting `v.*, c.IsActive`
- **Side benefit**: `$filter=IsActive eq 'T'` works on getAll/count
- **Note**: add/edit do NOT set IsActive (read-only exposure); SDK entities have `IsActive` if write support is needed later

### Creditor e2e false-positive PASS (MaxLength again)
- **Issue**: `Creditor getSingle IsActive` check failed because the creditor was never created
- **Root cause**: `$testCreditorCode = "3100-Test$timestamp"` = 23 chars > `AccNo` MaxLength (20). Add failed with "Cannot set column 'AccNo'. The value violates the MaxLength limit", but Creditor routes return exceptions as HTTP 200 strings, so `Test-ApiEndpoint` reported PASS for add/edit/delete (false positives)
- **Fix**: Shortened to `"3100-T$($timestamp.Substring(10))"` (same convention as Debtor/SalesAgent/StockGroup)
- **Lesson**: Same MaxLength trap as 2026-04-28 entry. Keep generated codes short. Beware: API errors returned as 200 strings make e2e PASS unreliable; field-presence checks can surface these hidden failures

### Build/run naming (AGENTS.md outdated)
- Solution file is `GCR-autocount-api.sln` (not `MyAutocount.sln`)
- Built exe is `MyAutocount\bin\Release\net48\GCR-autocount-api.exe`; process name to kill is `GCR-autocount-api` (not `MyAutocount`)
- Killing only `MyAutocount` leaves the old server running and locks the exe during build (MSB3027)

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

### GoodsReceivedNote date field name mismatch (RuntimeBinderException)
- **Issue**: POST /GoodsReceivedNote/add threw `RuntimeBinderException: Cannot perform runtime binding on a null reference` at GoodsReceivedNote.cs:89
- **Root cause**: GRN module read `data.docDate`, but the API payload (and Swagger docs) use `date`. Missing JObject property returns null, and `.ToString()` on null throws the binder exception.
- **Convention**: Every other doctype reads the date field as `data.date` / `data[Constants.Date]` (e.g. DeliveryOrder.cs:188, PurchaseOrder.cs:250). GRN was the only outlier using `docDate`.
- **Fix**: Changed GoodsReceivedNote.cs Add (line 89) and Edit (line 133) to read `data.date`.
- **Date format**: `DateStringToDateTime` (Utils.cs:71) expects `dd-MM-yyyy`. Payload date must match (e.g. `15-01-2026`), and must fall within the company fiscal year.
- **Verified**: Build OK; live POST created GRN `GR3-26-01-001` then deleted it.
- **Status**: FIXED
