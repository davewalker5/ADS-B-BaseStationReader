using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Api
{
    public interface ILookupEligibilityAssessor
    {
        Task<EligibilityResult> IsEligibleForLookupAsync(ApiEndpointType type, string address);
    }
}