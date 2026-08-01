using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class AgeRatingsController : BaseCRUDController<AgeRatingResponse, LookupSearchObject, AgeRatingInsertRequest, AgeRatingUpdateRequest, IAgeRatingService>
{
    public AgeRatingsController(IAgeRatingService service) : base(service)
    {
    }
}
