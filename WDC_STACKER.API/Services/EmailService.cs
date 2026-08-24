using System.Net.Mail;
using WDC_STACKER.API.Interfaces;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ActiveDirectoryService _activeDirectoryService;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, ActiveDirectoryService activeDirectoryService)
        {
            _configuration = configuration;
            _logger = logger;
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalPartialEmailAsync(FgiWithdrawalRequestView request)
        {
            int offsetQty = (request.Total ?? 0) - (request.ActualOutput ?? 0);
            string subject = "Withdrawal Request Partially Fulfilled";
            string requestorName = await _activeDirectoryService.GetDisplayNameAsync(request.Requestor);
            string intro = $"<p>The Withdrawal Request by <strong>{requestorName}</strong> has been <strong>Partially Fulfilled</strong> at {DateTime.Now:yyyy-MM-dd HH:mm}.</p>";

            return await SendAsync(request, subject, intro, offsetQty);
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalCompletedEmailAsync(FgiWithdrawalRequestView request)
        {
            string subject = "Withdrawal Request Completed";
            string requestorName = await _activeDirectoryService.GetDisplayNameAsync(request.Requestor);
            string intro = $"<p>The Withdrawal Request by <strong>{requestorName}</strong> has been <strong>Completed</strong> at {DateTime.Now:yyyy-MM-dd HH:mm}.</p>";

            return await SendAsync(request, subject, intro, null);
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalClosedEmailAsync(FgiWithdrawalRequestView request)
        {
            int offsetQty = (request.Total ?? 0) - (request.ActualOutput ?? 0);
            string subject = "Withdrawal Request Closed";
            string requestorName = await _activeDirectoryService.GetDisplayNameAsync(request.Requestor);
            string intro = $"<p>The Withdrawal Request by <strong>{requestorName}</strong> has been automatically <strong>Closed</strong> at {DateTime.Now:yyyy-MM-dd HH:mm}.</p>";

            return await SendAsync(request, subject, intro, offsetQty);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Identifier line: Grade, PartNum, LEC (if present), PenNum (if present)
        // ─────────────────────────────────────────────────────────────────────
        private static string GetIdentifierLine(FgiWithdrawalRequestView request)
        {
            var idParts = new List<string> { $"Grade: {request.Grade}", $"PartNum: {request.SliderPartNumber}" };
            if (!string.IsNullOrWhiteSpace(request.Lec)) idParts.Add($"LEC: {request.Lec}");
            if (!string.IsNullOrWhiteSpace(request.PenNum)) idParts.Add($"PenNum: {request.PenNum}");
            return string.Join(" | ", idParts);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shared SMTP send logic
        // ─────────────────────────────────────────────────────────────────────
        private async Task<(bool Success, string ErrorMessage)> SendAsync(
            FgiWithdrawalRequestView request, string subject, string introHtml, int? offsetQty)
        {
            try
            {
                string smtpHost = _configuration["EmailService:SMTP_HOST"] ?? string.Empty;
                string smtpPortStr = _configuration["EmailService:SMTP_PORT"] ?? string.Empty;
                string mailSender = _configuration["EmailService:MailSender"] ?? string.Empty;
                string smtpUser = _configuration["EmailService:SMTP_User"] ?? string.Empty;
                string smtpPass = _configuration["EmailService:SMTP_Password"] ?? string.Empty;
                string footer = _configuration["EmailService:DoNotReplyWarning"] ?? "This is an automated message. Please do not reply.";
                string copyRight = _configuration["EmailService:CopyRight"] ?? string.Empty;
                string appUrl = _configuration["EmailService:AppUrl"] ?? string.Empty;

                if (string.IsNullOrEmpty(smtpHost))
                    return (false, "SMTP_HOST configuration is missing");
                if (string.IsNullOrEmpty(smtpPortStr))
                    return (false, "SMTP_PORT configuration is missing");
                if (string.IsNullOrEmpty(mailSender))
                    return (false, "MailSender configuration is missing");

                if (!int.TryParse(smtpPortStr, out int smtpPort))
                    return (false, $"Invalid SMTP_PORT: {smtpPortStr}");

                // Get recipients list
                string recipientsConfig = _configuration["EmailService:Recipients"] ?? string.Empty;
                if (string.IsNullOrEmpty(recipientsConfig))
                    return (false, "EmailService:Recipients configuration is missing");

                string linkSection = string.IsNullOrEmpty(appUrl) ? "" :
                    $"<p>Please use the following link to view the request:<br/><a href='{appUrl}'>{appUrl}</a></p>";

                // Build email body with header details and table
                string body = await BuildEmailBody(request, subject, introHtml, offsetQty, linkSection, footer, copyRight);

                MailMessage mailMsg = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                smtp.Host = smtpHost;
                smtp.Port = smtpPort;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;

                if (!string.IsNullOrEmpty(smtpUser))
                {
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                }

                mailMsg.From = new MailAddress(mailSender);
                foreach (var addr in recipientsConfig.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    mailMsg.To.Add(addr);

                mailMsg.Subject = subject;
                mailMsg.Body = body;
                mailMsg.IsBodyHtml = true;

                await Task.Run(() => smtp.Send(mailMsg));
                _logger.LogInformation("Email notification sent: {Subject}", subject);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification");
                return (false, ex.Message);
            }
        }

        private async Task<string> BuildEmailBody(
            FgiWithdrawalRequestView request, 
            string subject, 
            string introHtml, 
            int? offsetQty, 
            string linkSection, 
            string footer, 
            string copyRight)
        {
            string requestCard = await BuildRequestCard(request);

            return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>{subject}</h2>
    {introHtml}

    {requestCard}

    {linkSection}
    <p>{footer}</p>
    <p>{copyRight}</p>
</body>
</html>";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Request card: mirrors FGI_Service layout
        //   top-left: identifiers (subtitle: requestor + request date)
        //   top-right: total, actual output, offset (color-coded)
        //   bottom: model, category, head type, status, acknowledged by, remarks
        // ─────────────────────────────────────────────────────────────────────
        private async Task<string> BuildRequestCard(FgiWithdrawalRequestView request)
        {
            int total = request.Total ?? 0;
            int actualOutput = request.ActualOutput ?? 0;
            int offset = total - actualOutput;
            var offsetColor = offset > 0 ? "#c0392b" : "#27ae60";
            var offsetLabel = offset > 0 ? $"-{offset}" : offset < 0 ? $"+{Math.Abs(offset)}" : "0";

            var idLine = GetIdentifierLine(request);
            var requestorName = await _activeDirectoryService.GetDisplayNameAsync(request.Requestor);
            var acknowledgeByName = string.IsNullOrWhiteSpace(request.AcknowledgeBy)
                ? request.AcknowledgeBy
                : await _activeDirectoryService.GetDisplayNameAsync(request.AcknowledgeBy);

            return $@"
        <div style='border: 1px solid #ddd; border-radius: 6px; padding: 14px; margin-bottom: 16px;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='vertical-align: top; text-align: left;'>
                        <div style='font-size: 16px; font-weight: bold;'>{idLine}</div>
                        <div style='font-size: 13px; color: #666; margin-top: 4px;'>
                            Requestor: {requestorName} &nbsp;|&nbsp; Request Date: {request.Date?.ToString("yyyy-MM-dd HH:mm") ?? "-"}
                        </div>
                    </td>
                    <td style='vertical-align: top; text-align: right; white-space: nowrap;'>
                        <span style='margin-right: 12px;'>Total: <b>{total}</b></span>
                        <span style='margin-right: 12px;'>Actual Output: <b>{actualOutput}</b></span>
                        <span style='color: {offsetColor}; font-weight: bold;'>Offset: {offsetLabel}</span>
                    </td>
                </tr>
            </table>

            <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%; margin-top: 10px;'>
                <tr style='background-color: #f2f2f2;'>
                    <th style='text-align: left;'>Model</th>
                    <th style='text-align: left;'>Category</th>
                    <th style='text-align: left;'>Head Type</th>
                    <th style='text-align: left;'>Status</th>
                    <th style='text-align: left;'>Acknowledged By</th>
                    <th style='text-align: left;'>Remarks</th>
                </tr>
                <tr>
                    <td>{request.Model}</td>
                    <td>{request.Category}</td>
                    <td>{request.HeadType}</td>
                    <td>{request.Status}</td>
                    <td>{acknowledgeByName}</td>
                    <td>{request.Remarks}</td>
                </tr>
            </table>
        </div>";
        }
    }
}
