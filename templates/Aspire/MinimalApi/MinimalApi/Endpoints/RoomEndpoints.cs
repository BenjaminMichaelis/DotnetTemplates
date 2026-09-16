using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

using MinimalApi.Core.Hubs;
using MinimalApi.Core.QA;
using MinimalApi.Data;

namespace MinimalApi.Endpoints;

internal static class RoomEndpoints
{
    internal static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var rooms = app.MapGroup("/api/rooms").WithTags("Rooms");

        rooms.MapGet("/", GetAllRoomsAsync).WithName("GetAllRooms");
        rooms.MapGet("/my", GetMyRoomsAsync).WithName("GetMyRooms").RequireAuthorization();
        rooms.MapGet("/{id:guid}", GetRoomByIdAsync).WithName("GetRoomById");
        rooms.MapGet("/name/{friendlyName}", GetRoomByFriendlyNameAsync).WithName("GetRoomByFriendlyName");
        rooms.MapPost("/", CreateRoomAsync).WithName("CreateRoom").RequireAuthorization();
        rooms.MapDelete("/{id:guid}", DeleteRoomAsync).WithName("DeleteRoom").RequireAuthorization();

        var questions = rooms.MapGroup("/{roomId:guid}/questions").WithTags("Questions");

        questions.MapGet("/", GetQuestionsAsync).WithName("GetQuestions");
        questions.MapGet("/approved", GetApprovedQuestionsAsync).WithName("GetApprovedQuestions");
        questions.MapPost("/", CreateQuestionAsync).WithName("CreateQuestion");
        questions.MapPut("/{questionId:guid}/approve", ApproveQuestionAsync).WithName("ApproveQuestion").RequireAuthorization();
        questions.MapPut("/{questionId:guid}/answer", AnswerQuestionAsync).WithName("AnswerQuestion").RequireAuthorization();
        questions.MapDelete("/{questionId:guid}", DeleteQuestionAsync).WithName("DeleteQuestion").RequireAuthorization();

        rooms.MapPut("/{roomId:guid}/current-question/{questionId:guid?}", SetCurrentQuestionAsync).WithName("SetCurrentQuestion").RequireAuthorization();
        rooms.MapDelete("/{roomId:guid}/current-question", ClearCurrentQuestionAsync).WithName("ClearCurrentQuestion").RequireAuthorization();

        return app;
    }

    private static async Task<Ok<RoomDto[]>> GetAllRoomsAsync(IRoomService roomService)
    {
        var result = await roomService.GetAllRoomsAsync();
        return TypedResults.Ok(result.Select(ToRoomDto).ToArray());
    }

    private static async Task<Results<Ok<RoomDto[]>, UnauthorizedHttpResult>> GetMyRoomsAsync(
        ClaimsPrincipal user,
        IRoomService roomService,
        UserManager<ApplicationUser> userManager)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await roomService.GetRoomsByUserIdAsync(userId);
        return TypedResults.Ok(result.Select(ToRoomDto).ToArray());
    }

    private static async Task<Results<Ok<RoomDto>, NotFound>> GetRoomByIdAsync(Guid id, IRoomService roomService)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        if (room is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToRoomDto(room));
    }

    private static async Task<Results<Ok<RoomDto>, NotFound>> GetRoomByFriendlyNameAsync(
        string friendlyName,
        IRoomService roomService)
    {
        var room = await roomService.GetRoomByFriendlyNameAsync(friendlyName);
        if (room is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToRoomDto(room));
    }

    private static async Task<Results<Created<RoomDto>, UnauthorizedHttpResult>> CreateRoomAsync(
        CreateRoomRequest request,
        ClaimsPrincipal user,
        IRoomService roomService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        var room = await roomService.CreateRoomAsync(request.FriendlyName, userId, cancellationToken);
        return TypedResults.Created($"/api/rooms/{room.Id}", ToRoomDto(room));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> DeleteRoomAsync(
        Guid id,
        ClaimsPrincipal user,
        IRoomService roomService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        await roomService.DeleteRoomAsync(id, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<QuestionDto[]>> GetQuestionsAsync(Guid roomId, IQuestionService questionService)
    {
        var questions = await questionService.GetQuestionsByRoomIdAsync(roomId);
        return TypedResults.Ok(questions.Select(ToQuestionDto).ToArray());
    }

    private static async Task<Ok<QuestionDto[]>> GetApprovedQuestionsAsync(Guid roomId, IQuestionService questionService)
    {
        var questions = await questionService.GetApprovedQuestionsByRoomIdAsync(roomId);
        return TypedResults.Ok(questions.Select(ToQuestionDto).ToArray());
    }

    private static async Task<Results<Created<QuestionDto>, ProblemHttpResult>> CreateQuestionAsync(
        Guid roomId,
        CreateQuestionRequest request,
        HttpRequest httpRequest,
        IQuestionService questionService,
        CancellationToken cancellationToken)
    {
        var clientId = httpRequest.Headers["X-Client-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        if (!await questionService.CanSubmitQuestionAsync(clientId))
        {
            return TypedResults.Problem(
                detail: "Rate limit exceeded. Please wait before submitting another question.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var question = await questionService.SubmitQuestionAsync(roomId, request.QuestionText, request.AuthorName, cancellationToken);
        return TypedResults.Created($"/api/rooms/{roomId}/questions", ToQuestionDto(question));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> ApproveQuestionAsync(
        Guid roomId,
        Guid questionId,
        ClaimsPrincipal user,
        IQuestionService questionService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        // Room scoping and authorization are enforced by the service layer for question mutations.
        await questionService.ApproveQuestionAsync(questionId, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> AnswerQuestionAsync(
        Guid roomId,
        Guid questionId,
        ClaimsPrincipal user,
        IQuestionService questionService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        // Room scoping and authorization are enforced by the service layer for question mutations.
        await questionService.MarkAsAnsweredAsync(questionId, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> DeleteQuestionAsync(
        Guid roomId,
        Guid questionId,
        ClaimsPrincipal user,
        IQuestionService questionService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        // Room scoping and authorization are enforced by the service layer for question mutations.
        await questionService.DeleteQuestionAsync(questionId, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> SetCurrentQuestionAsync(
        Guid roomId,
        Guid? questionId,
        ClaimsPrincipal user,
        IRoomService roomService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, questionId, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> ClearCurrentQuestionAsync(
        Guid roomId,
        ClaimsPrincipal user,
        IRoomService roomService,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return TypedResults.Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, null, userId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static RoomDto ToRoomDto(Room room) =>
        (RoomDto?)room ?? throw new InvalidOperationException("Room conversion returned null.");

    private static QuestionDto ToQuestionDto(Question question) =>
        (QuestionDto?)question ?? throw new InvalidOperationException("Question conversion returned null.");
}

public record CreateRoomRequest(string FriendlyName);
public record CreateQuestionRequest(string QuestionText, string AuthorName);
