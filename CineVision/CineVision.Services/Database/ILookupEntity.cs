using System;

namespace CineVision.Services.Database
{
    /// <summary>
    /// Shared shape of the simple reference (lookup) tables: screen types, hall statuses,
    /// age ratings and languages. Lets one generic service cover all of them.
    /// </summary>
    public interface ILookupEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
