using System.Threading;
using System.Threading.Tasks;

namespace backend.Services;

public interface IRecaptchaService
{
    Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default);
}
