namespace CodeExplainerCommon.Responses
{
    public record ResponseResult(bool Success, string Message)
    {
        public ResponseResult(bool Success, IEnumerable<string> Messages)
            : this(Success, string.Join("; \n", Messages))
        {
        }
    }
}