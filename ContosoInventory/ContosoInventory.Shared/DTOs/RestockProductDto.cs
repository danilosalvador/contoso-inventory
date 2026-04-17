using System.ComponentModel.DataAnnotations;

namespace ContosoInventory.Shared.DTOs;

public class RestockProductDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; set; }
}
