using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Provides operations for managing inventory products.
/// </summary>
public class ProductService : IProductService
{
    private readonly InventoryContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(InventoryContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId = null)
    {
        try
        {
            var query = _context.Products.AsNoTracking();
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            var products = await query.ToListAsync();
            return products.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
    {
        try
        {
            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return product == null ? null : MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
    {
        // Input validation
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 2 || dto.Name.Length > 100)
            throw new ArgumentException("Product name is required and must be 2-100 characters.");
        if (string.IsNullOrWhiteSpace(dto.Sku) || dto.Sku.Length < 2 || dto.Sku.Length > 50)
            throw new ArgumentException("SKU is required and must be 2-50 characters.");
        if (dto.Price < 0.01m) throw new ArgumentException("Price must be greater than zero.");
        if (dto.StockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative.");

        try
        {
            // Check for duplicate SKU
            var exists = await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.ToLower());
            if (exists)
                throw new InvalidOperationException($"A product with the SKU '{dto.Sku}' already exists.");

            // Check category exists
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = dto.Name,
                Sku = dto.Sku,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                CreatedDate = now,
                LastUpdatedDate = now
            };
            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("UNIQUE") == true)
            {
                _logger.LogWarning(dbEx, "Unique constraint violation when creating product with SKU '{Sku}'.", dto.Sku);
                throw new InvalidOperationException($"A product with the SKU '{dto.Sku}' already exists.");
            }
            _logger.LogInformation("Product created: {ProductName} (ID: {ProductId}, SKU: {Sku}).", product.Name, product.Id, product.Sku);
            return MapToDto(product);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product '{ProductName}'.", dto.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 2 || dto.Name.Length > 100)
            throw new ArgumentException("Product name is required and must be 2-100 characters.");
        if (string.IsNullOrWhiteSpace(dto.Sku) || dto.Sku.Length < 2 || dto.Sku.Length > 50)
            throw new ArgumentException("SKU is required and must be 2-50 characters.");
        if (dto.Price < 0.01m) throw new ArgumentException("Price must be greater than zero.");
        if (dto.StockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative.");

        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            // Check for duplicate SKU (excluding current product)
            var exists = await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.ToLower() && p.Id != id);
            if (exists)
                throw new InvalidOperationException($"A product with the SKU '{dto.Sku}' already exists.");

            // Check category exists
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with ID {dto.CategoryId} does not exist.");

            product.Name = dto.Name;
            product.Sku = dto.Sku;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.LastUpdatedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("UNIQUE") == true)
            {
                _logger.LogWarning(dbEx, "Unique constraint violation when updating product with SKU '{Sku}'.", dto.Sku);
                throw new InvalidOperationException($"A product with the SKU '{dto.Sku}' already exists.");
            }
            _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId}, SKU: {Sku}).", product.Name, product.Id, product.Sku);
            return MapToDto(product);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Product deleted: {ProductName} (ID: {ProductId}).", product.Name, product.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (dto.Quantity < 1) throw new ArgumentException("Quantity must be greater than zero.");
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;
            product.StockQuantity += dto.Quantity;
            product.LastUpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Product restocked: {ProductName} (ID: {ProductId}), Quantity Added: {Quantity}.", product.Name, product.Id, dto.Quantity);
            return MapToDto(product);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product with ID {ProductId}.", id);
            throw;
        }
    }

    private static ProductResponseDto MapToDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CreatedDate = product.CreatedDate,
            LastUpdatedDate = product.LastUpdatedDate
        };
    }
}
