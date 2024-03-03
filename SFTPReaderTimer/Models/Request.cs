using Newtonsoft.Json;
using System.Collections.Generic;

namespace SFTPReaderTimer.Models
{
    public class OrderDetails
    {
        [JsonProperty("Invoice_header")]
        public List<InvoiceHeader> InvoiceHeader { get; set; }

        [JsonProperty("Invoice_line")]
        public List<InvoiceLine> InvoiceLine { get; set; }

        [JsonProperty("payment_method")]
        public List<PaymentMethod> PaymentMethod { get; set; }
    }

    public class InvoiceHeader
    {
        [JsonProperty("CustNo")]
        public string CustNo { get; set; }

        [JsonProperty("kti_sourceSalesOrderid")]
        public string KtiSourceSalesOrderid { get; set; }

        [JsonProperty("customername")]
        public string Customername { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("phoneno")]
        public string Phoneno { get; set; }
    }

    public class InvoiceLine
    {
        [JsonProperty("kti_sourceSalesOrderid")]
        public string KtiSourceSalesOrderid { get; set; }

        [JsonProperty("ItemNo")]
        public string ItemNo { get; set; }

        [JsonProperty("VariantCode")]
        public string VariantCode { get; set; }

        [JsonProperty("Quantity")]
        public string Quantity { get; set; }

        [JsonProperty("UnitofMeasureCode")]
        public string UnitofMeasureCode { get; set; }

        [JsonProperty("kti_sourcesalesorderitemid")]
        public string KtiSourcesalesorderitemid { get; set; }

        [JsonProperty("UnitCost")]
        public string UnitCost { get; set; }

        [JsonProperty("Price")]
        public string Price { get; set; }
    }

    public class TenderType
    {
        [JsonProperty("TRN")]
        public string TRN { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }
    }

    public class PaymentMethod
    {
        [JsonProperty("kti_sourceSalesOrderid")]
        public string KtiSourceSalesOrderid { get; set; }

        [JsonProperty("tender_type")]
        public List<TenderType> TenderType { get; set; }
    }

    public class OrderAndFileDetails
    {
        [JsonProperty("root")]
        public string Root { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("order_details")]
        public OrderDetails OrderDetails { get; set; }
    }
}