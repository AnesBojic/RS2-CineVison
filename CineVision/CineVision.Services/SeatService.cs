using System.Collections.Generic;
using System.Linq;
using CineVision.Model.Requests;
using CineVision.Model.Responses;
using CineVision.Model.SearchObjects;
using CineVision.Services.Database;
using FluentValidation;
using CineVision.Model.Enums;

namespace CineVision.Services
{
    public class SeatService : BaseCRUDService<Seat, SeatResponse, SeatSearchObject, SeatInsertRequest, SeatUpdateRequest>, ISeatService
    {
        public SeatService(CineVisionDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<SeatInsertRequest> insertValidator, IValidator<SeatUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IQueryable<Seat> ApplyFilters(IQueryable<Seat> query, SeatSearchObject? search)
        {
            if (search != null)
            {
                if (search.HallId.HasValue)
                {
                    query = query.Where(s => s.HallId == search.HallId.Value);
                }

                if (search.SeatType.HasValue)
                {
                    query = query.Where(s => (int)s.SeatType == search.SeatType.Value);
                }
            }

            return query;
        }
    }
}
