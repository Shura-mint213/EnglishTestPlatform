/**
 * Visual Table Editor - Интерактивный редактор таблиц
 * Позволяет визуально редактировать таблицы с возможностью добавления/удаления строк и столбцов
 */

class VisualTableEditor {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
        this.tables = [];
        this.activeTableIndex = -1;
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
            adderContainer.style.cssText = 'position: absolute; top: -20px; left: 0; right: 0; display: flex; height: 20px; z-index: 100;';
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
            rowAdderContainer.style.cssText = 'position: absolute; top: 0; bottom: 0; left: -20px; display: flex; flex-direction: column; width: 20px; z-index: 100;';
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
            
            // Обработка фокуса
            cell.addEventListener('focus', () => {
                cell.classList.add('cell-focused');
            });

            cell.addEventListener('blur', () => {
                cell.classList.remove('cell-focused');
            });

            // Навигация клавиатурой
            cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));
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
                
                cell.addEventListener('focus', () => cell.classList.add('cell-focused'));
                cell.addEventListener('blur', () => cell.classList.remove('cell-focused'));
                cell.addEventListener('keydown', (e) => this.handleCellKeydown(e, table));
                
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
