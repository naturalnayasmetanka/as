namespace AuthService.Domain.Widgets;

public sealed record WidgetName
{
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    private WidgetName(string value) => Value = value;

    public static Result<WidgetName, Error> Of(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return AuthServiceErrors.Widget.NameRequired();

        string trimmed = raw.Trim();
        if (trimmed.Length > MAX_LENGTH)
            return AuthServiceErrors.Widget.NameTooLong(MAX_LENGTH);

        return new WidgetName(trimmed);
    }

    public override string ToString() => Value;
}
