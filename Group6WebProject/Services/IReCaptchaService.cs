using System.Threading.Tasks;

namespace Group6WebProject.Services
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyToken(string token);
    }
}