using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.Services
{
    public interface IAssetService : IBaseCRUDService<AssetResponse, AssetSearch, AssetInsertRequest, AssetUpdateRequest>
    {
    }
}
