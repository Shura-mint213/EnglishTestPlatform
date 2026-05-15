class CheatSheet {
    constructor() {
        this.isOpen = false;
        this.hasTheory = false;
        this.init();
    }

    init() {
        console.log('CheatSheet инициализация...');
        console.log('testName:', window.testName);

        // Создаем элементы
        this.createElements();

        // Загружаем теорию
        this.loadTheory();

        // Добавляем обработчики событий
        this.bindEvents();
    }

    createElements() {
        // Оверлей
        this.overlay = document.createElement('div');
        this.overlay.className = 'cheatsheet-overlay';
        document.body.appendChild(this.overlay);

        // Кнопка
        this.button = document.createElement('button');
        this.button.className = 'cheatsheet-toggle';
        this.button.innerHTML = '📚';
        this.button.title = 'Показать шпаргалку';
        document.body.appendChild(this.button);

        // Панель
        this.panel = document.createElement('div');
        this.panel.className = 'cheatsheet-panel';
        this.panel.innerHTML = `
            <div class="cheatsheet-header">
                <h3>📖 Шпаргалка</h3>
                <button class="cheatsheet-close">×</button>
            </div>
            <div class="cheatsheet-content">
                <div class="cheatsheet-loading">Загрузка теории...</div>
            </div>
        `;
        document.body.appendChild(this.panel);

        this.content = this.panel.querySelector('.cheatsheet-content');
        this.closeBtn = this.panel.querySelector('.cheatsheet-close');
    }

    async loadTheory() {
        try {
            const testName = window.testName;
            console.log('Загрузка теории для теста:', testName);

            if (!testName) {
                console.log('testName отсутствует');
                this.showNoTheory();
                return;
            }

            const url = `/Test/GetTheoryCheatSheet?testName=${encodeURIComponent(testName)}`;
            console.log('Запрос к:', url);

            const response = await fetch(url);
            console.log('Статус ответа:', response.status);

            const data = await response.json();
            console.log('Полученные данные:', data);

            if (data.hasTheory) {
                this.hasTheory = true;
                this.content.innerHTML = `
                    <div class="markdown-body">
                        ${data.content}
                    </div>
                `;
                console.log('Теория загружена успешно');
            } else {
                console.log('Теория не найдена, причина:', data.error);
                this.showNoTheory(data.error);
            }
        } catch (error) {
            console.error('Ошибка загрузки теории:', error);
            this.showNoTheory(error.message);
        }
    }

    showNoTheory(error = null) {
        this.hasTheory = false;
        this.content.innerHTML = `
            <div class="alert alert-info">
                <strong>ℹ️ Нет доступной теории</strong><br />
                Для этого теста пока нет связанных теоретических материалов.
                ${window.testName ? `<br />Тест: "${window.testName}"` : ''}
                ${error ? `<br /><small class="text-muted">Ошибка: ${error}</small>` : ''}
                <hr />
                <small>Вы можете добавить теорию в разделе администрирования и связать её с этим тестом.</small>
            </div>
        `;
    }

    bindEvents() {
        // Открытие/закрытие по кнопке
        this.button.addEventListener('click', () => {
            console.log('Кнопка шпаргалки нажата');
            if (this.hasTheory) {
                this.toggle();
            } else {
                this.showNoTheory();
                this.open();
            }
        });

        // Закрытие по кнопке "×"
        this.closeBtn.addEventListener('click', () => this.close());

        // Закрытие по оверлею
        this.overlay.addEventListener('click', () => this.close());

        // Закрытие по Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isOpen) {
                this.close();
            }
        });
    }

    toggle() {
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
    }

    open() {
        this.panel.classList.add('open');
        this.overlay.classList.add('show');
        this.isOpen = true;
        this.button.style.opacity = '0.7';
        console.log('Панель открыта');
    }

    close() {
        this.panel.classList.remove('open');
        this.overlay.classList.remove('show');
        this.isOpen = false;
        this.button.style.opacity = '1';
        console.log('Панель закрыта');
    }
}

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM загружен, путь:', window.location.pathname);
    // Инициализируем для всех страниц тестов
    if (window.testName) {
        console.log('Инициализация шпаргалки для теста:', window.testName);
        new CheatSheet();
    } else {
        console.log('testName не найден, шпаргалка не будет инициализирована');
    }
});