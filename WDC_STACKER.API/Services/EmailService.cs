using System.Net.Mail;
using WDC_STACKER.API.Interfaces;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalPartialEmailAsync(FgiWithdrawalRequestView request)
        {
            int offsetQty = (request.Total ?? 0) - (request.ActualOutput ?? 0);
            string subject = $"Withdrawal Request Partially Fulfilled - Request #{request.RequestId} (Offset Qty: {offsetQty})";
            string intro = $"<p>The withdrawal request has been partially fulfilled.</p>";

            return await SendAsync(request, subject, intro, offsetQty);
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalCompletedEmailAsync(FgiWithdrawalRequestView request)
        {
            string subject = $"Withdrawal Request Completed - Request #{request.RequestId}";
            string intro = $"<p>The withdrawal request has been completed.</p>";

            return await SendAsync(request, subject, intro, null);
        }

        public async Task<(bool Success, string ErrorMessage)> SendWithdrawalClosedEmailAsync(FgiWithdrawalRequestView request)
        {
            int offsetQty = (request.Total ?? 0) - (request.ActualOutput ?? 0);
            string subject = $"Withdrawal Request Closed - Request #{request.RequestId} (Offset Qty: {offsetQty})";
            string intro = $"<p>The withdrawal request has been closed automatically.</p>";

            return await SendAsync(request, subject, intro, offsetQty);
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
                string body = BuildEmailBody(request, subject, introHtml, offsetQty, linkSection, footer, copyRight);

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

        private string BuildEmailBody(
            FgiWithdrawalRequestView request, 
            string subject, 
            string introHtml, 
            int? offsetQty, 
            string linkSection, 
            string footer, 
            string copyRight)
        {
            string offsetQtySection = offsetQty.HasValue ? 
                $"<p><strong>Offset Qty:</strong> {offsetQty.Value}</p>" : "";

            string lecSection = !string.IsNullOrEmpty(request.Lec) ? 
                $"<td>{request.Lec}</td>" : "<td>-</td>";

            string penNumSection = !string.IsNullOrEmpty(request.PenNum) ? 
                $"<td>{request.PenNum}</td>" : "<td>-</td>";

            return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>{subject}</h2>
    {introHtml}
    
    <h3>Request Details</h3>
    <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>
        <tr style='background-color: #f2f2f2;'>
            <th style='text-align: left;'>Request ID</th>
            <th style='text-align: left;'>Grade</th>
            <th style='text-align: left;'>Part Number</th>
            <th style='text-align: left;'>Qty</th>
            <th style='text-align: left;'>LEC</th>
            <th style='text-align: left;'>Pen Num</th>
        </tr>
        <tr>
            <td>{request.RequestId}</td>
            <td>{request.Grade}</td>
            <td>{request.SliderPartNumber}</td>
            <td>{request.Total ?? 0}</td>
            {lecSection}
            {penNumSection}
        </tr>
    </table>

    {offsetQtySection}

    <h3>Additional Details</h3>
    <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>
        <tr style='background-color: #f2f2f2;'>
            <th style='text-align: left;'>Field</th>
            <th style='text-align: left;'>Value</th>
        </tr>
        <tr>
            <td>Date</td>
            <td>{request.Date?.ToString("yyyy-MM-dd HH:mm") ?? "-"}</td>
        </tr>
        <tr>
            <td>Requestor</td>
            <td>{request.Requestor}</td>
        </tr>
        <tr>
            <td>Shift</td>
            <td>{request.Shift}</td>
        </tr>
        <tr>
            <td>Model</td>
            <td>{request.Model}</td>
        </tr>
        <tr>
            <td>Category</td>
            <td>{request.Category}</td>
        </tr>
        <tr>
            <td>Head Type</td>
            <td>{request.HeadType}</td>
        </tr>
        <tr>
            <td>Actual Output</td>
            <td>{request.ActualOutput ?? 0}</td>
        </tr>
        <tr>
            <td>Status</td>
            <td>{request.Status}</td>
        </tr>
        <tr>
            <td>Acknowledge By</td>
            <td>{request.AcknowledgeBy}</td>
        </tr>
        <tr>
            <td>Remarks</td>
            <td>{request.Remarks}</td>
        </tr>
    </table>

    {linkSection}
    <p>{footer}</p>
    <p>{copyRight}</p>
</body>
</html>";
        }
    }
}
