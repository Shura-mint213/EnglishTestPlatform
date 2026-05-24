using Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace EnglishTestPlatform.ViewModel
{
    public class TheoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теории обязательно")]
        [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
        public string Name { get; set; } = string.Empty;
        public int? SectionId { get; set; }
        /// <summary>
        /// ID существующего файла
        /// </summary>
        public int? ExistingFileId { get; set; }

        public IFormFile? File { get; set; }
        
        /// <summary>
        /// Markdown контент теории (для ввода в редакторе)
        /// </summary>
        public string? MarkdownContent { get; set; }

        public List<Section>? Sections { get; set; }
    }
}
