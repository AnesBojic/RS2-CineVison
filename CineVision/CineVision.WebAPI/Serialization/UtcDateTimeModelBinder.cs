using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CineVision.WebAPI.Serialization
{
    /// <summary>UTC binder for query/route DateTime values (MVC, not System.Text.Json).</summary>
    public class UtcDateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.Metadata.UnderlyingOrModelType == typeof(DateTime)
                ? new UtcDateTimeModelBinder()
                : null;
        }
    }

    public class UtcDateTimeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

            var raw = valueResult.FirstValue;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return Task.CompletedTask;
            }

            if (!DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"'{raw}' is not a valid ISO 8601 date-time value.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(parsed.UtcDateTime);
            return Task.CompletedTask;
        }
    }
}
