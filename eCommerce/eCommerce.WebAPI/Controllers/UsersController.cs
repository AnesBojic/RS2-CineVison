using eCommerce.Model;
using eCommerce.Model.Access;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.WebAPI.Services.AccessManager;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class UsersController : BaseCRUDController<UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest, IUserService>
{
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly IValidator<EmailSendRequest> _emailSendValidator;

    public UsersController(
        IUserService userService,
        IEmailService emailService,
        INotificationService notificationService,
        IAuthenticatedUserAccessor userAccessor,
        IValidator<EmailSendRequest> emailSendValidator)
        : base(userService)
    {
        _emailService = emailService;
        _notificationService = notificationService;
        _userAccessor = userAccessor;
        _emailSendValidator = emailSendValidator;
    }

    /// <summary>
    /// Preview of related records that will be removed if this user is deleted.
    /// </summary>
    [HttpGet("{id}/DeleteImpact")]
    [ProducesResponseType(typeof(UserDeleteImpactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDeleteImpactResponse>> GetDeleteImpact(int id)
    {
        var result = await _service.GetDeleteImpactAsync(id);
        return Ok(result);
    }

    /// <summary>Queues an email to the given user's address (asynchronous delivery via RabbitMQ).</summary>
    [HttpPost("{id}/SendEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEmail(int id, [FromBody] EmailSendRequest request)
    {
        await _emailSendValidator.ValidateAndThrowAsync(request);

        var email = await _service.GetEmailByIdAsync(id);

        await _emailService.QueueEmailAsync(new EmailMessage
        {
            To = email,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml
        });

        var senderName = User.FindFirst(ClaimNames.FirstName)?.Value ?? "Admin";
        var preview = request.Body.Length > 180 ? request.Body[..180] + "…" : request.Body;
        await _notificationService.CreateAsync(
            id,
            $"New email: {request.Subject}",
            $"From {senderName}. {preview}",
            NotificationType.Email);

        return Ok();
    }
}
