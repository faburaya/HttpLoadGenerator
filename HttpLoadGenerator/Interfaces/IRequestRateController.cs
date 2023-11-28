using System.Threading.Tasks;

namespace HttpLoadGenerator.Interfaces
{
    internal interface IRequestRateController : IRequestRateReader
    {
        /// <summary>
        /// A ticket must be taken before sending a request to the API under test.
        /// This ensures that the count of requests does not exceeds the target rate.
        /// </summary>
        /// <returns>Whether a ticket is available within the current time window.</returns>
        bool TakeTicketToSendRequest();
    }
}