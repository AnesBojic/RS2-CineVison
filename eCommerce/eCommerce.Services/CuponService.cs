using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace eCommerce.Services
{
    public class CuponService : BaseCRUDService<Cupon, CuponResponse, CuponSearch, CuponInsertRequest, CuponUpdateRequest>, ICuponService
    {
        public CuponService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<CuponInsertRequest> insertValidator, IValidator<CuponUpdateRequest> updateValidator): base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Cupon> ApplyFilters(IEnumerable<Cupon> query, CuponSearch? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Code))
                {
                    query = query.Where(c => c.Code.Contains(search.Code, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }

        public override async Task DeleteAsync(int id)
        {
            if (await _dbContext.Orders.AnyAsync(o => o.CuponId == id))
            {
                throw new InvalidOperationException("Cannot delete coupon because it is used by existing orders.");
            }

            await base.DeleteAsync(id);
        }

        public async Task ToggleActivityAsync(int id)
        {
            var cupon = await _dbContext.Cupons.FindAsync(id);
            if (cupon == null){
                throw new KeyNotFoundException("Cupon not found.");
            }
            cupon.IsActive = !cupon.IsActive;
            cupon.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
       
    }
}
