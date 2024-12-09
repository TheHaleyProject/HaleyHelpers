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
            return Secret?.GetBytes();
        }
        public JWTParameters() { }
    }
}
