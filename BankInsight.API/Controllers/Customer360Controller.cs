using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/customer360")]
public class Customer360Controller : ControllerBase
{
    private readonly Customer360Service _customer360Service;

    public Customer360Controller(Customer360Service customer360Service)
    {
        _customer360Service = customer360Service;
    }

    [HttpGet("{customerId}")]
    [RequirePermission("customers.view")]
    public async Task<ActionResult<Customer360Response>> GetCustomer360(string customerId)
    {
        var response = await _customer360Service.GetCustomer360Async(customerId);
        if (response == null)
        {
            return NotFound(new { message = "Customer not found" });
        }

        return Ok(response);
    }
}
