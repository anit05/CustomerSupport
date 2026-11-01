namespace CustomerSupport.Mvc.Models.Api
{
    public sealed record ApiResult<T>(bool Ok, T? Data, string Message, int StatusCode = 200)
    {
        public static ApiResult<T> Failure(string message, int status = 500)
        {
            return new(false, default, message, status);
        }
    }
}
