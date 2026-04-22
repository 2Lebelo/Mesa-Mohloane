namespace Mesa_Mohloane_Backend.Models.Entities;

public class TenderLineItem : BaseEntity
{
    public Guid TenderApplicationId { get; set; }
    public TenderLineItemCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public TenderApplication? TenderApplication { get; set; }
}
