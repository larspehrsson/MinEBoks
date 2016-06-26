namespace MinEBoks
{
    internal class Session
    {
        public Account Account { get; set; }

        public string Name { get; set; }

        public string InternalUserId { get; set; }

        public string DeviceId { get; set; }

        public string SessionId { get; set; }

        public string Nonce { get; set; }
    }
}