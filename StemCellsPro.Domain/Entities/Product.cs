using System.ComponentModel.DataAnnotations.Schema;

namespace StemCellsPro.Domain.Entities;

[Table("ProductDetail")]
public class Product : BaseEntity
{
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Finish { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal GSTPercent { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Location { get; set; } = string.Empty;
}
