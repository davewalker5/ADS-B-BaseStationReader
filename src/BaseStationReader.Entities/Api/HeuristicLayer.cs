namespace BaseStationReader.Entities.Api
{
    public enum HeuristicLayer
    {
        None,
        ConfirmedMapping,
        NumberSuffixRule,
        SuffixDeltaRule,
        ConstantPrefix,
        ConstantDelta,
        IdentityMapping
    }
}