using Application.Abstractions.CQRS;
using SharedKernel;

namespace Projects.Application.Projects.AddMember;

public sealed record AddMemberCommand(Guid ProjectId, Guid ActorId, Guid UserId, string Role)
    : ICommand<Result>;