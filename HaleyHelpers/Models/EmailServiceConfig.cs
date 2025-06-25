namespace Haley.Models {
    public class EmailServiceConfig {
        [OtherNames("host")]
        public string Host { get; internal set; }
        [OtherNames("port")]
        public int Port { get; internal set; }
        [OtherNames("user")]
        public string User { get; internal set; }
        [OtherNames("password")]
        public string Password { get; internal set; }
        [OtherNames("default-sender")]
        public string DefaultSender { get; set; }
        public EmailServiceConfig(string host, int port) {
            Host = host;
            Port = port;
        }
        public EmailServiceConfig() { }
    }
}
