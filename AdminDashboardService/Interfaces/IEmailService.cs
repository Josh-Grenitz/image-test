using EmailUtility.Netcore;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AdminDashboardService.Interfaces
{
    public interface IEmailService
    {
        void SendEmail(List<string> to, string subject, string body, List<Attachment> attachments);
        void SendEmail(List<string> to, string subject, string body);
        void SendEmail(List<string> to, List<string> cc, string subject, string body);

    }
}