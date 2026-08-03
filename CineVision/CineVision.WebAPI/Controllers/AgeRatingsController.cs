using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services;
using Microsoft.AspNetCore.Authorization;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class AgeRatingsController : BaseCRUDController<AgeRatingResponse, LookupSearchObject, AgeRatingInsertRequest, AgeRatingUpdateRequest, IAgeRatingService>
{
    public AgeRatingsController(IAgeRatingService service) : base(service)
    {
    }
}
