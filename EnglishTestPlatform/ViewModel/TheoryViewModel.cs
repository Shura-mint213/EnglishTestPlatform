using System.ComponentModel.DataAnnotations;

namespace EnglishTestPlatform.ViewModel
{
    public class TheoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теории обязательно")]
        [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Загрузите файл")]
        public IFormFile? File { get; set; }
    }
}
