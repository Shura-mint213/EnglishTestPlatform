using Data.Entities;

namespace EnglishTestPlatform.Interfaces
{
    public interface IFileService
    {
        Task<FileP> SaveFileAsync(IFormFile file, string subFolder);
        void DeleteFile(FileP file);
    }
}
