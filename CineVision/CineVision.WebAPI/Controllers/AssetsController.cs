using CineVision.Services;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineVision.Model.Requests;

namespace CineVision.WebAPI.Controllers;

[Authorize]
public class AssetsController : BaseCRUDController<AssetResponse, AssetSearch, AssetInsertRequest, AssetUpdateRequest, IAssetService>
{
    public AssetsController(IAssetService assetService) : base(assetService)
    {
    }
}
