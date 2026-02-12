using API.DTOs.Mail;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MailController : ControllerBase
{
    private readonly IMailService _mailService;
    private readonly ILogger<MailController> _logger;

    public MailController(IMailService mailService, ILogger<MailController> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    /// <summary>
    /// Send en testmail (kun Admin).
    /// </summary>
    [HttpPost("demo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SendDemoMail([FromBody] SendDemoMailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _mailService.SendTestEmailAsync(
                request.ToEmail,
                request.Subject,
                request.Body,
                request.Template,
                request.TemplateUsername,
                cancellationToken: cancellationToken);
            return Ok(new { message = "Testmail sendt.", toEmail = request.ToEmail });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
