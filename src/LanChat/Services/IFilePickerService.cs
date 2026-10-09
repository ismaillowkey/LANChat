using System.Threading.Tasks;

namespace LanChat.Services;

public interface IFilePickerService
{
    Task<string?> PickImageFileAsync();
    Task<string?> PickVideoFileAsync();
}
