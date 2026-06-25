//using Core.Abstractions;
//using FluentValidation;
//using Framework.Endpoints;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Routing;
//using AuthService.Contracts;
//using AuthService.Core.Database;
//using AuthService.Domain.Widgets;

//namespace AuthService.Core.Features.Widgets.UseCases;

//public sealed record CreateWidgetCommand(Guid OwnerId, string Name) : ICommand;

//public sealed class CreateWidgetValidator : AbstractValidator<CreateWidgetCommand>
//{
//    public CreateWidgetValidator()
//    {
//        RuleFor(x => x.OwnerId).NotEmpty();
//        RuleFor(x => x.Name).NotEmpty().MaximumLength(WidgetName.MAX_LENGTH);
//    }
//}

//public sealed class CreateWidgetEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app) =>
//        app.MapPost("/widgets", HandleAsync);

//    private static async Task<EndpointResult<Guid>> HandleAsync(
//        [FromBody] CreateWidgetRequest request,
//        [FromServices] CreateWidgetHandler handler,
//        CancellationToken ct) =>
//        await handler.Handle(new CreateWidgetCommand(Guid.CreateVersion7(), request.Name), ct);
//}

//public sealed class CreateWidgetHandler : ICommandHandler<Guid, CreateWidgetCommand>
//{
//    private readonly IWidgetsRepository _repository;
//    private readonly ITransactionManager _transactionManager;
//    private readonly IValidator<CreateWidgetCommand> _validator;
//    private readonly ILogger<CreateWidgetHandler> _logger;

//    public CreateWidgetHandler(
//        IWidgetsRepository repository,
//        ITransactionManager transactionManager,
//        IValidator<CreateWidgetCommand> validator,
//        ILogger<CreateWidgetHandler> logger)
//    {
//        _repository = repository;
//        _transactionManager = transactionManager;
//        _validator = validator;
//        _logger = logger;
//    }

//    public async Task<Result<Guid, Error>> Handle(CreateWidgetCommand command, CancellationToken cancellationToken)
//    {
//        return Result.Success<Guid, Error>(Guid.CreateVersion7());
//    }
//}
