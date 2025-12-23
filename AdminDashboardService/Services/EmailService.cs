using AdminDashboardService.Interfaces;
using EmailUtility.Netcore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace AdminDashboardService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public EmailService(
        IEmailSender emailSender,
        IConfiguration configuration)
        {
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public void SendEmail(List<string> to, string subject, string body, List<Attachment> attachments)
        {
            string from = _configuration["EmailConfiguration:From"]
                ?? throw new KeyNotFoundException("Could not find key EmailConfiguration:From in the configuration");

            var message = CreateMessage(from, to, subject, body);

            _emailSender.SendEmailWithAttachments(message, attachments);
        }

        public void SendEmail(List<string> to, string subject, string body)
        {
            string from = _configuration["EmailConfiguration:From"]
                ?? throw new KeyNotFoundException("Could not find key EmailConfiguration:From in the configuration");

            var message = CreateMessage(from, to, subject, body);

            _emailSender.SendEmail(message);
        }

        public void SendEmail(List<string> to, List<string> cc, string subject, string body)
        {
            string from = _configuration["EmailConfiguration:From"]
                ?? throw new KeyNotFoundException("Could not find key EmailConfiguration:From in the configuration");

            var message = CreateMessage(from, to, subject, body, cc);

            _emailSender.SendEmailCC(message);
        }

        private Message CreateMessage(string fromAddress, List<string> toAddresses, string subject, string body, List<string>? ccAddresses = null, string? priority = null, string? title = null, DateTime? dataDate = null)
        {
            var message = new Message()
            {
                To = toAddresses,
                From = fromAddress,
                Subject = subject,
                Content = body
            };

            if (ccAddresses != null && ccAddresses.Count > 0)
                message.Cc = ccAddresses;

            if (!string.IsNullOrEmpty(priority))
                message.Priority = priority;

            if (!string.IsNullOrEmpty(title))
                message.Title = title;

            if (dataDate != null && dataDate != DateTime.MinValue)
                message.dataDate = (DateTime)dataDate;
            else
                message.dataDate = DateTime.Now.Date;

           return message;
        }

       
    }
}
