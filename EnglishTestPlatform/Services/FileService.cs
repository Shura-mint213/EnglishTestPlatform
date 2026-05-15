using Data.Entities;
using EnglishTestPlatform.Interfaces;

namespace EnglishTestPlatform.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        public FileService(IWebHostEnvironment env) => _env = env;

        public async Task<FileP> SaveFileAsync(IFormFile file, string subFolder)
        {
            string source = "Source";
            var uploadsDir = Path.Combine(_env.ContentRootPath, source,  subFolder);
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            return new FileP
            {
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FilePath = Path.Combine(source, subFolder, fileName)
            };
        }

        public void DeleteFile(FileP file)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, file.FilePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
