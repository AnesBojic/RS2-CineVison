using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IAgeRatingService : IBaseCRUDService<AgeRatingResponse, LookupSearchObject, AgeRatingInsertRequest, AgeRatingUpdateRequest>
    {
    }
}
