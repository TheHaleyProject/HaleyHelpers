using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Identity.Client;
using Haley.Models;
using System.Text;
using System;
using System.Net;

namespace Haley.Utils {

    public class JWTUtil {

        public static string GenerateToken(JwtPayload payload, out string signingkey) {
            var key = HashUtils.GetRandomBytes(256);
            signingkey = Encoding.UTF8.GetString(key.bytes);
            return GenerateToken(key.bytes,payload);
        }
        public static string GenerateToken(byte[] key, JwtPayload payload) {
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }

        public static string GenerateToken(string key,JwtPayload payload) {
            var _secret = key;
            if (key.IsBase64()) {
                return GenerateToken(Convert.FromBase64String(key), payload);
            }
            return GenerateToken(Encoding.UTF8.GetBytes(_secret), payload);
        }

        public static TokenValidationParameters GenerateTokenValidationParams(JWTParameters jwtparams) {
            return new TokenValidationParameters() {
                ValidateIssuerSigningKey = true, //Important as this will verfiy the signature
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
                ValidateIssuer = jwtparams.ValidateIssuer,
                ValidateAudience = jwtparams.ValidateAudience,
                ValidIssuer = jwtparams.Issuer,
                ValidAudience = jwtparams.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(jwtparams.GetSecret())
            };
        }
    }
}