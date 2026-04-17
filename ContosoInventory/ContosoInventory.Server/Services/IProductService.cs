using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Defines operations for managing inventory products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves all products, optionally filtered by category.
    /// </summary>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>List of products.</returns>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null);

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product DTO, or null if not found.</returns>
    Task<ProductResponseDto?> GetProductByIdAsync(int id);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="dto">The product creation data.</param>
    /// <returns>The created product DTO.</returns>
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The updated product data.</param>
    /// <returns>The updated product DTO, or null if not found.</returns>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto);

    /// <summary>
    /// Deletes a product by its unique identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>True if the product was deleted, false if not found.</returns>
    Task<bool> DeleteProductAsync(int id);

    /// <summary>
    /// Increases the stock quantity for a product.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="dto">The restock data.</param>
    /// <returns>The updated product DTO, or null if not found.</returns>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto);
}
