using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Lighthouse.Common;
using Lighthouse.Common.MongoDb;
using Lighthouse.Inventory.Service.Clients;
using Lighthouse.Inventory.Service.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string AdminRole = "Admin";
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }

    [HttpGet("{userId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId != Guid.Parse(currentUserId))
        {
            if (!User.IsInRole(AdminRole))
            {
                return Forbid();
            }
        }

        var inventoryItems = await _inventoryItemsRepository.GetAllAsync(item => item.UserId == userId);
        var itemIds = inventoryItems.Select(item => item.ItemId);
        var catalogItems = await _catalogItemsRepository.GetAllAsync(item => itemIds.Contains(item.Id));

        var inventoryItemDtos = inventoryItems.Select(item =>
        {
            var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == item.ItemId);
            return item.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(inventoryItemDtos);
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
        var existingItem = await _inventoryItemsRepository.GetAsync(
            item => item.UserId == grantItemsDto.UserId
            && item.ItemId == grantItemsDto.CatalogItemId
        );

        if (existingItem is null)
        {
            var newItem = new InventoryItem
            {
                ItemId = grantItemsDto.CatalogItemId,
                UserId = grantItemsDto.UserId,
                Quantity = grantItemsDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            await _inventoryItemsRepository.CreateAsync(newItem);
        }
        else
        {
            existingItem.Quantity += grantItemsDto.Quantity;
            await _inventoryItemsRepository.UpdateAsync(existingItem);
        }

        return Ok();
    }
}