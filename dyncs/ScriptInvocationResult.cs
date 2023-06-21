namespace dyncs
{
    public record ScriptInvocationResult
    {
        public bool Error { get; set; }
        public string? Message { get; set; }
    }
}