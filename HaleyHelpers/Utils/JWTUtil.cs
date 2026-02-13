using Haley.Models;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            return GenerateToken(key.GetBytes(), payload);
        }

        public static TokenValidationParameters GenerateTokenValidationParams(JWTParameters jwtparams) {
            if (jwtparams == null) return null;
            return new TokenValidationParameters() {
                ValidateIssuerSigningKey = true, //Important as this will verfiy the signature
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5), //Sometimes, the token would be generated with nbf (not before) value which is set in future.
                RequireExpirationTime = true,
                ValidateIssuer = jwtparams.ValidateIssuer,
                ValidateAudience = jwtparams.ValidateAudience,
                ValidIssuer = jwtparams.Issuer,
                ValidAudience = jwtparams.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(jwtparams.GetSecret()),

                // Prevent fallback to metadata or key resolution
                RequireSignedTokens = true,
                SignatureValidator = null,
                IssuerSigningKeyResolver = null,
                ValidateActor = false,
                ValidateTokenReplay = false
            };
        }

        public static ClaimsPrincipal ValidateToken(string token, JWTParameters jwtParams, out SecurityToken validatedToken, string authenticationType = null) => ValidateToken(token, GenerateTokenValidationParams(jwtParams), out validatedToken, authenticationType);

        public static ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParams, out SecurityToken validatedToken, string authenticationType = null) {
            var handler = new JwtSecurityTokenHandler();
            try {
                var principal = handler.ValidateToken(token, validationParams, out validatedToken);

                // If authenticationType is specified, recreate the identity with it
                if (!string.IsNullOrEmpty(authenticationType) && principal?.Identity is ClaimsIdentity oldIdentity) {
                    var newIdentity = new ClaimsIdentity(oldIdentity.Claims, authenticationType);
                    return new ClaimsPrincipal(newIdentity);
                }

                return principal;
            } catch (SecurityTokenException ex) {
                Console.WriteLine($"Token validation failed: {ex.Message}");
                validatedToken = null;
                return null;
            } catch (Exception ex) {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                validatedToken = null;
                return null;
            }
        }
    }
}