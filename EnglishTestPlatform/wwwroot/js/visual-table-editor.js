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
        this.tables = [];
        this.activeTableIndex = -1;
        this.onChange = onChangeCallback || null;
        this.init();
    }

    init() {
        this.scanTables();
        this.attachEventListeners();
    }

    // Сканирование всех таблиц в контейнере
    scanTables() {
        this.tables = Array.from(this.container.querySelectorAll('table'));
        this.tables.forEach((table, index) => {
            table.dataset.tableIndex = index;
            this.enhanceTable(table);
        });
    }

    // Улучшение таблицы интерактивными элементами
    enhanceTable(table) {
        if (table.classList.contains('visual-table-enhanced')) return;
        
        table.classList.add('visual-table-enhanced');
        table.style.position = 'relative';
        
        // Добавляем обработчики наведения для показа плюсиков
        this.setupColumnAdders(table);
        this.setupRowAdders(table);
        
        // Делаем ячейки редактируемыми
        this.makeCellsEditable(table);
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
            opacity: 0;
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
            opacity: 0;
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
        const rows = Array.from(table.querySelectorAll('tr'));
        const rowCount = rows.length;

        // Создаём контейнер для плюсиков слева от таблицы
        let rowAdderContainer = table.querySelector('.row-adder-container');
        if (!rowAdderContainer) {
            rowAdderContainer = document.createElement('div');
            rowAdderContainer.className = 'row-adder-container';
            rowAdderContainer.style.cssText = 'position: absolute; top: 0; bottom: 0; left: -20px; display: flex; flex-direction: column; width: 20px; z-index: 100; pointer-events: none;';
            table.appendChild(rowAdderContainer);
        }

        // Очищаем и пересоздаём плюсики
        rowAdderContainer.innerHTML = '';

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
            opacity: 0;
            transition: opacity 0.2s;
            pointer-events: auto;
        `;

        // Иконка плюса
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
            opacity: 0;
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

        // Клик для добавления строки
        adder.addEventListener('click', (e) => {
            e.stopPropagation();
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
            
            // Пустая ячейка без текста "Новый"
            newCell.innerHTML = '';
            
            // Добавляем обработчики
            newCell.addEventListener('focus', () => newCell.classList.add('cell-focused'));
            newCell.addEventListener('blur', () => newCell.classList.remove('cell-focused'));
            newCell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));

            if (columnIndex >= cells.length) {
                row.appendChild(newCell);
            } else {
                row.insertBefore(newCell, cells[columnIndex]);
            }

            // Если это первая строка и новая ячейка, фокусируемся на ней
            if (rowIndex === 0 && columnIndex === cells.length) {
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
        const rows = Array.from(table.querySelectorAll('tr'));
        const headerRow = rows[0];
        const columnCount = headerRow ? headerRow.querySelectorAll('th, td').length : 3;

        const newRow = document.createElement('tr');
        
        for (let i = 0; i < columnCount; i++) {
            const cell = document.createElement('td');
            cell.contentEditable = 'true';
            cell.style.outline = 'none';
            cell.innerHTML = ''; // Пустая ячейка
            
            cell.addEventListener('focus', () => cell.classList.add('cell-focused'));
            cell.addEventListener('blur', () => cell.classList.remove('cell-focused'));
            cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));
            
            newRow.appendChild(cell);
        }

        if (rowIndex >= rows.length) {
            table.appendChild(newRow);
            // Фокус на первую ячейку новой строки
            setTimeout(() => {
                const firstCell = newRow.querySelector('td:first-child');
                if (firstCell) firstCell.focus();
            }, 10);
        } else {
            table.insertBefore(newRow, rows[rowIndex]);
        }

        // Обновляем плюсик
        this.setupColumnAdders(table);
        this.setupRowAdders(table);
        
        // Событие изменения
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
                this.removeRow(table, rowIndex);
                break;
            case 'delete-column':
                this.removeColumn(table, cellIndex);
                break;
        }
    }

    // Конвертация таблицы в Markdown
    tableToMarkdown(table) {
        const rows = Array.from(table.querySelectorAll('tr'));
        if (rows.length === 0) return '';

        let markdown = '\n';
        
        rows.forEach((row, rowIndex) => {
            const cells = Array.from(row.querySelectorAll('th, td'));
            let line = '|';
            
            cells.forEach(cell => {
                const text = cell.innerText.trim();
                line += ` ${text} |`;
            });
            
            markdown += line + '\n';
            
            // Добавляем разделитель после заголовка
            if (rowIndex === 0) {
                markdown += '|' + cells.map(() => ' --- |').join('') + '\n';
            }
        });

        return markdown + '\n';
    }

    // Конвертация Markdown в HTML таблицу
    markdownToTable(markdown) {
        const lines = markdown.trim().split('\n').filter(line => line.trim());
        if (lines.length === 0) return null;

        const table = document.createElement('table');
        table.className = 'table table-bordered visual-table-enhanced';
        table.style.cssText = 'border-collapse: collapse; width: 100%; margin: 10px 0;';

        lines.forEach((line, index) => {
            // Пропускаем строки-разделители
            if (line.includes('---')) return;

            const cells = line.split('|').filter((c, i, arr) => i > 0 && i < arr.length - 1);
            if (cells.length === 0) return;

            const tr = document.createElement('tr');
            
            cells.forEach(cellContent => {
                const cell = document.createElement(index === 0 ? 'th' : 'td');
                cell.contentEditable = 'true';
                cell.style.cssText = 'border: 1px solid #dee2e6; padding: 8px; outline: none;';
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

            table.appendChild(tr);
        });

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

    // Получение всех таблиц в виде Markdown
    getAllTablesMarkdown() {
        return this.tables.map(table => this.tableToMarkdown(table)).join('');
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
