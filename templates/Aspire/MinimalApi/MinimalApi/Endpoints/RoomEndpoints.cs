using System.Security.Claims;

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

        rooms.MapGet("/", async (IRoomService roomService) =>
        {
            var result = await roomService.GetAllRoomsAsync();
            return TypedResults.Ok(result.Select(r => (RoomDto?)r));
        });

        rooms.MapGet("/my", async (ClaimsPrincipal user, IRoomService roomService, UserManager<ApplicationUser> userManager) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await roomService.GetRoomsByUserIdAsync(userId);
            return Results.Ok(result.Select(r => (RoomDto?)r));
        }).RequireAuthorization();

        rooms.MapGet("/{id:guid}", async (Guid id, IRoomService roomService) =>
        {
            var room = await roomService.GetRoomByIdAsync(id);
            return room is null ? Results.NotFound() : Results.Ok((RoomDto?)room);
        });

        rooms.MapGet("/name/{friendlyName}", async (string friendlyName, IRoomService roomService) =>
        {
            var room = await roomService.GetRoomByFriendlyNameAsync(friendlyName);
            return room is null ? Results.NotFound() : Results.Ok((RoomDto?)room);
        });

        rooms.MapPost("/", async (CreateRoomRequest request, ClaimsPrincipal user, IRoomService roomService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var room = await roomService.CreateRoomAsync(request.FriendlyName, userId, cancellationToken);
            return Results.Created($"/api/rooms/{room.Id}", (RoomDto?)room);
        }).RequireAuthorization();

        rooms.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, IRoomService roomService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await roomService.DeleteRoomAsync(id, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        var questions = rooms.MapGroup("/{roomId:guid}/questions").WithTags("Questions");

        questions.MapGet("/", async (Guid roomId, IQuestionService questionService) =>
            TypedResults.Ok((await questionService.GetQuestionsByRoomIdAsync(roomId)).Select(q => (QuestionDto)q)));

        questions.MapGet("/approved", async (Guid roomId, IQuestionService questionService) =>
            TypedResults.Ok((await questionService.GetApprovedQuestionsByRoomIdAsync(roomId)).Select(q => (QuestionDto)q)));

        questions.MapPost("/", async (Guid roomId, CreateQuestionRequest request, HttpRequest httpRequest, IQuestionService questionService, CancellationToken cancellationToken) =>
        {
            var clientId = httpRequest.Headers["X-Client-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            if (!await questionService.CanSubmitQuestionAsync(clientId))
                return Results.Problem("Rate limit exceeded. Please wait before submitting another question.", statusCode: 429);

            var question = await questionService.SubmitQuestionAsync(roomId, request.QuestionText, request.AuthorName, cancellationToken);
            return Results.Created($"/api/rooms/{roomId}/questions", (QuestionDto)question);
        });

        questions.MapPut("/{questionId:guid}/approve", async (Guid roomId, Guid questionId, ClaimsPrincipal user, IQuestionService questionService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            // Room scoping and authorization are enforced by the service layer for question mutations.
            await questionService.ApproveQuestionAsync(questionId, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        questions.MapPut("/{questionId:guid}/answer", async (Guid roomId, Guid questionId, ClaimsPrincipal user, IQuestionService questionService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            // Room scoping and authorization are enforced by the service layer for question mutations.
            await questionService.MarkAsAnsweredAsync(questionId, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        questions.MapDelete("/{questionId:guid}", async (Guid roomId, Guid questionId, ClaimsPrincipal user, IQuestionService questionService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            // Room scoping and authorization are enforced by the service layer for question mutations.
            await questionService.DeleteQuestionAsync(questionId, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        rooms.MapPut("/{roomId:guid}/current-question/{questionId:guid?}", async (Guid roomId, Guid? questionId, ClaimsPrincipal user, IRoomService roomService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await roomService.SetCurrentQuestionAsync(roomId, questionId, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        rooms.MapDelete("/{roomId:guid}/current-question", async (Guid roomId, ClaimsPrincipal user, IRoomService roomService, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken) =>
        {
            var userId = userManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            await roomService.SetCurrentQuestionAsync(roomId, null, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}

public record CreateRoomRequest(string FriendlyName);
public record CreateQuestionRequest(string QuestionText, string AuthorName);
