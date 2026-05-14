namespace EnglishTestPlatform.Models
{
    /// <summary>
    /// Модель для получения ответов из формы
    /// </summary>
    public class UserAnswerForm
    {
        public int QuestionIndex { get; set; }
        public string? Answer { get; set; }  // Для одиночного выбора и fill_in
        public List<string>? SelectedOptions { get; set; }  // Для множественного выбора
        public List<KeyValuePair<string, string>>? Matches { get; set; }  // Для сопоставления
    }
}
