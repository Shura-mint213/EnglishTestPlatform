using System.ComponentModel.DataAnnotations;

namespace EnglishTestPlatform.Models
{
    public class TestViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Загрузите файл теста")]
        public IFormFile? File { get; set; }
    }
}