/**
 * Visual Table Editor - Интерактивный редактор таблиц
 * Позволяет визуально редактировать таблицы с возможностью добавления/удаления строк и столбцов
 * 
 * Особенности:
 * - WYSIWYG режим - таблицы отображаются как в обычных редакторах
 * - Плюсики для добавления строк/столбцов появляются только при наведении на границы таблицы
 * - Каждая таблица независима - действия применяются только к той таблице, с которой взаимодействует пользователь
 * - Пустые ячейки при добавлении (без текста "Новый" или "Новая ячейка")
 * - Навигация клавиатурой (Tab, Enter, Ctrl+стрелки)
 */

class VisualTableEditor {
    constructor(containerId, onChangeCallback) {
        this.container = document.getElementById(containerId);
        this.onChange = onChangeCallback;
        this.tables = [];

        if (!this.container) {
            console.error('Container not found:', containerId);
            return;
        }

        this.init();
    }

    init() {
        // Конвертируем все Markdown таблицы в HTML
        this.convertAllMarkdownTables();

        // Сканируем существующие таблицы
        this.scanTables();

        // Добавляем наблюдателя за изменениями
        this.setupMutationObserver();

        // Прикрепляем глобальные обработчики
        this.attachEventListeners();
    }

    // Наблюдатель за изменениями DOM
    setupMutationObserver() {
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList' || mutation.type === 'characterData') {
                    // Небольшая задержка для избежания циклов
                    clearTimeout(this.observerTimeout);
                    this.observerTimeout = setTimeout(() => {
                        this.scanTables();
                        if (this.onChange) this.onChange();
                    }, 100);
                }
            });
        });

        observer.observe(this.container, {
            childList: true,
            subtree: true,
            characterData: true
        });
    }

    // Сканирование всех таблиц в контейнере
    scanTables() {
        console.log('scanTables called');
        this.tables = Array.from(this.container.querySelectorAll('table'));
        console.log('Found tables:', this.tables.length);

        this.tables.forEach((table, index) => {
            console.log('Processing table', index);
            table.dataset.tableIndex = index;

            // Проверяем, есть ли строки
            const rows = table.querySelectorAll('tr');
            console.log('Table', index, 'has', rows.length, 'rows');

            if (rows.length > 0) {
                this.enhanceTable(table);
            } else {
                console.warn('Table', index, 'has no rows, skipping enhancement');
            }
        });
    }

    tableToMarkdown(table) {
        let md = '';
        const rows =
            Array.from(table.rows);
        if (!rows.length)
            return '';
        rows.forEach((row, rowIndex) => {
            md += '|';
            Array.from(row.cells)
                .forEach(cell => {
                    const value =
                        (cell.innerText || '')
                            .replace(/\|/g, '\\|');
                    md += value + '|';
                });
            md += '\n';
            if (rowIndex === 0) {
                md += '|';
                Array.from(row.cells)
                    .forEach(() => {
                        md += '---|';
                    });
                md += '\n';
            }
        });
        return md;
    }

    // Улучшение таблицы интерактивными элементами
    enhanceTable(table) {
        if (!table) {
            console.error('enhanceTable: table is null');
            return;
        }

        // Проверяем, есть ли строки в таблице
        const rows = table.querySelectorAll('tr');
        if (rows.length === 0) {
            console.error('enhanceTable: no rows found in table');
            return;
        }

        console.log('enhanceTable - enhancing table with', rows.length, 'rows');

        if (table.classList.contains('visual-table-enhanced')) {
            console.log('Table already enhanced');
            return;
        }

        table.classList.add('visual-table-enhanced');
        table.style.position = 'relative';

        // Добавляем обработчики наведения для показа плюсиков
        this.setupColumnAdders(table);
        this.setupRowAdders(table);

        // Делаем ячейки редактируемыми
        this.makeCellsEditable(table);

        table.addEventListener('mouseenter', () => {
            const adders = table.querySelectorAll('.column-adder, .row-adder');
            adders.forEach(el => {
                el.style.opacity = '1';
                const span = el.querySelector('span');
                if (span) span.style.opacity = '1';
            });
        });

        table.addEventListener('mouseleave', () => {
            const adders = table.querySelectorAll('.column-adder, .row-adder');
            adders.forEach(el => {
                el.style.opacity = '0';
                const span = el.querySelector('span');
                if (span) span.style.opacity = '0';
            });
        });
    }

    // Парсинг Markdown таблиц из текста и конвертация в HTML
    parseMarkdownTablesFromContent(content) {
        // Улучшенное регулярное выражение для поиска таблиц
        const markdownTableRegex = /(\|(?:[^\n]*\|)(?:\n\|[-:|\s]+\|)?(?:\n\|[^\n]*\|)*)/g;
        let match;
        const tables = [];

        while ((match = markdownTableRegex.exec(content)) !== null) {
            const markdownTable = match[1];
            // Проверяем наличие разделителя
            if (!markdownTable.includes('---') && !markdownTable.includes(':-')) {
                continue;
            }
            const htmlTable = this.markdownToTable(markdownTable);
            if (htmlTable) {
                tables.push({
                    original: markdownTable,
                    html: htmlTable.outerHTML
                });
            }
        }

        return tables;
    }

    // Замена всех Markdown таблиц в контейнере на HTML
    convertAllMarkdownTables() {
        if (!this.container) return;

        // Получаем текущий HTML контент
        let html = this.container.innerHTML;
        let hasChanges = false;

        // НОВОЕ РЕГУЛЯРНОЕ ВЫРАЖЕНИЕ для поиска полноценных Markdown таблиц
        // Ищем: заголовок | a | b | c |, затем разделитель |---|, затем строки данных
        const markdownTableRegex = /(\|(?:[^\n]*\|)(?:\n\|[-:|\s]+\|)?(?:\n\|[^\n]*\|)*)/g;

        html = html.replace(markdownTableRegex, (match) => {
            // Проверяем, что это действительно таблица (есть разделитель)
            if (!match.includes('---') && !match.includes(':-') && !match.includes('-|')) {
                return match; // Не таблица, возвращаем как есть
            }

            hasChanges = true;
            const table = this.markdownToTable(match);
            return table ? table.outerHTML : match;
        });

        if (hasChanges) {
            this.container.innerHTML = html;
            this.scanTables(); // Пересканируем таблицы
        }
    }

    // Настройка добавления столбцов
    setupColumnAdders(table) {
        const headerRow = table.querySelector('tr:first-child');
        if (!headerRow) return;

        const cells = Array.from(headerRow.querySelectorAll('th, td'));
        const columnCount = cells.length;

        // Создаём контейнер для плюсиков над таблицей
        let adderContainer = table.querySelector('.column-adder-container');
        if (!adderContainer) {
            adderContainer = document.createElement('div');
            adderContainer.className = 'column-adder-container';
            adderContainer.style.cssText = 'position: absolute; top: -20px; left: 0; right: 0; display: flex; height: 20px; z-index: 100; pointer-events: none;';
            table.appendChild(adderContainer);
        }

        // Очищаем и пересоздаём плюсики
        adderContainer.innerHTML = '';

        for (let i = 0; i <= columnCount; i++) {
            const adder = this.createColumnAdder(table, i);
            adderContainer.appendChild(adder);
        }
    }

    // Создание плюсика для добавления столбца
    createColumnAdder(table, columnIndex) {
        const adder = document.createElement('div');
        adder.className = 'column-adder';
        adder.style.cssText = `
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            opacity: 0.35;
            transition: opacity 0.2s;
            position: relative;
            pointer-events: auto;
        `;

        // Иконка плюса
        const plusIcon = document.createElement('span');
        plusIcon.innerHTML = '+';
        plusIcon.style.cssText = `
            background: #007bff;
            color: white;
            border-radius: 50%;
            width: 20px;
            height: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 16px;
            font-weight: bold;
            opacity: 0.35;
            transition: opacity 0.2s;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
        `;

        adder.appendChild(plusIcon);

        // Показываем при наведении на область
        adder.addEventListener('mouseenter', () => {
            plusIcon.style.opacity = '1';
            adder.style.opacity = '1';
        });

        adder.addEventListener('mouseleave', () => {
            plusIcon.style.opacity = '0';
            adder.style.opacity = '0';
        });

        // Клик для добавления столбца
        adder.addEventListener('click', (e) => {
            e.stopPropagation();
            this.addColumn(table, columnIndex);
        });

        return adder;
    }

    // Настройка добавления строк
    setupRowAdders(table) {
        if (!table) {
            console.error('setupRowAdders: table is null');
            return;
        }

        // Удаляем старый контейнер если есть
        let rowAdderContainer = table.querySelector('.row-adder-container');
        if (rowAdderContainer) {
            rowAdderContainer.remove();
        }

        // Создаём новый контейнер для плюсиков
        rowAdderContainer = document.createElement('div');
        rowAdderContainer.className = 'row-adder-container';
        rowAdderContainer.style.cssText = 'position: absolute; top: 0; bottom: 0; left: -20px; display: flex; flex-direction: column; width: 20px; z-index: 100; pointer-events: none;';
        table.appendChild(rowAdderContainer);

        // Считаем общее количество строк
        const rows = Array.from(table.querySelectorAll('tr'));
        const rowCount = rows.length;

        // Создаем плюсики для каждой строки + один дополнительный в конце
        for (let i = 0; i <= rowCount; i++) {
            const adder = this.createRowAdder(table, i);
            rowAdderContainer.appendChild(adder);
        }
    }

    // Создание плюсика для добавления строки
    createRowAdder(table, rowIndex) {
        const adder = document.createElement('div');
        adder.className = 'row-adder';
        adder.style.cssText = `
        flex: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        opacity: 0.35;
        transition: opacity 0.2s;
        pointer-events: auto;
    `;

        const plusIcon = document.createElement('span');
        plusIcon.innerHTML = '+';
        plusIcon.style.cssText = `
        background: #28a745;
        color: white;
        border-radius: 50%;
        width: 20px;
        height: 20px;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 16px;
        font-weight: bold;
        opacity: 0.35;
        transition: opacity 0.2s;
        box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    `;

        adder.appendChild(plusIcon);

        adder.addEventListener('mouseenter', () => {
            plusIcon.style.opacity = '1';
            adder.style.opacity = '1';
        });

        adder.addEventListener('mouseleave', () => {
            plusIcon.style.opacity = '0';
            adder.style.opacity = '0';
        });

        // Клик для добавления строки
        adder.addEventListener('click', (e) => {
            e.stopPropagation();
            console.log('Row adder clicked - rowIndex:', rowIndex);
            console.log('Table:', table);

            // Проверяем, что таблица существует и содержит строки
            if (!table || !table.querySelector('tr')) {
                console.error('Table or rows not found');
                return;
            }

            this.addRow(table, rowIndex);
        });

        return adder;
    }

    // Делаем ячейки редактируемыми
    makeCellsEditable(table) {
        const cells = table.querySelectorAll('td, th');
        cells.forEach(cell => {
            cell.contentEditable = 'true';
            cell.style.outline = 'none';
            cell.style.border = '1px solid #dee2e6';
            cell.style.padding = '8px';

            // Обработка фокуса
            cell.addEventListener('focus', () => {
                cell.classList.add('cell-focused');
                cell.style.backgroundColor = '#f0f7ff';
            });

            cell.addEventListener('blur', () => {
                cell.classList.remove('cell-focused');
                cell.style.backgroundColor = '';
            });

            // Навигация клавиатурой
            cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));

            // Контекстное меню по правому клику
            cell.addEventListener('contextmenu', (e) => {
                const row = cell.parentElement;
                const rows = Array.from(table.querySelectorAll('tr'));
                const rowIndex = rows.indexOf(row);
                const cellIndex = Array.from(row.querySelectorAll('td, th')).indexOf(cell);
                this.showTableContextMenu(e, table, rowIndex, cellIndex);
            });
        });
    }

    // Обработка нажатий клавиш в ячейке
    handleCellKeydown(e, table) {
        const currentCell = e.target;
        const row = currentCell.parentElement;
        const cells = Array.from(row.querySelectorAll('td, th'));
        const cellIndex = cells.indexOf(currentCell);
        const rows = Array.from(table.querySelectorAll('tr'));
        const rowIndex = rows.indexOf(row);

        switch (e.key) {
            case 'Tab':
                e.preventDefault();
                if (e.shiftKey) {
                    // Shift+Tab - предыдущая ячейка
                    this.focusCell(table, rowIndex, cellIndex - 1);
                } else {
                    // Tab - следующая ячейка
                    this.focusCell(table, rowIndex, cellIndex + 1);
                }
                break;
            case 'Enter':
                if (!e.shiftKey) {
                    e.preventDefault();
                    // Enter - следующая строка
                    this.focusCell(table, rowIndex + 1, cellIndex);
                }
                break;
            case 'ArrowUp':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.focusCell(table, rowIndex - 1, cellIndex);
                }
                break;
            case 'ArrowDown':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.focusCell(table, rowIndex + 1, cellIndex);
                }
                break;
            case 'ArrowLeft':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.focusCell(table, rowIndex, cellIndex - 1);
                }
                break;
            case 'ArrowRight':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.focusCell(table, rowIndex, cellIndex + 1);
                }
                break;
        }
    }

    // Фокус на ячейку
    focusCell(table, rowIndex, cellIndex) {
        const rows = Array.from(table.querySelectorAll('tr'));
        if (rowIndex < 0 || rowIndex >= rows.length) return;

        const row = rows[rowIndex];
        const cells = Array.from(row.querySelectorAll('td, th'));
        if (cellIndex < 0 || cellIndex >= cells.length) return;

        cells[cellIndex].focus();
    }

    // Добавление столбца
    addColumn(table, columnIndex) {
        const rows = Array.from(table.querySelectorAll('tr'));

        rows.forEach((row, rowIndex) => {
            const cells = Array.from(row.querySelectorAll('th, td'));
            const newCell = document.createElement(rowIndex === 0 ? 'th' : 'td');
            newCell.contentEditable = 'true';
            newCell.style.outline = 'none';
            newCell.style.border = '1px solid #dee2e6';
            newCell.style.padding = '8px';
            newCell.style.minWidth = '80px';

            // Пустая ячейка без текста "Новый"
            newCell.innerHTML = '';

            // Добавляем обработчики
            newCell.addEventListener('focus', () => {
                newCell.classList.add('cell-focused');
                newCell.style.backgroundColor = '#f0f7ff';
            });

            newCell.addEventListener('blur', () => {
                newCell.classList.remove('cell-focused');
                newCell.style.backgroundColor = '';
            });

            newCell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));

            // Добавляем обработчик контекстного меню
            newCell.addEventListener('contextmenu', (e) => {
                e.stopPropagation(); // Предотвращаем всплытие к редактору
                const row = newCell.parentElement;
                const rows = Array.from(table.querySelectorAll('tr'));
                const currentRowIndex = rows.indexOf(row);
                const cells = Array.from(row.querySelectorAll('td, th'));
                const currentCellIndex = cells.indexOf(newCell);
                this.showTableContextMenu(e, table, currentRowIndex, currentCellIndex);
            });

            if (columnIndex >= cells.length) {
                row.appendChild(newCell);
            } else {
                const referenceCell = cells[columnIndex];
                if (referenceCell && referenceCell.parentNode === row) {
                    row.insertBefore(newCell, referenceCell);
                } else {
                    row.appendChild(newCell);
                }
            }

            // Если это первая строка и новая ячейка, фокусируемся на ней
            if (rowIndex === 0) {
                setTimeout(() => newCell.focus(), 10);
            }
        });

        // Обновляем плюсик
        this.setupColumnAdders(table);
        this.setupRowAdders(table);

        // Событие изменения
        this.onTableChange(table);
    }

    // Добавление строки
    addRow(table, rowIndex) {
        // Получаем ВСЕ строки tr в таблице
        const rows = Array.from(table.querySelectorAll('tr'));

        console.log('addRow called - table:', table);
        console.log('addRow - rowIndex:', rowIndex);
        console.log('addRow - found rows:', rows.length);

        if (rows.length === 0) {
            console.error('No rows found in table');
            return;
        }

        // Проверяем структуру таблицы (есть ли thead/tbody)
        const thead = table.querySelector('thead');
        const tbody = table.querySelector('tbody');

        console.log('Table structure - thead:', !!thead, 'tbody:', !!tbody);

        const headerRow = rows[0];
        const columnCount = headerRow ? headerRow.querySelectorAll('th, td').length : 3;

        console.log('Column count:', columnCount);

        const newRow = document.createElement('tr');

        for (let i = 0; i < columnCount; i++) {
            const cell = document.createElement('td');
            cell.contentEditable = 'true';
            cell.style.outline = 'none';
            cell.style.border = '1px solid #dee2e6';
            cell.style.padding = '8px';
            cell.style.minWidth = '80px';
            cell.innerHTML = '';

            cell.addEventListener('focus', () => {
                cell.classList.add('cell-focused');
                cell.style.backgroundColor = '#f0f7ff';
            });

            cell.addEventListener('blur', () => {
                cell.classList.remove('cell-focused');
                cell.style.backgroundColor = '';
            });

            cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));

            cell.addEventListener('contextmenu', (e) => {
                const row = cell.parentElement;
                const currentRows = Array.from(table.querySelectorAll('tr'));
                const currentRowIndex = currentRows.indexOf(row);
                const cellIndex = Array.from(row.querySelectorAll('td, th')).indexOf(cell);
                this.showTableContextMenu(e, table, currentRowIndex, cellIndex);
            });

            newRow.appendChild(cell);
        }

        // Определяем, куда вставлять новую строку
        if (tbody) {
            // Есть tbody - работаем с ним
            const bodyRows = Array.from(tbody.querySelectorAll('tr'));
            console.log('Body rows:', bodyRows.length);

            if (rowIndex === 0 || bodyRows.length === 0) {
                // Добавляем в начало tbody (после заголовка)
                if (bodyRows.length > 0) {
                    tbody.insertBefore(newRow, bodyRows[0]);
                    console.log('Inserted row at beginning of tbody');
                } else {
                    tbody.appendChild(newRow);
                    console.log('Added row to empty tbody');
                }
            } else if (rowIndex >= bodyRows.length) {
                // Добавляем в конец tbody
                tbody.appendChild(newRow);
                console.log('Added row to end of tbody');
            } else {
                // Вставляем на указанную позицию в tbody
                const referenceRow = bodyRows[rowIndex];
                if (referenceRow && referenceRow.parentNode === tbody) {
                    tbody.insertBefore(newRow, referenceRow);
                    console.log('Inserted row at position', rowIndex, 'in tbody');
                } else {
                    tbody.appendChild(newRow);
                    console.log('Fallback: added row to end of tbody');
                }
            }
        } else if (thead) {
            // Есть thead, но нет tbody - создаем tbody
            const newTbody = document.createElement('tbody');
            newTbody.appendChild(newRow);

            // Вставляем tbody после thead
            if (thead.nextSibling) {
                table.insertBefore(newTbody, thead.nextSibling);
            } else {
                table.appendChild(newTbody);
            }
            console.log('Created tbody and added row');
        } else {
            // Нет ни thead, ни tbody - работаем напрямую с table
            if (rowIndex >= rows.length) {
                table.appendChild(newRow);
                console.log('Added row to end of table');
            } else if (rowIndex <= 0) {
                // После заголовка
                if (rows.length > 1) {
                    table.insertBefore(newRow, rows[1]);
                    console.log('Inserted row after header');
                } else {
                    table.appendChild(newRow);
                    console.log('Added row as only data row');
                }
            } else {
                const referenceRow = rows[rowIndex];
                if (referenceRow && referenceRow.parentNode === table) {
                    table.insertBefore(newRow, referenceRow);
                    console.log('Inserted row at position', rowIndex);
                } else {
                    table.appendChild(newRow);
                    console.log('Fallback: added row to end');
                }
            }
        }

        // Обновляем плюсики
        setTimeout(() => {
            this.setupColumnAdders(table);
            this.setupRowAdders(table);
        }, 50);

        // Фокус на первую ячейку новой строки
        setTimeout(() => {
            const firstCell = newRow.querySelector('td:first-child');
            if (firstCell) {
                firstCell.focus();
            }
        }, 100);

        // Событие изменения
        this.onTableChange(table);
    }


    // Запасной метод добавления строки
    addRowFallback(table, rowIndex, rows) {
        if (rows.length === 0) return;

        const headerRow = rows[0];
        const columnCount = headerRow ? headerRow.cells.length : 3;

        const newRow = document.createElement('tr');

        for (let i = 0; i < columnCount; i++) {
            const cell = document.createElement('td');
            cell.contentEditable = 'true';
            cell.style.cssText = 'border: 1px solid #dee2e6; padding: 8px; outline: none; min-width: 80px;';
            cell.innerHTML = '';

            cell.addEventListener('focus', () => {
                cell.classList.add('cell-focused');
                cell.style.backgroundColor = '#f0f7ff';
            });

            cell.addEventListener('blur', () => {
                cell.classList.remove('cell-focused');
                cell.style.backgroundColor = '';
            });

            cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));

            newRow.appendChild(cell);
        }

        // Просто добавляем в конец таблицы
        table.appendChild(newRow);

        setTimeout(() => {
            this.setupColumnAdders(table);
            this.setupRowAdders(table);
        }, 50);

        setTimeout(() => {
            const firstCell = newRow.querySelector('td:first-child');
            if (firstCell) firstCell.focus();
        }, 100);

        this.onTableChange(table);
    }

    // Удаление столбца
    removeColumn(table, columnIndex) {
        const rows = Array.from(table.querySelectorAll('tr'));

        rows.forEach(row => {
            const cells = Array.from(row.querySelectorAll('th, td'));
            if (cells.length > 2 && columnIndex < cells.length) {
                cells[columnIndex].remove();
            }
        });

        this.setupColumnAdders(table);
        this.setupRowAdders(table);
        this.onTableChange(table);
    }

    // Удаление строки
    removeRow(table, rowIndex) {
        const rows = Array.from(table.querySelectorAll('tr'));

        // Не удаляем заголовок
        if (rowIndex === 0) {
            alert('Нельзя удалить строку заголовка!');
            return;
        }

        // Оставляем минимум одну строку данных
        if (rows.length <= 2) {
            alert('В таблице должна быть хотя бы одна строка данных!');
            return;
        }

        if (rowIndex < rows.length) {
            rows[rowIndex].remove();
        }

        this.setupRowAdders(table);
        this.onTableChange(table);
    }

    // Показать контекстное меню для таблицы
    showTableContextMenu(e, table, rowIndex, cellIndex) {
        e.preventDefault();
        e.stopPropagation();

        // Удаляем старое меню если есть
        const existingMenu = document.getElementById('visual-table-context-menu');
        if (existingMenu) existingMenu.remove();

        const menu = document.createElement('div');
        menu.id = 'visual-table-context-menu';
        menu.style.cssText = `
            position: fixed;
            z-index: 9999;
            background: white;
            border: 1px solid #dee2e6;
            border-radius: 4px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            min-width: 200px;
        `;

        menu.innerHTML = `
            <div class=\"dropdown-item\" style="padding: 8px 12px; cursor: pointer;" data-action="add-row-above">➕ Добавить строку выше</div>
            <div class="dropdown-item" style="padding: 8px 12px; cursor: pointer;" data-action="add-row-below">➕ Добавить строку ниже</div>
            <div class="dropdown-item" style="padding: 8px 12px; cursor: pointer;" data-action="add-column-left">➕ Добавить столбец слева</div>
            <div class="dropdown-item" style="padding: 8px 12px; cursor: pointer;" data-action="add-column-right">➕ Добавить столбец справа</div>
            <hr style="margin: 4px 0;">
            <div class="dropdown-item" style="padding: 8px 12px; cursor: pointer; color: #dc3545;" data-action="delete-row">❌ Удалить строку</div>
            <div class="dropdown-item" style="padding: 8px 12px; cursor: pointer; color: #dc3545;" data-action="delete-column">❌ Удалить столбец</div>
        `;

        document.body.appendChild(menu);

        // Позиционируем меню
        let x = e.pageX;
        let y = e.pageY;

        const rect = menu.getBoundingClientRect();
        if (x + rect.width > window.innerWidth) {
            x = window.innerWidth - rect.width - 10;
        }
        if (y + rect.height > window.innerHeight) {
            y = window.innerHeight - rect.height - 10;
        }

        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        // Обработчики кликов
        menu.querySelectorAll('[data-action]').forEach(item => {
            item.addEventListener('click', () => {
                const action = item.dataset.action;
                this.handleContextMenuAction(action, table, rowIndex, cellIndex);
                menu.remove();
            });
        });

        // Закрытие при клике вне меню
        setTimeout(() => {
            document.addEventListener('click', function closeMenu() {
                menu.remove();
                document.removeEventListener('click', closeMenu);
            });
        }, 100);
    }

    // Обработка действий контекстного меню
    handleContextMenuAction(action, table, rowIndex, cellIndex) {
        switch (action) {
            case 'add-row-above':
                this.addRow(table, rowIndex);
                break;
            case 'add-row-below':
                this.addRow(table, rowIndex + 1);
                break;
            case 'add-column-left':
                this.addColumn(table, cellIndex);
                break;
            case 'add-column-right':
                this.addColumn(table, cellIndex + 1);
                break;
            case 'delete-row':
                if (rowIndex > 0) { // Не удаляем заголовок
                    this.removeRow(table, rowIndex);
                }
                break;
            case 'delete-column':
                this.removeColumn(table, cellIndex);
                break;
        }
    }

    // Конвертация Markdown в HTML таблицу
    markdownToTable(markdown) {
        const lines = markdown.trim().split('\n');
        if (lines.length < 2) return null;

        // Находим индекс строки-разделителя
        let separatorIndex = -1;
        for (let i = 0; i < lines.length; i++) {
            if (lines[i].includes('---') || lines[i].includes(':-')) {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex === -1) return null;

        const table = document.createElement('table');
        table.className = 'table table-bordered visual-table visual-table-enhanced';
        table.style.cssText = 'border-collapse: collapse; width: 100%; margin: 10px 0; position: relative;';

        // Создаем thead и tbody для правильной структуры
        const thead = document.createElement('thead');
        const tbody = document.createElement('tbody');

        // Обрабатываем строки
        lines.forEach((line, index) => {
            if (index === separatorIndex) return; // Пропускаем строку разделитель
            if (!line.trim().startsWith('|')) return;

            const cells = line.split('|').filter((c, i, arr) => i > 0 && i < arr.length - 1);
            if (cells.length === 0) return;

            const tr = document.createElement('tr');
            const isHeader = index < separatorIndex;

            cells.forEach(cellContent => {
                const cell = document.createElement(isHeader ? 'th' : 'td');
                cell.contentEditable = 'true';
                cell.style.cssText = 'border: 1px solid #dee2e6; padding: 8px; outline: none; min-width: 80px;';
                cell.innerText = cellContent.trim();

                cell.addEventListener('focus', () => {
                    cell.classList.add('cell-focused');
                    cell.style.backgroundColor = '#f0f7ff';
                });
                cell.addEventListener('blur', () => {
                    cell.classList.remove('cell-focused');
                    cell.style.backgroundColor = '';
                });
                cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));
                cell.addEventListener('contextmenu', (e) => {
                    const row = cell.parentElement;
                    const rows = Array.from(table.querySelectorAll('tr'));
                    const rowIndex = rows.indexOf(row);
                    const cellIndex = Array.from(row.querySelectorAll('td, th')).indexOf(cell);
                    this.showTableContextMenu(e, table, rowIndex, cellIndex);
                });

                tr.appendChild(cell);
            });

            // Добавляем строку в соответствующий раздел
            if (isHeader) {
                thead.appendChild(tr);
            } else {
                tbody.appendChild(tr);
            }
        });

        table.appendChild(thead);
        table.appendChild(tbody);

        return table;
    }
    // Обновление плюсиков после изменения
    onTableChange(table) {
        // Здесь можно вызвать callback для уведомления об изменении
        if (typeof this.onChange === 'function') {
            this.onChange(table);
        }
    }

    // Привязка глобальных обработчиков
    attachEventListeners() {
        // Пересчёт позиций при изменении размера окна
        window.addEventListener('resize', () => {
            this.tables.forEach(table => {
                this.setupColumnAdders(table);
                this.setupRowAdders(table);
            });
        });
    }

    // Обновить все таблицы из Markdown контента
    refreshFromContent(content) {
        // Парсим контент и находим все таблицы
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = content;
        const tables = tempDiv.querySelectorAll('table');

        // Обновляем существующие или создаём новые
        tables.forEach((table, index) => {
            if (index < this.tables.length) {
                // Заменяем содержимое существующей таблицы
                this.tables[index].innerHTML = table.innerHTML;
                this.enhanceTable(this.tables[index]);
            }
        });
    }
}

// Экспорт для использования
if (typeof module !== 'undefined' && module.exports) {
    module.exports = VisualTableEditor;
}
