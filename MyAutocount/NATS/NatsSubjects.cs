namespace GCR_autocount_api.NATS
{
    public static class NatsSubjects
    {
        public static class Sales
        {
            public const string Prefix = "sales";
            
            public const string InvoiceCreated = "sales.invoice.created";
            public const string InvoiceUpdated = "sales.invoice.updated";
            public const string InvoiceDeleted = "sales.invoice.deleted";
            
            public const string OrderCreated = "sales.order.created";
            public const string OrderUpdated = "sales.order.updated";
            public const string OrderDeleted = "sales.order.deleted";
            
            public const string QuotationCreated = "sales.quotation.created";
            public const string QuotationUpdated = "sales.quotation.updated";
            public const string QuotationDeleted = "sales.quotation.deleted";
            
            public const string CashSaleCreated = "sales.cashsale.created";
            public const string CashSaleUpdated = "sales.cashsale.updated";
            public const string CashSaleDeleted = "sales.cashsale.deleted";
            
            public const string CreditNoteCreated = "sales.creditnote.created";
            public const string CreditNoteUpdated = "sales.creditnote.updated";
            public const string CreditNoteDeleted = "sales.creditnote.deleted";
            
            public const string DebitNoteCreated = "sales.debitnote.created";
            public const string DebitNoteUpdated = "sales.debitnote.updated";
            public const string DebitNoteDeleted = "sales.debitnote.deleted";
            
            public const string DeliveryOrderCreated = "sales.deliveryorder.created";
            public const string DeliveryOrderUpdated = "sales.deliveryorder.updated";
            public const string DeliveryOrderDeleted = "sales.deliveryorder.deleted";
            
            public const string DeliveryReturnCreated = "sales.deliveryreturn.created";
            public const string DeliveryReturnUpdated = "sales.deliveryreturn.updated";
            public const string DeliveryReturnDeleted = "sales.deliveryreturn.deleted";
            
            public const string CancelSOCreated = "sales.cancelso.created";
            public const string CancelSOUpdated = "sales.cancelso.updated";
            public const string CancelSODeleted = "sales.cancelso.deleted";
        }
        
        public static class Purchase
        {
            public const string Prefix = "purchase";
            
            public const string OrderCreated = "purchase.order.created";
            public const string OrderUpdated = "purchase.order.updated";
            public const string OrderDeleted = "purchase.order.deleted";
            
            public const string GRNCreated = "purchase.grn.created";
            public const string GRNUpdated = "purchase.grn.updated";
            public const string GRNDeleted = "purchase.grn.deleted";
            
            public const string CancelPOCreated = "purchase.cancelpo.created";
            public const string CancelPOUpdated = "purchase.cancelpo.updated";
            public const string CancelPODeleted = "purchase.cancelpo.deleted";
        }
        
        public static class Stock
        {
            public const string Prefix = "stock";
            
            public const string ItemCreated = "stock.item.created";
            public const string ItemUpdated = "stock.item.updated";
            public const string ItemDeleted = "stock.item.deleted";
            
            public const string GroupCreated = "stock.group.created";
            public const string GroupUpdated = "stock.group.updated";
            public const string GroupDeleted = "stock.group.deleted";
            
            public const string AdjustmentCreated = "stock.adjustment.created";
            public const string AdjustmentUpdated = "stock.adjustment.updated";
            public const string AdjustmentDeleted = "stock.adjustment.deleted";
            
            public const string ReceiveCreated = "stock.receive.created";
            public const string ReceiveUpdated = "stock.receive.updated";
            public const string ReceiveDeleted = "stock.receive.deleted";
            
            public const string IssueCreated = "stock.issue.created";
            public const string IssueUpdated = "stock.issue.updated";
            public const string IssueDeleted = "stock.issue.deleted";
            
            public const string TransferCreated = "stock.transfer.created";
            public const string TransferUpdated = "stock.transfer.updated";
            public const string TransferDeleted = "stock.transfer.deleted";
            
            public const string TakeCreated = "stock.take.created";
            public const string TakeUpdated = "stock.take.updated";
            public const string TakeDeleted = "stock.take.deleted";
            
            public const string WriteOffCreated = "stock.writeoff.created";
            public const string WriteOffUpdated = "stock.writeoff.updated";
            public const string WriteOffDeleted = "stock.writeoff.deleted";
            
            public const string AssemblyCreated = "stock.assembly.created";
            public const string AssemblyUpdated = "stock.assembly.updated";
            public const string AssemblyDeleted = "stock.assembly.deleted";
            
            public const string UpdateCostCreated = "stock.updatecost.created";
            public const string UpdateCostUpdated = "stock.updatecost.updated";
            public const string UpdateCostDeleted = "stock.updatecost.deleted";
        }
        
        public static class MasterData
        {
            public const string Prefix = "master";
            
            public const string DebtorCreated = "master.debtor.created";
            public const string DebtorUpdated = "master.debtor.updated";
            public const string DebtorDeleted = "master.debtor.deleted";
            
            public const string CreditorCreated = "master.creditor.created";
            public const string CreditorUpdated = "master.creditor.updated";
            public const string CreditorDeleted = "master.creditor.deleted";
            
            public const string SalesAgentCreated = "master.salesagent.created";
            public const string SalesAgentUpdated = "master.salesagent.updated";
            public const string SalesAgentDeleted = "master.salesagent.deleted";
        }
        
        public static class GL
        {
            public const string Prefix = "gl";
            
            public const string JournalEntryCreated = "gl.journalentry.created";
            public const string JournalEntryUpdated = "gl.journalentry.updated";
            public const string JournalEntryDeleted = "gl.journalentry.deleted";
        }
    }
}
