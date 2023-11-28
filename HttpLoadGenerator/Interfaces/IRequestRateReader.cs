
namespace HttpLoadGenerator.Interfaces
{
    internal interface IRequestRateReader
    {
        /// <summary>
        /// Gives the next evaluation of requests per second.
        /// </summary>
        /// <returns>The rate of requests per second.</returns>
        public double WaitAndGetNextRps();
    }
}