namespace CineVision.Model.SearchObjects;

/// <summary>
/// Shared paging defaults and hard caps for all list endpoints.
/// </summary>
public static class PagingLimits
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Ensures Page/PageSize are set and PageSize never exceeds <see cref="MaxPageSize"/>.
    /// Call before Skip/Take so list endpoints stay bounded even when search is null/empty.
    /// </summary>
    public static void Normalize(BaseSearchObject search)
    {
        if (search.Page is null or < 1)
        {
            search.Page = 1;
        }

        if (search.PageSize is null or < 1)
        {
            search.PageSize = DefaultPageSize;
        }

        if (search.PageSize > MaxPageSize)
        {
            search.PageSize = MaxPageSize;
        }
    }
}
