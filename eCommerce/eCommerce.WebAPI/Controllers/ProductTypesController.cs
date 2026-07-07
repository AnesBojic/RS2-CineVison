using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Model.Requests;

namespace eCommerce.WebAPI.Controllers;

public class ProductTypesController : BaseCRUDController<ProductTypeResponse, ProductTypeSearch, ProductTypeInsertRequest, ProductTypeUpdateRequest, IProductTypeService>
{
    public ProductTypesController(IProductTypeService productTypeService) : base(productTypeService)
    {
    }
}
