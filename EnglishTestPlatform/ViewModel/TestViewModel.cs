using Data.Entities;
using System.ComponentModel.DataAnnotations;
using EnglishTestPlatform.Models;

namespace EnglishTestPlatform.ViewModel
{
    public class TestViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название теста обязательно")]
        [StringLength(200, ErrorMessage = "Название не может быть длиннее 200 символов")]
        public string Name { get; set; } = string.Empty;
        public int? SectionId { get; set; }
        /// <summary>
        /// ID существующего файла
        /// </summary>
        public int? ExistingFileId { get; set; } 

        public IFormFile? File { get; set; }
        
        /// <summary>
        /// JSON контент теста (для вставки текстом)
        /// </summary>
        public string? JsonContent { get; set; }
        
        /// <summary>
        /// Список вопросов для создания через форму
        /// </summary>
        public List<QuestionFormModel> Questions { get; set; } = new();
        
        public List<Section>? Sections { get; set; }
    }
    
    /// <summary>
    /// Модель вопроса для формы создания теста
    /// </summary>
    public class QuestionFormModel
    {
        public string Type { get; set; } = "multiple_choice";
        public string Text { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        
        // Для multiple_choice и multiple_select
        public List<string> Options { get; set; } = new();
        public List<bool> CorrectAnswers { get; set; } = new();
        
        // Для matching
        public List<string> LeftItems { get; set; } = new();
        public List<string> RightItems { get; set; } = new();
        
        // Для fill_in
        public List<string> CorrectAnswersList { get; set; } = new();
    }
}