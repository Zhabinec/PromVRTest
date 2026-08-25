# Material Accumulation

Рабочий Unity-прототип накопления материала на регулярной поверхности с изменением одного общего Mesh. Материал хранится в независимом height field, сохраняется между воздействиями и добавляется по всей траектории движущейся полусферической зоны.

Текущий статус: механика, демо-сцена и автоматические тесты работают. Профилирование steady state, standalone build, финальный polish и видео ещё не завершены.

## Запуск

1. Открыть проект в Unity `6000.3.10f1`.
2. Открыть `Assets/_Project/Scenes/MaterialAccumulationDemo.unity`.
3. Запустить Play Mode.

Сцена уже добавлена первой и единственной сценой в Build Settings.

## Управление

| Клавиша | Действие |
|---|---|
| `WASD` | Перемещение полусферической зоны |
| `Space` | Накопление материала |
| `R` | Сброс поверхности |
| `Esc` | Выход из standalone build |

Настраиваемые параметры движения, радиуса, `AnimationCurve`, частоты и скорости накопления находятся на `Brush Controller` и `Accumulation Surface` в Inspector.

## Как устроено накопление

Core хранит высоты в одном заранее выделенном `float[]`. Mesh не является источником состояния и только отображает изменившуюся часть height field.

Для ячейки внутри текущего диска вычисляется верхняя граница полусферы:

```text
ceiling = sqrt(radius² - distance²)
candidate = min(oldHeight + accumulationSpeed * exposureTime, ceiling)
newHeight = max(oldHeight, candidate)
```

Последний `max` гарантирует, что меньшая или переместившаяся полусфера никогда не срежет ранее накопленный материал.

Движение обрабатывается как sweep между предыдущим и текущим состоянием. Центр и радиус сэмплируются с шагом, зависящим от размера ячейки сетки. Крайние stamps получают половинный временной вес, поэтому сумма времени всех подшагов равна исходному `deltaTime` и накопление не ускоряется из-за дополнительной дискретизации.

## Архитектура

```text
Input System / AnimationCurve / Time
                  ↓
       BrushControllerBehaviour
                  ↓
     AccumulationSurfaceBehaviour
                  ↓
        HemisphereAccumulator
                  ↓
             HeightField
                  ↓ dirty region
        HeightFieldMeshView → Mesh
```

- `PromVR.MaterialAccumulation.Core` собран с `noEngineReferences: true`.
- Unity runtime отвечает только за ввод, жизненный цикл и представление.
- Один runtime Mesh отображает весь материал.
- Топология и UV создаются один раз; dirty-строки vertex buffer обновляются через `SetVertexBufferData`.
- Нормали пересчитываются вручную только вокруг изменённой области.
- Preview полусферы — один постоянный объект и не является порцией материала.

Подробные решения, ограничения и критерии сдачи находятся в `PROJECT.md`.

## Структура проекта

```text
Assets/_Project/
├── Art/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Editor/
│   └── Runtime/
│       ├── Core/
│       └── Unity/
├── Settings/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Демо-сцену и материалы можно осознанно пересобрать через меню:

```text
Tools → Material Accumulation → Rebuild Demo Scene
```

Shortcut: `Ctrl+Shift+G`. Команда заменяет демо-сцену, поэтому перед ручным запуском предлагает сохранить текущие изменения.

## Подтверждённые проверки

Проверено 2026-08-25 на Unity `6000.3.10f1`:

- компиляция Core, Unity, Editor и test assemblies — успешно;
- Edit Mode: `13/13 Passed`;
- Play Mode: `1/1 Passed`;
- Play Mode smoke test загрузил демо-сцену, создал Mesh на `16 641` вершину, применил sweep и reset без исключений.

Пример запуска тестов — без `-quit`, потому что Unity Test Framework завершает процесс самостоятельно:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' `
  -batchmode -automated `
  -projectPath '<PROJECT_PATH>' `
  -runTests -testPlatform EditMode `
  -testResults '<PROJECT_PATH>\Logs\EditModeResults.xml' `
  -logFile '<PROJECT_PATH>\Logs\EditModeTests.log'
```

Пока не проверены и не заявляются как готовые:

- отсутствие managed allocations в длительном Profiler-прогоне;
- standalone Development Build;
- производительность на нескольких разрешениях;
- финальное видео.

## Основные ограничения

- Height field не поддерживает нависания, пещеры и отдельные вертикальные слои.
- Минимальная деталь ограничена шагом регулярной сетки.
- Это визуальное накопление, а не симуляция сыпучего материала с осыпанием и сохранением объёма.
- CPU-стоимость растёт с resolution, площадью кисти и длиной sweep за кадр.
- Базовая версия рассчитана на одну кисть и не обновляет MeshCollider.
- Surface ожидает единичный scale, чтобы локальные метры и радиус совпадали.

Полный список ограничений и направлений развития находится в `PROJECT.md`.
