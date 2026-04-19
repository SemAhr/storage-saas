using System.ComponentModel.DataAnnotations;

namespace MediaService.Shared.Validations;

public sealed class GuidAttribute : ValidationAttribute
{
    public GuidAttribute()
        : base("{0} must be a valid UUID.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return false;

        if (value is Guid guid)
            return guid != Guid.Empty;

        if (value is string text)
            return Guid.TryParse(text, out var parsedGuid) && parsedGuid != Guid.Empty;

        return false;
    }
}
