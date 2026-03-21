using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
        Task SendGroupSmsAsync(List<string> phoneNumbers, string message);
    }
}
