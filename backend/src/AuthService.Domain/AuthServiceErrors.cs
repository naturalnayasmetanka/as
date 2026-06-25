namespace AuthService.Domain;

public static class AuthServiceErrors
{
    public static class Widget
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("widget.not.found", $"Widget {id} не найден.");

        public static Error NameTooLong(int max) =>
            Error.Validation("widget.name.too.long", $"Название не может быть длиннее {max} символов.");

        public static Error NameRequired() =>
            Error.Validation("widget.name.required", "Название обязательно.");
    }
}
