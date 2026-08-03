using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;

namespace CineVision.Services
{
    public interface ILanguageService : IBaseCRUDService<LanguageResponse, LookupSearchObject, LanguageInsertRequest, LanguageUpdateRequest>
    {
    }
}
