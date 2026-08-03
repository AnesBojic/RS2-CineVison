using Microsoft.AspNetCore.Mvc;
using CineVision.Services;
using CineVision.Model.SearchObjects;
using CineVision.Model.Responses;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

/// <summary>Read-only base. Derived controllers must declare [Authorize] (or stronger).</summary>
[ApiController]
[Route("[controller]")]
public abstract class BaseReadController<TResponse, TSearch, TService> : ControllerBase
    where TSearch : BaseSearchObject
    where TService : IBaseReadService<TResponse, TSearch>
{
    protected readonly TService _service;

    protected BaseReadController(TService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    public virtual async Task<PageResult<TResponse>> GetAll([FromQuery] TSearch? search)
    {
        var results = await _service.GetAllAsync(search);
        return results;
    }

    [HttpGet("{id}")]
    [Authorize]
    public virtual async Task<ActionResult<TResponse>> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
