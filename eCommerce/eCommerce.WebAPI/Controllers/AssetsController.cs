using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Model.Requests;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
public class AssetsController : BaseCRUDController<AssetResponse, AssetSearch, AssetInsertRequest, AssetUpdateRequest, IAssetService>
{
    public AssetsController(IAssetService assetService) : base(assetService)
    {
    }
}
