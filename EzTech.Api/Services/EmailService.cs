﻿using System.Net;
using System.Net.Mail;

namespace EzTech.Api.Services;

public interface IEmailManager
{
    Task<bool> SendEmail(string email, string subject, string message);
}

// Here we send an email, subject and message to the user
// Ideally, we would have some sort of template for the message, but for now, we'll just send normal text
public class EmailService : IEmailManager
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendEmail(string email, string subject, string message)
    {
        var mail = _configuration["Email:Login"];
        var pw = _configuration["Email:Password"];
        var client = new SmtpClient("smtp-mail.outlook.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(mail, pw)
        };
        try
        {
            await client.SendMailAsync(
                new MailMessage(from: mail,
                    to: email,
                    subject,
                    message));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}