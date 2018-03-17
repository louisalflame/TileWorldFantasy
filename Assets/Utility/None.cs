namespace Utility
{
    /// <summary>
    /// A coroutine might have parameter of type IResult<None> to indicate that no value but only error may be returned.
    /// In the same manner, IMonad<None> represents the fact that no value will be returned as well.
    /// </summary>
    public struct None
    {
    }
}