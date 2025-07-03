namespace Haley.Models {
    public class EmailData {
        public string[] To { get; set; }
        public string[] CC { get; set; }
        public string[] BCC { get; set; }
        public string[] ReplyTo { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string From { get; set; }
        public bool IsHtml { get; set; } = true;
        public EmailData() {  }
    }
}
