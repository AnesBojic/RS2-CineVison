using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
{
    /// <summary>
    /// Read-only on purpose: role names drive authorization, so they are seeded with the
    /// schema rather than edited at runtime. Clients read them to fill the role picker.
    /// </summary>
    public interface IRoleService : IBaseReadService<RoleResponse, LookupSearchObject>
    {
    }
}
