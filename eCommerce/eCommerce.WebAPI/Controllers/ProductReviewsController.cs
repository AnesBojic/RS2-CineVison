using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class ProductReviewsController
    : BaseCRUDController<ProductReviewResponse, ProductReviewSearchObject, ProductReviewInsertRequest, ProductReviewUpdateRequest, IProductReviewService>
{
    public ProductReviewsController(IProductReviewService productReviewService)
        : base(productReviewService)
    {
    }
}
