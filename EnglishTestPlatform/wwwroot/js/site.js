// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Навигационная панель для теории - якоря и подсветка активного раздела
(function() {
    'use strict';

    // Инициализация навигационной панели теории
    function initTheoryNavigation() {
        const markdownBody = document.querySelector('.markdown-body');
        if (!markdownBody) return;

        // Создаем контейнер навигации
        const navContainer = document.createElement('div');
        navContainer.className = 'theory-nav-container';

        // Кнопка переключения видимости sidebar
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'btn btn-primary theory-sidebar-toggle';
        toggleBtn.innerHTML = '<i class="bi bi-list"></i>';
        toggleBtn.title = 'Показать/скрыть навигацию';
        toggleBtn.setAttribute('aria-label', 'Показать/скрыть навигацию');

        // Создаем sidebar
        const sidebar = document.createElement('aside');
        sidebar.className = 'theory-sidebar';
        sidebar.id = 'theorySidebar';

        const sidebarTitle = document.createElement('h5');
        sidebarTitle.innerHTML = '<i class="bi bi-menu-button-wide me-2"></i>Содержание';
        
        const navList = document.createElement('ul');
        navList.className = 'theory-nav-list';

        // Находим все заголовки в контенте
        const headings = markdownBody.querySelectorAll('h1, h2, h3, h4, h5, h6');
        
        if (headings.length === 0) {
            // Если заголовков нет, скрываем sidebar
            sidebar.classList.add('hidden');
            toggleBtn.innerHTML = '<i class="bi bi-list"></i>';
        }

        let headingIndex = 0;
        const headingElements = [];

        headings.forEach(heading => {
            // Добавляем id к заголовку если его нет
            if (!heading.id) {
                const text = heading.textContent.trim().toLowerCase();
                const id = 'heading-' + headingIndex++;
                heading.id = id;
            }

            headingElements.push({
                element: heading,
                id: heading.id,
                text: heading.textContent.trim(),
                level: parseInt(heading.tagName.charAt(1))
            });

            // Создаем элемент навигации
            const navItem = document.createElement('li');
            navItem.className = 'theory-nav-item';

            const navLink = document.createElement('a');
            navLink.className = 'theory-nav-link';
            navLink.href = '#' + heading.id;
            navLink.textContent = heading.textContent.trim();
            navLink.dataset.targetId = heading.id;

            // Добавляем отступ в зависимости от уровня заголовка
            const indentClass = 'nav-indent-' + Math.min(heading.tagName.charAt(1) - 1, 3);
            navLink.classList.add(indentClass);

            navItem.appendChild(navLink);
            navList.appendChild(navItem);
        });

        sidebar.appendChild(sidebarTitle);
        sidebar.appendChild(navList);

        // Оборачиваем контент
        const contentWrapper = document.createElement('div');
        contentWrapper.className = 'theory-content-wrapper';
        
        // Перемещаем markdown-body в wrapper
        markdownBody.parentNode.insertBefore(navContainer, markdownBody);
        navContainer.appendChild(sidebar);
        navContainer.appendChild(contentWrapper);
        contentWrapper.appendChild(markdownBody);

        // Добавляем кнопку в body, чтобы она всегда была поверх страницы
        document.body.appendChild(toggleBtn);

        // Обработчик клика по кнопке переключения
        toggleBtn.addEventListener('click', function() {
            sidebar.classList.toggle('hidden');
            
            if (sidebar.classList.contains('hidden')) {
                toggleBtn.innerHTML = '<i class="bi bi-list"></i>';
                toggleBtn.title = 'Показать навигацию';
            } else {
                toggleBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
                toggleBtn.title = 'Скрыть навигацию';
            }
        });

        // Плавная прокрутка при клике на якорь
        navList.querySelectorAll('.theory-nav-link').forEach(link => {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                const targetId = this.dataset.targetId;
                const targetElement = document.getElementById(targetId);
                
                if (targetElement) {
                    const offset = 120; // Отступ сверху
                    const elementPosition = targetElement.getBoundingClientRect().top + window.pageYOffset;
                    const offsetPosition = elementPosition - offset;

                    window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                    });

                    // Обновляем активный класс
                    updateActiveHeading(targetId);
                }
            });
        });

        // Отслеживание прокрутки для подсветки активного заголовка
        let ticking = false;
        
        window.addEventListener('scroll', function() {
            if (!ticking) {
                window.requestAnimationFrame(function() {
                    updateActiveHeadingFromScroll();
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });

        // Функция обновления активного заголовка при прокрутке
        function updateActiveHeadingFromScroll() {
            const scrollPosition = window.pageYOffset + 150; // Смещение для учета header

            let currentActive = null;

            // Проходим по всем заголовкам и находим текущий
            for (let i = headingElements.length - 1; i >= 0; i--) {
                const heading = headingElements[i];
                const elementTop = heading.element.offsetTop;

                if (scrollPosition >= elementTop) {
                    currentActive = heading.id;
                    break;
                }
            }

            // Если ни один заголовок не найден, берем первый
            if (!currentActive && headingElements.length > 0) {
                currentActive = headingElements[0].id;
            }

            if (currentActive) {
                updateActiveHeading(currentActive);
            }
        }

        // Функция обновления активного класса в навигации
        function updateActiveHeading(activeId) {
            navList.querySelectorAll('.theory-nav-link').forEach(link => {
                link.classList.remove('active');
                
                if (link.dataset.targetId === activeId) {
                    link.classList.add('active');
                    
                    // Прокручиваем sidebar чтобы активный элемент был виден
                    const linkTop = link.offsetTop;
                    const linkHeight = link.offsetHeight;
                    const sidebarScrollTop = sidebar.scrollTop;
                    const sidebarHeight = sidebar.offsetHeight;

                    if (linkTop < sidebarScrollTop) {
                        sidebar.scrollTop = linkTop;
                    } else if (linkTop + linkHeight > sidebarScrollTop + sidebarHeight) {
                        sidebar.scrollTop = linkTop + linkHeight - sidebarHeight;
                    }
                }
            });
        }

        // Инициализация активного элемента при загрузке
        setTimeout(updateActiveHeadingFromScroll, 100);
    }

    // Запуск после загрузки DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheoryNavigation);
    } else {
        initTheoryNavigation();
    }
})();
