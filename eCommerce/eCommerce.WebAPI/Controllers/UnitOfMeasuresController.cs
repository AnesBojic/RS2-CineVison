using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Model.Requests;

namespace eCommerce.WebAPI.Controllers;

public class UnitOfMeasuresController : BaseCRUDController<UnitOfMeasureResponse, UnitOfMeasureSearch, UnitOfMeasureInsertRequest, UnitOfMeasureUpdateRequest, IUnitOfMeasureService>
{
    public UnitOfMeasuresController(IUnitOfMeasureService unitOfMeasureService) : base(unitOfMeasureService)
    {
    }
}
