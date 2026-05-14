using EnglishTestPlatform.Models;

namespace EnglishTestPlatform.Services
{
    public class TestEvaluatorService
    {
        public ResultViewModel Evaluate(TestModel test, List<UserAnswer> userAnswers)
        {
            var results = new List<QuestionResult>();
            int correctCount = 0;
            var recommendations = new List<string>();

            for (int i = 0; i < test.Questions.Count; i++)
            {
                var question = test.Questions[i];
                var userAnswerObj = userAnswers.FirstOrDefault(ua => ua.questionIndex == i)?.answer;
                bool isCorrect = false;
                object correctAnswerObj = null;

                switch (question)
                {
                    case MultipleChoiceQuestion mcq:
                        correctAnswerObj = mcq.Correct;
                        isCorrect = mcq.Correct.Equals(userAnswerObj?.ToString(), StringComparison.OrdinalIgnoreCase);
                        break;

                    case MultipleSelectQuestion msq:
                        var userSelected = (userAnswerObj as List<string>) ?? new List<string>();
                        var correctSet = new HashSet<string>(msq.Correct, StringComparer.OrdinalIgnoreCase);
                        var userSet = new HashSet<string>(userSelected, StringComparer.OrdinalIgnoreCase);
                        isCorrect = correctSet.SetEquals(userSet);
                        correctAnswerObj = msq.Correct;
                        break;

                    case MatchingQuestion mq:
                        var userMatches = (userAnswerObj as Dictionary<string, string>) ?? new Dictionary<string, string>();
                        bool allMatch = true;
                        foreach (var pair in mq.Pairs)
                        {
                            if (!userMatches.ContainsKey(pair.Left) ||
                                !userMatches[pair.Left].Equals(pair.Right, StringComparison.OrdinalIgnoreCase))
                            {
                                allMatch = false;
                                break;
                            }
                        }
                        isCorrect = allMatch;
                        correctAnswerObj = mq.Pairs.ToDictionary(p => p.Left, p => p.Right);
                        break;

                    case FillInQuestion fiq:
                        var userText = userAnswerObj?.ToString()?.Trim() ?? "";
                        var correctText = fiq.Correct.Trim();
                        // Сравнение без учёта регистра и лишних пробелов
                        isCorrect = string.Equals(userText, correctText, StringComparison.OrdinalIgnoreCase);
                        correctAnswerObj = fiq.Correct;
                        break;
                }

                if (isCorrect) correctCount++;
                else if (!string.IsNullOrEmpty(question.Explanation))
                    recommendations.Add($"❌ Вопрос {i + 1}: {question.Explanation}");

                results.Add(new QuestionResult
                {
                    Question = question,
                    UserAnswer = userAnswerObj,
                    CorrectAnswer = correctAnswerObj,
                    IsCorrect = isCorrect,
                    Explanation = question.Explanation
                });
            }

            // Уникальные рекомендации
            recommendations = recommendations.Distinct().ToList();

            return new ResultViewModel
            {
                TestTitle = test.TestTitle,
                TotalQuestions = test.Questions.Count,
                CorrectCount = correctCount,
                QuestionResults = results,
                Recommendations = recommendations
            };
        }
    }
}
