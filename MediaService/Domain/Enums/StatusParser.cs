namespace MediaService.Domain.Enums;

public static class StatusParser
{
    public static bool TryParse(string value, out Status status) => Enum.TryParse(value, ignoreCase: true, out status);
}
