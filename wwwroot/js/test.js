(function () {
    // Сбор ответов
    function collectAnswers() {
        let answers = [];
        document.querySelectorAll('.question-card').forEach((card, idx) => {
            let type = card.dataset.type;
            let answer = null;

            if (type === 'multiple_choice') {
                let selected = card.querySelector('input[type="radio"]:checked');
                answer = selected ? selected.value : null;
            }
            else if (type === 'multiple_select') {
                let selected = Array.from(card.querySelectorAll('input[type="checkbox"]:checked')).map(cb => cb.value);
                answer = selected;
            }
            else if (type === 'matching') {
                let matches = {};
                card.querySelectorAll('.matching-select').forEach(select => {
                    let left = select.dataset.left;
                    let val = select.value;
                    if (val) matches[left] = val;
                });
                answer = matches;
            }
            else if (type === 'fill_in') {
                answer = card.querySelector('.fill-input').value;
            }
            answers.push({
                questionIndex: idx,
                answer: answer
            });
        });
        return answers;
    }

    // Сохранение в localStorage
    function saveProgress() {
        let answers = collectAnswers();
        localStorage.setItem(`test_${window.testName}_progress`, JSON.stringify(answers));
        console.log('Progress saved:', answers);
    }

    // Загрузка из localStorage
    function loadProgress() {
        let saved = localStorage.getItem(`test_${window.testName}_progress`);
        if (!saved) return;
        let answers = JSON.parse(saved);
        console.log('Loading progress:', answers);

        answers.forEach(ans => {
            let card = document.querySelector(`.question-card[data-index='${ans.questionIndex}']`);
            if (!card) return;
            let type = card.dataset.type;
            if (type === 'multiple_choice' && ans.answer) {
                let radio = card.querySelector(`input[value='${ans.answer}']`);
                if (radio) radio.checked = true;
            }
            else if (type === 'multiple_select' && Array.isArray(ans.answer)) {
                ans.answer.forEach(val => {
                    let cb = card.querySelector(`input[value='${val}']`);
                    if (cb) cb.checked = true;
                });
            }
            else if (type === 'matching' && typeof ans.answer === 'object') {
                for (let left in ans.answer) {
                    let select = card.querySelector(`.matching-select[data-left='${left}']`);
                    if (select) select.value = ans.answer[left];
                }
            }
            else if (type === 'fill_in' && typeof ans.answer === 'string') {
                let input = card.querySelector('.fill-input');
                if (input) input.value = ans.answer;
            }
        });
    }

    // Отправка результатов
    async function submitTest() {
        console.log('=== SUBMIT TEST START ===');
        console.log('Test name:', window.testName);

        try {
            let answers = collectAnswers();
            console.log('Collected answers:', answers);

            let payload = {
                testName: window.testName,
                answersJson: JSON.stringify(answers)
            };

            console.log('Payload to send:', payload);

            // Пробуем разные способы отправки
            console.log('Sending fetch request...');

            let response = await fetch('/Test/Submit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            console.log('Response received!');
            console.log('Response status:', response.status);
            console.log('Response status text:', response.statusText);
            console.log('Response headers:', [...response.headers.entries()]);

            if (response.ok) {
                console.log('Response is OK, redirecting to result page...');

                // Очищаем прогресс
                localStorage.removeItem(`test_${window.testName}_progress`);

                window.location.href = `/Test/Result?testName=${encodeURIComponent(window.testName)}`;
                console.log('Redirected to result page');
            } else {
                console.error('Response not OK!', response.status);
                let errorText = await response.text();
                console.error('Error body:', errorText);
                alert(`Ошибка ${response.status}: ${errorText}`);
            }
        } catch (error) {
            console.error('=== FETCH ERROR ===');
            console.error('Error type:', error.name);
            console.error('Error message:', error.message);
            console.error('Stack trace:', error.stack);
            alert('Ошибка при отправке теста: ' + error.message);
        }

        console.log('=== SUBMIT TEST END ===');
    }

    // Функция для получения антифоржери токена
    function getAntiForgeryToken() {
        let token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    // Автосохранение при изменении полей
    function bindAutoSave() {
        document.querySelectorAll('.question-card input, .question-card select').forEach(el => {
            el.addEventListener('change', saveProgress);
            el.addEventListener('input', saveProgress);
        });
    }

    // Инициализация
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            if (window.testName) {
                loadProgress();
                bindAutoSave();
                let submitBtn = document.getElementById('submitBtn');
                if (submitBtn) {
                    submitBtn.addEventListener('click', submitTest);
                    console.log('Submit button bound');
                } else {
                    console.error('Submit button not found!');
                }
            }
        });
    } else {
        if (window.testName) {
            loadProgress();
            bindAutoSave();
            let submitBtn = document.getElementById('submitBtn');
            if (submitBtn) {
                submitBtn.addEventListener('click', submitTest);
                console.log('Submit button bound');
            } else {
                console.error('Submit button not found!');
            }
        }
    }
})();