using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class HallsController : BaseCRUDController<HallResponse, HallSearchObject, HallInsertRequest, HallUpdateRequest, IHallService>
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService) : base(hallService)
    {
        _hallService = hallService;
    }

    [HttpPut("{id}/SeatLayout")]
    public async Task<HallResponse> UpdateSeatLayout(int id, [FromBody] HallSeatLayoutUpdateRequest request)
    {
        return await _hallService.UpdateSeatLayoutAsync(id, request);
    }
}
