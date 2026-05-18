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
            // Создаем полный путь: ContentRootPath/Source/Tests или ContentRootPath/Source/Theories
            var uploadsDir = Path.Combine(_env.ContentRootPath, "Source", subFolder);

            Console.WriteLine($"Saving file to: {uploadsDir}");

            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
                Console.WriteLine($"Directory created: {uploadsDir}");
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            Console.WriteLine($"File saved: {filePath}");

            // Сохраняем относительный путь для БД
            var relativePath = Path.Combine("Source", subFolder, fileName);

            return new FileP
            {
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                FilePath = relativePath
            };
        }

        public void DeleteFile(FileP file)
        {
            var fullPath = Path.Combine(_env.ContentRootPath, file.FilePath);
            Console.WriteLine($"Deleting file: {fullPath}");

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Console.WriteLine($"File deleted: {fullPath}");
            }
            else
            {
                Console.WriteLine($"File not found: {fullPath}");
            }
        }
    }
}