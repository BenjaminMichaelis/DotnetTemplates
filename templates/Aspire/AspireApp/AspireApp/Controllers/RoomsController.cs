using AspireApp.Core.Hubs;
using AspireApp.Core.QA;
using AspireApp.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(
    IRoomService roomService,
    IQuestionService questionService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet(Name = "GetAllRooms")]
    [ProducesResponseType<IEnumerable<RoomDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAllRooms()
    {
        var rooms = await roomService.GetAllRoomsAsync();
        return Ok(rooms.Select(r => (RoomDto?)r));
    }

    [HttpGet("my", Name = "GetMyRooms")]
    [Authorize]
    [ProducesResponseType<IEnumerable<RoomDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetMyRooms()
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var rooms = await roomService.GetRoomsByUserIdAsync(userId);
        return Ok(rooms.Select(r => (RoomDto?)r));
    }

    [HttpGet("{id:guid}", Name = "GetRoom")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetRoom(Guid id)
    {
        var room = await roomService.GetRoomByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return Ok((RoomDto?)room);
    }

    [HttpGet("name/{friendlyName}", Name = "GetRoomByName")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetRoomByName(string friendlyName)
    {
        var room = await roomService.GetRoomByFriendlyNameAsync(friendlyName);
        if (room == null)
        {
            return NotFound();
        }

        return Ok((RoomDto?)room);
    }

    [HttpPost(Name = "CreateRoom")]
    [Authorize]
    [ProducesResponseType<RoomDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var room = await roomService.CreateRoomAsync(request.FriendlyName, userId, cancellationToken);
        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, (RoomDto?)room);
    }

    [HttpDelete("{id:guid}", Name = "DeleteRoom")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.DeleteRoomAsync(id, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{roomId:guid}/questions", Name = "GetQuestions")]
    [ProducesResponseType<IEnumerable<QuestionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions(Guid roomId)
    {
        var questions = await questionService.GetQuestionsByRoomIdAsync(roomId);
        return Ok(questions.Select(q => (QuestionDto)q));
    }

    [HttpGet("{roomId:guid}/questions/approved", Name = "GetApprovedQuestions")]
    [ProducesResponseType<IEnumerable<QuestionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetApprovedQuestions(Guid roomId)
    {
        var questions = await questionService.GetApprovedQuestionsByRoomIdAsync(roomId);
        return Ok(questions.Select(q => (QuestionDto)q));
    }

    [HttpPost("{roomId:guid}/questions", Name = "CreateQuestion")]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(Guid roomId, [FromBody] CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        // Get client ID from header or generate one
        var clientId = Request.Headers["X-Client-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        // Check rate limit
        if (!await questionService.CanSubmitQuestionAsync(clientId))
        {
            return StatusCode(429, "Rate limit exceeded. Please wait before submitting another question.");
        }

        var question = await questionService.SubmitQuestionAsync(
            roomId,
            request.QuestionText,
            request.AuthorName,
            cancellationToken);

        return CreatedAtAction(nameof(GetQuestions), new { roomId }, (QuestionDto)question);
    }

    [HttpPut("{roomId:guid}/questions/{questionId:guid}/approve", Name = "ApproveQuestion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveQuestion(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.ApproveQuestionAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{roomId:guid}/questions/{questionId:guid}/answer", Name = "MarkQuestionAnswered")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkQuestionAnswered(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.MarkAsAnsweredAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}/questions/{questionId:guid}", Name = "DeleteQuestion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteQuestion(Guid roomId, Guid questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await questionService.DeleteQuestionAsync(questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{roomId:guid}/current-question/{questionId:guid?}", Name = "SetCurrentQuestion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetCurrentQuestion(Guid roomId, Guid? questionId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, questionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}/current-question", Name = "ClearCurrentQuestion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCurrentQuestion(Guid roomId, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await roomService.SetCurrentQuestionAsync(roomId, null, userId, cancellationToken);
        return NoContent();
    }
}

public record CreateRoomRequest(string FriendlyName);
public record CreateQuestionRequest(string QuestionText, string AuthorName);
