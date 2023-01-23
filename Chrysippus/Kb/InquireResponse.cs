namespace Chrysippus.Kb;

public enum InquireResponse : byte
{
    False = 0,
    True = 1,
    Unknown = 2
}

/// <summary>
/// Conversion from internal bool? to readable response
/// </summary>
internal class InquireResponseResult
{
    private readonly bool? _internalState;

    private InquireResponseResult(bool? internalState)
    {
        _internalState = internalState;
    }

    public static implicit operator bool?(InquireResponseResult inquireResponse) => inquireResponse._internalState;
    public static implicit operator InquireResponseResult(bool? result) => new InquireResponseResult(result);

    public InquireResponse Response
    {
        get
        {
            if (!_internalState.HasValue)
            {
                return InquireResponse.Unknown;
            }

            return _internalState.Value ? InquireResponse.True : InquireResponse.False;
        }
    }
}