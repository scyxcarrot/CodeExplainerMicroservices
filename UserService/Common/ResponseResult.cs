namespace UserService.Common
{
    public record ResponseResult(bool Success, string Message)
    {
        public ResponseResult(bool Success, IEnumerable<string> Messages)
            : this(true, string.Join("; \n", Messages))
        {
        }
    }
}
