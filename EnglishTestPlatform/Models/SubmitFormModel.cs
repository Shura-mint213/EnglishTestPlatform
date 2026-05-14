namespace EnglishTestPlatform.Models
{
    /// <summary>
    /// Модель отправки формы
    /// </summary>
    public class SubmitFormModel
    {
        public string TestName { get; set; }
        public List<UserAnswerForm> UserAnswers { get; set; }
    }
}
