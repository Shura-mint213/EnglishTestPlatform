using System.ComponentModel.DataAnnotations;

namespace EnglishTestPlatform.ViewModel
{
    public class SectionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название раздела обязательно")]
        [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Описание не может быть длиннее 500 символов")]
        public string? Description { get; set; }

        public int? ParentSectionId { get; set; }

        public int OrderBy { get; set; } = 0;
    }
}