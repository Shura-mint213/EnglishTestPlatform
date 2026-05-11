using System.Collections.Generic;

namespace EnglishTestPlatform.Models
{
    public class ResultViewModel
    {
        public string TestTitle { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public double Percentage => TotalQuestions > 0 ? (double)CorrectCount / TotalQuestions * 100 : 0;
        public List<QuestionResult> QuestionResults { get; set; }
        public List<string> Recommendations { get; set; }
    }

    public class QuestionResult
    {
        public Question Question { get; set; }
        public object UserAnswer { get; set; }
        public object CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; }
    }
}