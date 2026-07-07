using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services;

public interface IProductReviewService : IBaseCRUDService<ProductReviewResponse, ProductReviewSearchObject, ProductReviewInsertRequest, ProductReviewUpdateRequest>
{
}
