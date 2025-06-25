using System.Net.Mail;
using System.Text;
using Haley.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Haley.Abstractions;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace Haley.Services {
    public class MailService  {
        public bool IsInitialized { get; private set; }
        //REGEX IS NOT THREAD SAFE. SO USE ONLY THE STRING.
        string emailPattern = @"^[\w\-_.]+@([\w\-_]+\.)+[\w\-_]{2,4}$";
        EmailServiceConfig _param;
        ILogger<MailService> _logger;

        public MailService():this(null) { }
        public MailService(ILogger<MailService> logger) {
            _logger = logger;
        }
        public void Initialize(EmailServiceConfig config) {
            if (config == null || string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.User)) {
                var message = "Invalid SMTP Configuration";
                _logger?.LogError(message);
                throw new ArgumentException(message);
            }
            _param = config;
            IsInitialized = true;
        }

        void Sanitize(EmailData data) {
            //remove all the null values.
            data.CC = data.CC?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? new string[0];
            data.To = data.To?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? new string[0];
            data.ReplyTo = data.ReplyTo?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? new string[0];
            data.From = data.From ?? _param.DefaultSender;
            data.Subject = data.Subject ?? "(No Subject)";
            data.Body = data.Body ?? "(No Body)";
        }

        public async Task SendEmailAsync(EmailData data) {
            try {
                if (!IsInitialized) throw new InvalidOperationException("EmailService not initialized");
                if (_param == null) throw new ArgumentException("SMTP details are missing");
                if (data == null) return; //nothing to send.

                Sanitize(data);

                //Prepare the message first.
                using (var message = new MailMessage()) {
                    message.From = GetValidAddress(data.From, _param.DefaultSender);
                    AddAddresses(message.To, data.To);
                    AddAddresses(message.CC, data.CC);
                    AddAddresses(message.ReplyToList, data.ReplyTo);

                    message.Subject = data.Subject;
                    message.SubjectEncoding = System.Text.Encoding.UTF8;
                    message.Body = data.Body;
                    message.BodyEncoding = System.Text.Encoding.UTF8;
                    message.IsBodyHtml = data.IsHtml;

                    using (SmtpClient smtpCli = new SmtpClient(_param.Host)) { //Don't store this client as new client is needed for each method call as it can be called in parallel by different services.

                        // set smtp-client with basicAuthentication
                        smtpCli.UseDefaultCredentials = false;
                        smtpCli.Port = _param.Port;
                        smtpCli.Credentials = new System.Net.NetworkCredential(_param.User, _param.Password);
                        await smtpCli.SendMailAsync(message);
                    }
                }
                
                //After sending, store it in the db.
            } catch (Exception ex) {
                _logger?.LogError(ex, "Error during mail send");
                throw ex;
            }
        }

        private MailAddress GetValidAddress(string input, string fallback) {
            var emailRegex = new Regex(emailPattern);
            return emailRegex.IsMatch(input ?? string.Empty)
                ? new MailAddress(input)
                : new MailAddress(fallback);
        }

        private void AddAddresses(MailAddressCollection collection, IEnumerable<string> addresses) {
            var emailRegex = new Regex(emailPattern);
            foreach (var email in addresses ?? Enumerable.Empty<string>()) {
                if (emailRegex.IsMatch(email))
                    collection.Add(new MailAddress(email));
            }
        }
    }
}
