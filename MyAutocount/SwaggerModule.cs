using Nancy;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace GCR_autocount_api
{
    public class SwaggerModule : NancyModule
    {
        private static byte[] swaggerCssBytes = null;
        private static byte[] swaggerBundleBytes = null;
        private static byte[] swaggerPresetBytes = null;

        public SwaggerModule()
        {
            Get("/swagger", _ => GetSwaggerUI());
            Get("/swagger/spec", _ => GetSwaggerSpec());
            Get("/swagger-ui.css", _ => GetSwaggerFile("css"));
            Get("/swagger-ui-bundle.js", _ => GetSwaggerFile("bundle"));
            Get("/swagger-ui-standalone-preset.js", _ => GetSwaggerFile("preset"));
        }

        private static object downloadLock = new object();

        public static void PreloadSwaggerFiles()
        {
            lock (downloadLock)
            {
                if (swaggerCssBytes != null) return;
                
                try
                {
                    var wc = new System.Net.WebClient();
                    swaggerCssBytes = wc.DownloadData("https://unpkg.com/swagger-ui-dist@4.10.3/swagger-ui.css");
                    swaggerBundleBytes = wc.DownloadData("https://unpkg.com/swagger-ui-dist@4.10.3/swagger-ui-bundle.js");
                    swaggerPresetBytes = wc.DownloadData("https://unpkg.com/swagger-ui-dist@4.10.3/swagger-ui-standalone-preset.js");
                    wc.Dispose();
                    Utils.Log("Swagger UI files downloaded successfully");
                }
                catch (System.Exception ex)
                {
                    Utils.Log("Failed to download Swagger files: " + ex.Message);
                }
            }
        }

        private Response GetSwaggerFile(string type)
        {
            byte[] data = null;
            string contentType = "";
            
            switch (type)
            {
                case "css":
                    data = swaggerCssBytes;
                    contentType = "text/css";
                    break;
                case "bundle":
                    data = swaggerBundleBytes;
                    contentType = "application/javascript";
                    break;
                case "preset":
                    data = swaggerPresetBytes;
                    contentType = "application/javascript";
                    break;
            }
            
            if (data == null)
                return Response.AsText("Not loaded", "text/plain");
            
            var response = (Response)"";
            response.ContentType = contentType;
            response.StatusCode = HttpStatusCode.OK;
            response.Headers["Content-Length"] = data.Length.ToString();
            response.Contents = stream =>
            {
                stream.Write(data, 0, data.Length);
            };
            return response;
        }

        private Response GetSwaggerUI()
        {
            var html = @"<!DOCTYPE html>
<html>
<head>
    <title>GCR AutoCount Sync - Swagger UI</title>
    <link rel=""stylesheet"" type=""text/css"" href=""http://localhost:8888/swagger-ui.css"">
    <style>
        body { margin: 0; padding: 0; }
        .swagger-ui .topbar { display: none; }
        .info { margin: 30px 0 !important; }
    </style>
</head>
<body>
    <div id=""swagger-ui""></div>
    <script src=""http://localhost:8888/swagger-ui-bundle.js""></script>
    <script src=""http://localhost:8888/swagger-ui-standalone-preset.js""></script>
    <script>
        window.onload = function() {
            window.ui = SwaggerUIBundle({
                url: 'http://localhost:8888/swagger/spec',
                dom_id: '#swagger-ui',
                deepLinking: true,
                presets: [
                    SwaggerUIBundle.presets.apis,
                    SwaggerUIStandalonePreset
                ],
                layout: 'StandaloneLayout'
            });
        };
    </script>
</body>
</html>";
            var response = Response.AsText(html, "text/html");
            return response;
        }

        private Response GetSwaggerSpec()
        {
            var swaggerDoc = new
            {
                openapi = "3.0.1",
                info = new
                {
                    title = "GCR AutoCount Sync API",
                    version = "1.0.0",
                    description = "REST API for AutoCount ERP\n\n**How to use:**\n1. POST /login with username/password to get JWT token\n2. Click 'Authorize' button at top right\n3. Enter token as: Bearer <your-token>\n4. Now you can test all endpoints"
                },
                servers = new[] { new { url = $@"http://{Auth.ipAddress}:{Auth.port}" } },
                components = new
                {
                    securitySchemes = new Dictionary<string, object>
                    {
                        ["bearerAuth"] = new Dictionary<string, object>
                        {
                            ["type"] = "http",
                            ["scheme"] = "bearer",
                            ["bearerFormat"] = "JWT",
                            ["description"] = "Enter JWT token from /login endpoint. Format: Bearer <token>"
                        }
                    }
                },
                security = new[] { new Dictionary<string, string[]> { ["bearerAuth"] = new string[] { } } },
                paths = GetPaths()
            };
            return Response.AsJson(swaggerDoc);
        }

        private object GetPaths()
        {
            var paths = new Dictionary<string, object>
            {
                #region Authentication
                ["/login"] = GetLoginPathItem(),
                #endregion

                #region Master Data
                ["/SalesAgent/getAll"] = GetPathItem("GET", "Get all Sales Agents", "Master Data", "Retrieve all sales agent records"),
                ["/SalesAgent/getSingle/{agentCode}"] = GetPathItem("GET", "Get Single Sales Agent", "Master Data", "Retrieve a single sales agent"),
                ["/SalesAgent/add"] = GetPathItem("POST", "Add Sales Agent", "Master Data", "Create a new sales agent", true, new {
                    agentCode = "SA001",
                    agentName = "John Doe"
                }),
                ["/SalesAgent/edit"] = GetPathItem("PUT", "Edit Sales Agent", "Master Data", "Update an existing sales agent", true, new {
                    agentCode = "SA001",
                    agentName = "John Doe Updated"
                }),
                ["/SalesAgent/delete/{agentCode}"] = GetPathItem("DELETE", "Delete Sales Agent", "Master Data", "Delete a sales agent"),

                ["/Debtor/getAll"] = GetPathItem("GET", "Get all Debtors", "Master Data", "Retrieve all debtor records"),
                ["/Debtor/getSingle/{debtorCode}"] = GetPathItem("GET", "Get Single Debtor", "Master Data", "Retrieve a single debtor"),
                ["/Debtor/add"] = GetPathItem("POST", "Add Debtor", "Master Data", "Create a new debtor\n\n**Note**: debtorCode must follow existing format (e.g., 2200-T001). Parent GL account 2200-0000 must exist.", true, new {
                    debtorCode = "2200-T001",
                    companyName = "Customer ABC Sdn Bhd",
                    billingAddress1 = "123 Jalan Ampang",
                    billingAddress2 = "Kuala Lumpur",
                    billingAddress3 = "Selangor",
                    billingAddress4 = "50000",
                    deliveryAddress1 = "456 Jalan Tun Razak",
                    deliveryAddress2 = "Kuala Lumpur",
                    deliveryAddress3 = "Selangor",
                    deliveryAddress4 = "55000",
                    phone = "03-12345678",
                    mobile = "012-3456789",
                    fax = "03-12345679",
                    emailAddress = "info@customerabc.com",
                    attention = "Mr. Tan",
                    businessNature = "Trading",
                    creditTerm = "C.O.D",
                    statementType = "O",
                    agingOn = "I",
                    creditLimit = 50000,
                    overdueLimit = 5000
                }),
                ["/Debtor/edit"] = GetPathItem("PUT", "Edit Debtor", "Master Data", "Update an existing debtor", true, new {
                    debtorCode = "2200-T001",
                    companyName = "Customer ABC Updated Sdn Bhd",
                    billingAddress1 = "123 Jalan Ampang",
                    billingAddress2 = "Kuala Lumpur",
                    creditTerm = "Net 60",
                    creditLimit = 75000
                }),
                ["/Debtor/delete/{debtorCode}"] = GetPathItem("DELETE", "Delete Debtor", "Master Data", "Delete a debtor"),

                ["/Creditor/getAll"] = GetPathItem("GET", "Get all Creditors", "Master Data", "Retrieve all creditor records"),
                ["/Creditor/getSingle/{creditorCode}"] = GetPathItem("GET", "Get Single Creditor", "Master Data", "Retrieve a single creditor"),
                ["/Creditor/add"] = GetPathItem("POST", "Add Creditor", "Master Data", "Create a new creditor\n\n**Note**: creditorCode must follow existing format (e.g., 3100-S001). Parent GL account 3100-0000 must exist.", true, new {
                    creditorCode = "3100-S001",
                    companyName = "Supplier XYZ Sdn Bhd",
                    billingAddress1 = "789 Jalan Bukit Bintang",
                    billingAddress2 = "Kuala Lumpur",
                    billingAddress3 = "Wilayah",
                    billingAddress4 = "55100",
                    phone = "03-9876543",
                    mobile = "019-8765432",
                    fax = "03-9876544",
                    emailAddress = "sales@supplierxyz.com",
                    attention = "Ms. Lim",
                    businessNature = "Manufacturing",
                    creditTerm = "C.O.D",
                    statementType = "O",
                    agingOn = "I",
                    creditLimit = 100000,
                    overdueLimit = 10000
                }),
                ["/Creditor/edit"] = GetPathItem("PUT", "Edit Creditor", "Master Data", "Update an existing creditor", true, new {
                    creditorCode = "3100-S001",
                    companyName = "Supplier XYZ Updated Sdn Bhd",
                    creditTerm = "C.O.D",
                    creditLimit = 150000
                }),
                ["/Creditor/delete/{creditorCode}"] = GetPathItem("DELETE", "Delete Creditor", "Master Data", "Delete a creditor"),

                ["/StockGroup/getAll"] = GetPathItem("GET", "Get all Stock Groups", "Master Data", "Retrieve all stock group records"),
                ["/StockGroup/getSingle/{itemGroup}"] = GetPathItem("GET", "Get Single Stock Group", "Master Data", "Retrieve a single stock group"),
                ["/StockGroup/getCodes"] = GetPathItem("GET", "Get GL Codes", "Master Data", "Get general account codes for stock group"),
                ["/StockGroup/add"] = GetPathItem("POST", "Add Stock Group", "Master Data", "Create a new stock group\n\n**Note**: Use JSON Content-Type for nested stockCodes object. Form data supported for simple fields.", true, new {
                    itemGroup = "FG",
                    description = "Finished Goods",
                    stockCodes = new {
                        SalesCode = "5100-0001",
                        CashSalesCode = "5100-0001",
                        SalesReturnCode = "5200-0001",
                        SalesDiscountCode = "5300-0001",
                        PurchaseCode = "6100-0001",
                        CashPurchaseCode = "6100-0001",
                        PurchaseReturnCode = "6200-0001",
                        PurchaseDiscountCode = "6300-0001",
                        BalanceStockCode = "1500-0001"
                    }
                }),
                ["/StockGroup/edit"] = GetPathItem("PUT", "Edit Stock Group", "Master Data", "Update an existing stock group", true, new {
                    itemGroup = "FG",
                    description = "Finished Goods Updated",
                    stockCodes = new {
                        SalesCode = "5100-0001",
                        CashSalesCode = "5100-0001",
                        SalesReturnCode = "5200-0001",
                        SalesDiscountCode = "5300-0001",
                        PurchaseCode = "6100-0001",
                        CashPurchaseCode = "6100-0001",
                        PurchaseReturnCode = "6200-0001",
                        PurchaseDiscountCode = "6300-0001",
                        BalanceStockCode = "1500-0001"
                    }
                }),
                ["/StockGroup/delete/{itemGroup}"] = GetPathItem("DELETE", "Delete Stock Group", "Master Data", "Delete a stock group"),

                ["/StockItem/getAll"] = GetPathItem("GET", "Get all Stock Items", "Master Data", "Retrieve all stock item records"),
                ["/StockItem/getSingle/{itemCode}"] = GetPathItem("GET", "Get Single Stock Item", "Master Data", "Retrieve a single stock item"),
                ["/StockItem/add"] = GetPathItem("POST", "Add Stock Item", "Master Data", "Create a new stock item", true, new {
                    itemCode = "FG-001",
                    description = "Finished Good Item 1",
                    uom = "UNIT",
                    unitCost = "10.00",
                    price = "20.00",
                    costingMethod = "0",
                    itemGroup = "01",
                    leadTime = "0",
                    dutyRate = "0"
                }),
                ["/StockItem/edit"] = GetPathItem("PUT", "Edit Stock Item", "Master Data", "Update an existing stock item", true, new {
                    itemCode = "FG-001",
                    description = "Finished Good Item 1 Updated",
                    uom = "UNIT",
                    unitCost = "12.00",
                    price = "25.00",
                    costingMethod = "0",
                    itemGroup = "01",
                    leadTime = "0",
                    dutyRate = "0"
                }),
                ["/StockItem/delete/{itemCode}"] = GetPathItem("DELETE", "Delete Stock Item", "Master Data", "Delete a stock item"),
                #endregion

                #region Sales
                ["/SalesInvoice/getAll"] = GetPathItem("GET", "Get all Sales Invoices", "Sales", "Retrieve all sales invoice records"),
                ["/SalesInvoice/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Sales Invoice", "Sales", "Retrieve a single sales invoice"),
                ["/SalesInvoice/getDetail/{docNo}"] = GetPathItem("GET", "Get Sales Invoice Details", "Sales", "Retrieve invoice with details"),
                ["/SalesInvoice/add"] = GetPathItem("POST", "Add Sales Invoice", "Sales", "Create a new sales invoice\n\n**ON HOLD**: SDK/DB schema mismatch - 'WithholdingTaxVersion' column required by SDK but not in database. Awaiting resolution.", true, new {
                    docNo = "INV-00001",
                    debtorCode = "2200-J001",
                    date = "2024-01-15",
                    shipInfo = "Deliver to customer address",
                    detailList = new[] {
                        new {
                            itemCode = "01Z01Z01001BLU",
                            uom = "UNT",
                            quantity = 10,
                            unitPrice = 10.00,
                            discount = "0"
                        }
                    }
                }),
                ["/SalesInvoice/delete/{docNo}"] = GetPathItem("DELETE", "Delete Sales Invoice", "Sales", "Delete a sales invoice"),

                ["/CashSale/getAll"] = GetPathItem("GET", "Get all Cash Sales", "Sales", "Retrieve all cash sale records"),
                ["/CashSale/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Cash Sale", "Sales", "Retrieve a single cash sale"),
                ["/CashSale/getDetail/{docNo}"] = GetPathItem("GET", "Get Cash Sale Details", "Sales", "Retrieve a cash sale with details"),
                ["/CashSale/add"] = GetPathItem("POST", "Add Cash Sale", "Sales", "Create a new cash sale", true, new {
                    docNo = "CS-00001",
                    debtorCode = "300-C001",
                    date = "2024-01-15",
                    shipInfo = "Deliver to Front Desk",
                    paymentMode = 1,
                    cashPayment = 110.40,
                    detailList = new[] {
                        new {
                            itemCode = "FG00001",
                            quantity = 1,
                            unitPrice = 50.20,
                            uom = "UNIT",
                            discount = "0"
                        }
                    }
                }),
                ["/CashSale/edit"] = GetPathItem("PUT", "Edit Cash Sale", "Sales", "Update an existing cash sale", true, new {
                    docNo = "CS-00001",
                    debtorCode = "300-C001",
                    date = "2024-01-15",
                    shipInfo = "Deliver to Front Desk",
                    paymentMode = 1,
                    cashPayment = 110.40,
                    detailList = new[] {
                        new {
                            itemCode = "FG00001",
                            quantity = 1,
                            unitPrice = 50.20,
                            uom = "UNIT",
                            discount = "0"
                        }
                    }
                }),
                ["/CashSale/delete/{docNo}"] = GetPathItem("DELETE", "Delete Cash Sale", "Sales", "Delete a cash sale"),
                #endregion

                #region Purchase
                ["/PurchaseOrder/getAll"] = GetPathItem("GET", "Get all Purchase Orders", "Purchase", "Retrieve all purchase order records"),
                ["/PurchaseOrder/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Purchase Order", "Purchase", "Retrieve a single purchase order"),
                ["/PurchaseOrder/add"] = GetPathItem("POST", "Add Purchase Order", "Purchase", "Create a new purchase order", true, new {
                    docNo = "PO-00001",
                    creditorCode = "400-S001",
                    date = "2024-01-15",
                    shipInfo = "Ship to Warehouse",
                    detailList = new[] {
                        new { itemCode = "RM00001", uom = "UNIT", quantity = 100, unitPrice = 10.50, discount = "5%" }
                    }
                }),
                ["/PurchaseOrder/edit"] = GetPathItem("PUT", "Edit Purchase Order", "Purchase", "Update an existing purchase order", true, new {
                    docNo = "PO-00001",
                    creditorCode = "400-S001",
                    date = "2024-01-15",
                    shipInfo = "Ship to Warehouse A",
                    detailList = new[] {
                        new { itemCode = "RM00001", uom = "UNIT", quantity = 150, unitPrice = 10.50, discount = "5%" }
                    }
                }),
                ["/PurchaseOrder/delete/{docNo}"] = GetPathItem("DELETE", "Delete Purchase Order", "Purchase", "Delete a purchase order"),

                ["/GoodsReceivedNote/getAll"] = GetPathItem("GET", "Get all Goods Received Notes", "Purchase", "Retrieve all GRN records"),
                ["/GoodsReceivedNote/getSingle/{docNo}"] = GetPathItem("GET", "Get Single GRN", "Purchase", "Retrieve a single GRN"),
                ["/GoodsReceivedNote/add"] = GetPathItem("POST", "Add Goods Received Note", "Purchase", "Create a new GRN", true, new {
                    docNo = "GRN-00001",
                    creditorCode = "400-S001",
                    date = "2024-01-15",
                    shipInfo = "Delivered to HQ",
                    detailList = new[] {
                        new { itemCode = "RM00001", uom = "UNIT", quantity = 100, unitPrice = 10.50 }
                    }
                }),
                ["/GoodsReceivedNote/edit"] = GetPathItem("PUT", "Edit Goods Received Note", "Purchase", "Update an existing GRN", true, new {
                    docNo = "GRN-00001",
                    creditorCode = "400-S001",
                    date = "2024-01-15",
                    shipInfo = "Delivered to Warehouse B",
                    detailList = new[] {
                        new { itemCode = "RM00001", uom = "UNIT", quantity = 150, unitPrice = 10.50 }
                    }
                }),
                ["/GoodsReceivedNote/delete/{docNo}"] = GetPathItem("DELETE", "Delete Goods Received Note", "Purchase", "Delete a GRN"),
                #endregion

                #region Stock Transactions
                ["/StockAdjustment/getAll"] = GetPathItem("GET", "Get all Stock Adjustments", "Stock Transactions", "Retrieve all stock adjustment records"),
                ["/StockAdjustment/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Stock Adjustment", "Stock Transactions", "Retrieve a single stock adjustment"),
                ["/StockAdjustment/add"] = GetPathItem("POST", "Add Stock Adjustment", "Stock Transactions", "Create a new stock adjustment", true, new {
                    docNo = "SA-00001",
                    docDate = "2024-01-15",
                    description = "Stock Adjustment entry",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 10, unitCost = 5.00 }
                    }
                }),
                ["/StockAdjustment/edit"] = GetPathItem("PUT", "Edit Stock Adjustment", "Stock Transactions", "Update an existing stock adjustment", true, new {
                    docNo = "SA-00001",
                    docDate = "2024-01-15",
                    description = "Stock Adjustment entry updated",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 15, unitCost = 5.00 }
                    }
                }),
                ["/StockAdjustment/delete/{docNo}"] = GetPathItem("DELETE", "Delete Stock Adjustment", "Stock Transactions", "Delete a stock adjustment"),

                ["/StockTransfer/getAll"] = GetPathItem("GET", "Get all Stock Transfers", "Stock Transactions", "Retrieve all stock transfer records"),
                ["/StockTransfer/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Stock Transfer", "Stock Transactions", "Retrieve a single stock transfer"),
                ["/StockTransfer/getDetail/{docNo}"] = GetPathItem("GET", "Get Stock Transfer Details", "Stock Transactions", "Retrieve transfer with details"),
                ["/StockTransfer/add"] = GetPathItem("POST", "Add Stock Transfer", "Stock Transactions", "Create a new stock transfer", true, new {
                    docNo = "TR-00001",
                    docDate = "2024-01-15",
                    fromLocation = "HQ",
                    toLocation = "BRANCH1",
                    reason = "Restock",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 100 }
                    }
                }),
                ["/StockTransfer/edit"] = GetPathItem("PUT", "Edit Stock Transfer", "Stock Transactions", "Update an existing stock transfer", true, new {
                    docNo = "TR-00001",
                    docDate = "2024-01-15",
                    fromLocation = "HQ",
                    toLocation = "BRANCH1",
                    reason = "Restock Updated",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 150 }
                    }
                }),
                ["/StockTransfer/delete/{docNo}"] = GetPathItem("DELETE", "Delete Stock Transfer", "Stock Transactions", "Delete a stock transfer"),

                ["/StockAssembly/getAll"] = GetPathItem("GET", "Get all Stock Assemblies", "Stock Transactions", "Retrieve all stock assembly records"),
                ["/StockAssembly/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Stock Assembly", "Stock Transactions", "Retrieve a single stock assembly"),
                ["/StockAssembly/getDetail/{docNo}"] = GetPathItem("GET", "Get Stock Assembly Details", "Stock Transactions", "Retrieve assembly with details"),
                ["/StockAssembly/add"] = GetPathItem("POST", "Add Stock Assembly", "Stock Transactions", "Create a new stock assembly", true, new {
                    docNo = "AS-00001",
                    docDate = "2024-01-15",
                    description = "Assemble PC",
                    itemCode = "FG00002",
                    quantity = 1,
                    materialList = new[] {
                        new { itemCode = "RM00001", quantity = 2, uom = "UNIT", unitCost = 5.00 }
                    }
                }),
                ["/StockAssembly/edit"] = GetPathItem("PUT", "Edit Stock Assembly", "Stock Transactions", "Update an existing stock assembly", true, new {
                    docNo = "AS-00001",
                    docDate = "2024-01-15",
                    description = "Assemble PC Updated",
                    itemCode = "FG00002",
                    quantity = 2,
                    materialList = new[] {
                        new { itemCode = "RM00001", quantity = 4, uom = "UNIT", unitCost = 5.00 }
                    }
                }),
                ["/StockAssembly/delete/{docNo}"] = GetPathItem("DELETE", "Delete Stock Assembly", "Stock Transactions", "Delete a stock assembly"),

                ["/StockWriteOff/getAll"] = GetPathItem("GET", "Get all Stock Write Off", "Stock Transactions", "Retrieve all stock write off records"),
                ["/StockWriteOff/getSingle/{docNo}"] = GetPathItem("GET", "Get Single Stock Write Off", "Stock Transactions", "Retrieve a single stock write off"),
                ["/StockWriteOff/add"] = GetPathItem("POST", "Add Stock Write Off", "Stock Transactions", "Create a new stock write off", true, new {
                    docNo = "SW-00001",
                    docDate = "2024-01-15",
                    description = "Write off damaged goods",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 5 }
                    }
                }),
                ["/StockWriteOff/edit"] = GetPathItem("PUT", "Edit Stock Write Off", "Stock Transactions", "Update an existing stock write off", true, new {
                    docNo = "SW-00001",
                    docDate = "2024-01-15",
                    description = "Write off damaged goods updated",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 2 }
                    }
                }),
                ["/StockWriteOff/delete/{docNo}"] = GetPathItem("DELETE", "Delete Stock Write Off", "Stock Transactions", "Delete a stock write off"),

                ["/StockReceive/getAll"] = GetPathItem("GET", "Get all Stock Receive", "Stock Transactions", "Retrieve all stock receive records"),
                ["/StockReceive/add"] = GetPathItem("POST", "Add Stock Receive", "Stock Transactions", "Create a new stock receive", true, new {
                    docNo = "SR-00001",
                    docDate = "2024-01-15",
                    description = "Receive missing items",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 10, unitCost = 5.00 }
                    }
                }),

                ["/StockIssue/getAll"] = GetPathItem("GET", "Get all Stock Issue", "Stock Transactions", "Retrieve all stock issue records"),
                ["/StockIssue/add"] = GetPathItem("POST", "Add Stock Issue", "Stock Transactions", "Create a new stock issue", true, new {
                    docNo = "SI-00001",
                    docDate = "2024-01-15",
                    description = "Issue items for marketing",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 5 }
                    }
                }),

                ["/StockUpdateCost/getAll"] = GetPathItem("GET", "Get all Stock Update Cost", "Stock Transactions", "Retrieve all stock update cost records"),
                ["/StockUpdateCost/add"] = GetPathItem("POST", "Add Stock Update Cost", "Stock Transactions", "Create a new stock update cost", true, new {
                    docNo = "SU-00001",
                    docDate = "2024-01-15",
                    description = "Cost update",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", newCost = 6.50 }
                    }
                }),

                ["/StockTake/getAll"] = GetPathItem("GET", "Get all Stock Take", "Stock Transactions", "Retrieve all stock take records"),
                ["/StockTake/add"] = GetPathItem("POST", "Add Stock Take", "Stock Transactions", "Create a new stock take", true, new {
                    docNo = "ST-00001",
                    docDate = "2024-01-15",
                    description = "Year end stock take",
                    detailList = new[] {
                        new { itemCode = "FG00001", uom = "UNIT", quantity = 95 }
                    }
                }),
                #endregion

                #region Database
                ["/getTId"] = GetPathItem("GET", "Get Transaction ID", "Database", "Get current transaction ID from SQL log"),
                #endregion
            };
            return paths;
        }

        private object GetPathItem(string method, string summary, string tag, string description, bool hasBody = false, object example = null)
        {
            var operations = new Dictionary<string, object>
            {
                ["summary"] = summary,
                ["tags"] = new[] { tag },
                ["description"] = description + "\n\n**Authentication required**: Include 'Authorization: Bearer <token>' header or '?token=<token>' query parameter.",
                ["security"] = new[] { new { bearerAuth = new string[] { } } },
                ["responses"] = new Dictionary<string, object>
                {
                    ["200"] = new Dictionary<string, object> { ["description"] = "Successful response" },
                    ["400"] = new Dictionary<string, object> { ["description"] = "Bad request" },
                    ["401"] = new Dictionary<string, object> { ["description"] = "Unauthorized - Invalid or missing token" },
                    ["500"] = new Dictionary<string, object> { ["description"] = "Server error" }
                }
            };

            // Add OData parameters for GET requests
            if (method.ToLower() == "get")
            {
                string selectDescription = GetSelectDescription(tag);
                operations["parameters"] = new[]
                {
                    new Dictionary<string, object> { ["name"] = "$select", ["in"] = "query", ["description"] = selectDescription, ["required"] = false, ["schema"] = new { type = "string" } },
                    new Dictionary<string, object> { ["name"] = "$filter", ["in"] = "query", ["description"] = "Filter results (e.g., DocNo eq 'SO-0001')", ["required"] = false, ["schema"] = new { type = "string" } },
                    new Dictionary<string, object> { ["name"] = "$orderby", ["in"] = "query", ["description"] = "Order by field (e.g., DocDate desc)", ["required"] = false, ["schema"] = new { type = "string" } },
                    new Dictionary<string, object> { ["name"] = "$top", ["in"] = "query", ["description"] = "Max records (default: 100, max: 1000)", ["required"] = false, ["schema"] = new { type = "integer" } }
                };
            }

            if (hasBody)
            {
                var contentDict = new Dictionary<string, object>
                {
                    ["schema"] = new Dictionary<string, object> { ["type"] = "object" }
                };
                if (example != null)
                {
                    contentDict["example"] = example;
                }

                operations["requestBody"] = new Dictionary<string, object>
                {
                    ["content"] = new Dictionary<string, object>
                    {
                        ["application/json"] = contentDict
                    }
                };
            }

            var pathItem = new Dictionary<string, object>();
            var methodLower = method.ToLower();
            pathItem[methodLower] = operations;
            return pathItem;
        }

        private string GetSelectDescription(string tag)
        {
            var fields = new Dictionary<string, string[]>
            {
                ["SalesOrder"] = new[] { "DocNo", "DocDate", "DebtorCode", "SalesAgent", "Total", "Status" },
                ["SalesInvoice"] = new[] { "DocNo", "DocDate", "DebtorCode", "SalesAgent", "Total", "Status" },
                ["Quotation"] = new[] { "DocNo", "DocDate", "DebtorCode", "SalesAgent", "Total", "Status" },
                ["DeliveryOrder"] = new[] { "DocNo", "DocDate", "DebtorCode", "SalesAgent", "Total", "Status" },
                ["DeliveryReturn"] = new[] { "DocNo", "DocDate", "DebtorCode", "Total", "Status" },
                ["Debtor"] = new[] { "DebtorCode", "CompanyName", "Address1", "Address2", "Phone", "Fax", "Email" },
                ["DebitNote"] = new[] { "DocNo", "DocDate", "DebtorCode", "Total", "Status" },
                ["CreditNote"] = new[] { "DocNo", "DocDate", "DebtorCode", "Total", "Status" },
                ["CashSale"] = new[] { "DocNo", "DocDate", "DebtorCode", "Total", "Status" },
                ["PurchaseOrder"] = new[] { "DocNo", "DocDate", "CreditorCode", "Total", "Status" },
                ["Creditor"] = new[] { "CreditorCode", "CompanyName", "Address1", "Address2", "Phone", "Fax", "Email" },
                ["GoodsReceivedNote"] = new[] { "DocNo", "DocDate", "CreditorCode", "Total", "Status" },
                ["StockItem"] = new[] { "ItemCode", "Description", "ItemGroup", "UOM", "BalQty", "AvgCost" },
                ["StockGroup"] = new[] { "GroupCode", "Description" },
                ["StockAdjustment"] = new[] { "DocNo", "DocDate", "Total", "Status" },
                ["StockWriteOff"] = new[] { "DocNo", "DocDate", "Total", "Status" },
                ["StockTransfer"] = new[] { "DocNo", "DocDate", "FromLoc", "ToLoc", "Total", "Status" },
                ["StockAssembly"] = new[] { "DocNo", "DocDate", "Total", "Status" },
                ["SalesAgent"] = new[] { "AgentCode", "AgentName", "Phone", "Email" },
                ["JournalEntry"] = new[] { "DocNo", "DocDate", "Description", "TotalDebit", "TotalCredit" }
            };

            if (fields.ContainsKey(tag))
            {
                return "Select fields (comma-separated).\nAvailable: " + string.Join(", ", fields[tag]);
            }
            return "Select specific fields (comma-separated)";
        }

        private object GetLoginPathItem()
        {
            return new Dictionary<string, object>
            {
                ["post"] = new Dictionary<string, object>
                {
                    ["summary"] = "Login to AutoCount",
                    ["tags"] = new[] { "Auth" },
                    ["description"] = "Authenticate with AutoCount and receive JWT token. No authentication required.",
                    ["security"] = new object[] { }, // This removes the lock icon from the login endpoint
                    ["requestBody"] = new Dictionary<string, object>
                    {
                        ["content"] = new Dictionary<string, object>
                        {
                            ["application/x-www-form-urlencoded"] = new Dictionary<string, object>
                            {
                                ["schema"] = new Dictionary<string, object>
                                {
                                    ["type"] = "object",
                                    ["required"] = new[] { "username", "password" },
                                    ["properties"] = new Dictionary<string, object>
                                    {
                                        ["username"] = new Dictionary<string, object> { ["type"] = "string", ["example"] = "KENNY", ["default"] = "KENNY" },
                                        ["password"] = new Dictionary<string, object> { ["type"] = "string", ["example"] = "1111", ["default"] = "1111" }
                                    }
                                }
                            }
                        }
                    },
                    ["responses"] = new Dictionary<string, object>
                    {
                        ["200"] = new Dictionary<string, object>
                        {
                            ["description"] = "Login successful",
                            ["content"] = new Dictionary<string, object>
                            {
                                ["application/json"] = new Dictionary<string, object>
                                {
                                    ["example"] = new { success = true, token = "jwt-token-here", message = "Login successful" }
                                }
                            }
                        },
                        ["401"] = new Dictionary<string, object> { ["description"] = "Invalid credentials" }
                    }
                }
            };
        }
    }
}
