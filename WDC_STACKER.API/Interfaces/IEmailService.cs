using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Interfaces
{
    /// <summary>
    /// Service interface for sending email notifications for FGI withdrawal requests.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send email when withdrawal request status changes to Partial.
        /// Offset qty = (request total - actual output)
        /// </summary>
        Task<(bool Success, string ErrorMessage)> SendWithdrawalPartialEmailAsync(FgiWithdrawalRequestView request);

        /// <summary>
        /// Send email when withdrawal request status changes to Completed.
        /// </summary>
        Task<(bool Success, string ErrorMessage)> SendWithdrawalCompletedEmailAsync(FgiWithdrawalRequestView request);

        /// <summary>
        /// Send email when withdrawal request status changes to Closed (automatically).
        /// Offset qty = (request total - actual output)
        /// </summary>
        Task<(bool Success, string ErrorMessage)> SendWithdrawalClosedEmailAsync(FgiWithdrawalRequestView request);
    }
}
