using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services
{
    public class CategoryService : BaseCRUDService<Category, CategoryResponse, CategorySearchObject, CategoriesInsertRequest, CategoriesUpdateRequest>, ICategoryService
    {
        // dummy in-memory collection with some hierarchical categories
      
        public CategoryService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<CategoriesInsertRequest> insertValidator, IValidator<CategoriesUpdateRequest> updateValidator) : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Category> ApplyFilters(IEnumerable<Category> query, CategorySearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(c => c.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (search.ParentCategoryId.HasValue)
                {
                    query = query.Where(c => c.ParentCategoryId == search.ParentCategoryId.Value);
                }
            }

            return query;
        }

        public Task<CategoryResponse> ExceptionTestingInsertAsync(CategoriesInsertRequest request)
        {
            if (request.Name.Length < 3)
            {
                throw new ClinetException("Category name must be at least 3 characters long.");
            }

            var entity = MapInsertRequestToEntity(request);

            // Set the Id property
            var entityType = entity.GetType();
            var idProperty = entityType.GetProperty("Id");
      

            // Set CreatedAt if exists
            var createdAtProperty = entityType.GetProperty("CreatedAt");
            if (createdAtProperty?.CanWrite == true)
            {
                createdAtProperty.SetValue(entity, DateTime.UtcNow);
            }

            var dataSource = this._dbContext.Set<Category>();
            dataSource.Add(entity);

            return Task.FromResult(_mapper.Map<CategoryResponse>(entity));
        }
    }
}