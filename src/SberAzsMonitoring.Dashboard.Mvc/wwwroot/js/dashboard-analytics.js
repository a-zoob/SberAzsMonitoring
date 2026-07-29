document.addEventListener('DOMContentLoaded', function () {
    const filterForm = document.getElementById('analyticsFilterForm');
    const tableBody = document.getElementById('tableDataBody');
    const chartContainer = document.getElementById('analyticsChartContainer');
    const btnLoadData = document.getElementById('btnLoadData');

    if (!filterForm) return;

    filterForm.addEventListener('submit', function (event) {
        event.preventDefault();

        const selectedRegion = document.getElementById('regionSelect').value;
        const selectedFuelType = document.getElementById('fuelTypeSelect').value;

        if (!selectedRegion || !selectedFuelType) {
            alert('Пожалуйста, выберите регион и марку топлива.');
            return;
        }

        // Блокируем элементы управления на время AJAX-запроса
        btnLoadData.disabled = true;
        btnLoadData.innerHTML = '⚡ Загрузка среза...';
        tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-4"><div class="spinner-border text-success" role="status"></div></td></tr>`;
        chartContainer.innerHTML = `<div class="spinner-border text-secondary" role="status"></div>`;

        // Формируем URL для локального прокси-метода контроллера
        const url = `/Analytics/GetLatestAvailability?region=${encodeURIComponent(selectedRegion)}&fuelType=${encodeURIComponent(selectedFuelType)}`;

        fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Ошибка сети или внутренний сервис мониторинга недоступен.');
                }
                return response.json();
            })
            .then(data => {
                renderTable(data);
                renderChart(data);
            })
            .catch(error => {
                console.error('Error fetching analytics:', error);
                tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-danger">⚠️ Не удалось загрузить оперативные данные: ${error.message}</td></tr>`;
                chartContainer.innerHTML = `<span class="text-danger">Ошибка построения графика</span>`;
            })
            .finally(() => {
                // Возвращаем кнопку в исходное состояние
                btnLoadData.disabled = false;
                btnLoadData.innerHTML = '⚡ Показать данные за текущий момент';
            });
    });

    // Функция отрисовки таблицы «Светофор» с выводом Названия и Адреса (без ID АЗС)
    function renderTable(items) {
        if (!items || items.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-muted">По данным критериям ограничения отсутствуют (Все АЗС работают штатно)</td></tr>`;
            return;
        }

        let htmlRows = '';
        items.forEach(item => {
            let rowClass = '';
            let statusText = item.availabilityStatus || 'Неизвестно';
            const statusLower = statusText.toLowerCase();

            // цветовое кодирование строк на основе статусов и флага доступности
            // if (statusLower.includes('критическ') || statusLower.includes('отсутствует') || !item.isAvailable) {
            //     rowClass = 'table-danger';
            // } else if (statusLower.includes('мало') || statusLower.includes('резерв') || statusLower.includes('внимание') || item.limitLiters > 0) {
            //     rowClass = 'table-warning';
            // } else {
            //     rowClass = 'table-success';
            // }
            if (statusLower.includes('stale') || !item.isAvailable) {
                rowClass = 'table-danger';
            } else if (statusLower.includes('unknown') || item.limitLiters > 0) {
                rowClass = 'table-warning';
            } else {
                rowClass = 'table-success';
            }

            // Форматирование даты из ClickHouse timestamp
            const dateStr = item.timestamp ? new Date(item.timestamp).toLocaleString('ru-RU') : 'Текущая';

            htmlRows += `
                <tr class="${rowClass}">
                    <td><strong>${dateStr}</strong></td>
                    <td>
                        <div class="fw-bold">${item.stationName || 'Автозаправочная станция'}</div>
                        <div class="small text-muted">${item.stationAddress || 'Адрес не указан'}</div>
                    </td>
                    <td><span class="badge bg-secondary">${item.fuelType}</span></td>
                    <td>${statusText}</td>
                    <td>${item.limitLiters > 0 ? `Лимит: ${item.limitLiters} л.` : 'Без лимитов'}</td>
                </tr>
            `;
        });

        tableBody.innerHTML = htmlRows;
    }

    // Функция отрисовки долевого графика распределения состояний АЗС
    function renderChart(items) {
        if (!items || items.length === 0) {
            chartContainer.innerHTML = `<div class="text-success text-center">💯 100% АЗС региона работают в штатном режиме</div>`;
            return;
        }

        let criticalCount = 0;
        let warningCount = 0;
        let normalCount = 0;

        items.forEach(item => {
            const status = (item.availabilityStatus || '').toLowerCase();
            if (status.includes('критическ') || status.includes('отсутствует') || !item.isAvailable) {
                criticalCount++;
            } else if (status.includes('мало') || status.includes('резерв') || status.includes('внимание') || item.limitLiters > 0) {
                warningCount++;
            } else {
                normalCount++;
            }
        });

        const total = criticalCount + warningCount + normalCount;
        const critPercent = Math.round((criticalCount / total) * 100);
        const warnPercent = Math.round((warningCount / total) * 100);
        const normPercent = 100 - critPercent - warnPercent;

        // Построение прогресс-бара Bootstrap 5 и CSS 
        chartContainer.innerHTML = `
            <div class="w-100 p-3">
                <h6 class="text-center mb-3 text-secondary">Долевое распределение состояний АЗС в выбранном срезе (Всего объектов: ${total})</h6>
                <div class="progress mb-4" style="height: 35px; font-size: 14px; font-weight: bold;">
                    ${critPercent > 0 ? `<div class="progress-bar bg-danger" role="progressbar" style="width: ${critPercent}%">${critPercent}% Критично</div>` : ''}
                    ${warnPercent > 0 ? `<div class="progress-bar bg-warning text-dark" role="progressbar" style="width: ${warnPercent}%">${warnPercent}% Внимание</div>` : ''}
                    ${normPercent > 0 ? `<div class="progress-bar bg-success" role="progressbar" style="width: ${normPercent}%">${normPercent}% Норма</div>` : ''}
                </div>
                <div class="d-flex justify-content-center gap-4 small fw-semibold">
                    <div><span class="badge bg-danger">&nbsp;</span> Критические сбои: ${criticalCount}</div>
                    <div><span class="badge bg-warning">&nbsp;</span> Лимиты/Предупреждения: ${warningCount}</div>
                    <div><span class="badge bg-success">&nbsp;</span> Работают штатно: ${normalCount}</div>
                </div>
            </div>
        `;
    }
});
