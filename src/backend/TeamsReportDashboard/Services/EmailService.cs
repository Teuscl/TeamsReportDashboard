using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using TeamsReportDashboard.Backend.Models.Configuration;

namespace TeamsReportDashboard.Backend.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }
    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        var subject = "Redefinição de senha - Sistema de Relatórios Helpdesk PECEGE";
        
        var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                    <h2>Redefinição de Senha</h2>
                    <p>Olá, {userName},</p>
                    <p>Recebemos uma solicitação para redefinir a senha da sua conta em nosso Sistema de Relatórios.</p>
                    <p>Se você não fez essa solicitação, pode ignorar este email com segurança.</p>
                    <p>Para definir uma nova senha, clique no link abaixo. Este link é válido por 1 hora.</p>
                    <p style='margin: 20px 0;'>
                        <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-size: 16px;'>
                            Redefinir Minha Senha
                        </a>
                    </p>
                    <p>Se o botão não funcionar, copie e cole a seguinte URL no seu navegador:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>
                    <br/>
                    <p>Atenciosamente,</p>
                    <p>Equipe do Sistema de Relatórios</p>
                </body>
                </html>";
        
        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };
        
        using var smtp = new SmtpClient();
        try
        {
            // Conecta ao servidor SMTP. SecureSocketOptions.StartTls é o mais comum.
            await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                
            // Autentica com seu usuário e senha (ou senha de app)
            await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                
            // Envia o email
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            // Lidar com erros de envio de email. Logar o erro é crucial.
            Console.WriteLine($"Erro ao enviar email: {ex.Message}");
            // Você pode querer lançar uma exceção customizada aqui se o envio de email for crítico
            // para a operação que o chamou. No caso do "esqueci a senha", talvez não
            // queiramos que a API falhe, apenas logar o erro.
            throw; // Lança a exceção para que a camada de serviço superior saiba que falhou.
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}