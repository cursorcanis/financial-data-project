using System;

namespace FinancialDataApp.Core.Entities
{
    public enum DataType
    {
        String,
        Number,
        Boolean,
        DateTime,
        Json
    }

    public class DynamicField : BaseEntity
    {
        public string FieldDefinition { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public DataType DataType { get; set; }
    }
}
