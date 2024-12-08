using System.Text;
using System;
using Haley.Utils;

namespace Haley.Models {
    public class JWTParameters {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public bool ValidateIssuer { get; set; } 
        public bool ValidateAudience { get; set; }
        public double ValidMinutes { get; set; } = 10.0;
        public byte[] GetSecret() {
            if (!Secret.IsBase64()) {
                return Encoding.UTF8.GetBytes(Secret);
            }
            var _secret = Encoding.UTF8.GetString(Convert.FromBase64String(Secret));
            return Encoding.UTF8.GetBytes(_secret);
        }
        public JWTParameters() { }
    }
}
