using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services
{
    public class SeatService : BaseCRUDService<Seat, SeatResponse, SeatSearchObject, SeatInsertRequest, SeatUpdateRequest>, ISeatService
    {
        public SeatService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<SeatInsertRequest> insertValidator, IValidator<SeatUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Seat> ApplyFilters(IEnumerable<Seat> query, SeatSearchObject? search)
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
