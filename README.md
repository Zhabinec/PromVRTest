# Material Accumulation

Небольшая законченная Unity-система накопления материала: один общий Mesh отображает независимое CPU height field, а движущаяся полусферическая зона непрерывно добавляет объём по всей траектории.

![Polished demo scene](Documentation/Images/material-accumulation-preview.png)

Статус: механика, production-polish сцены/UI, автоматические тесты и стандартная Windows-сборка проверены. Перед отправкой остаются длительный ручной Profiler-прогон и запись короткого видео.

## Запуск

1. Открыть проект в Unity `6000.3.10f1`.
2. Открыть `Assets/_Project/Scenes/MaterialAccumulationDemo.unity`.
3. Запустить Play Mode.

Демо является первой и единственной сценой в Build Settings. Standalone собирается стандартными средствами Unity через `File → Build Profiles`.

## Управление

| Клавиша | Действие |
|---|---|
| `WASD` | Перемещение полусферической зоны |
| `Space` | Накопление материала |
| `R` | Полный сброс поверхности |
| `Esc` | Выход из standalone build |
| `RU / EN` в верхней панели | Переключение языка HUD; выбор сохраняется между запусками |

Скорость движения, базовый радиус, амплитуда, частота, `AnimationCurve` и скорость накопления настраиваются на `Brush Controller` и `Accumulation Surface` в Inspector.

## Хранение и обновление Mesh

Единственный источник истины — заранее выделенный `float[]` в `HeightField`. Mesh не хранит игровое состояние и является только представлением массива высот.

Для образца внутри полусферы:

```text
ceiling   = sqrt(radius² - distance²)
candidate = min(oldHeight + accumulationSpeed * exposureTime, ceiling)
newHeight = max(oldHeight, candidate)
```

Финальный `max` делает накопление монотонным: меньшая или переместившаяся зона не срезает старый материал. Sweep между предыдущим и текущим состоянием дискретизируется по размеру ячейки; крайние stamps получают половинный временной вес, поэтому сумма времени подшагов равна исходному `deltaTime`.

Топология, UV и буферы создаются один раз. После воздействия Core возвращает `GridRect`; Unity-слой расширяет его на одну ячейку для зависимых нормалей и обновляет только dirty-строки через `Mesh.SetVertexBufferData`. На горячем пути нет LINQ, новых коллекций, форматирования строк или создания Unity objects.

## Архитектура

```text
Input System / AnimationCurve / Time
                  ↓
       BrushControllerBehaviour ← DemoHudBehaviour (read-only state)
                  ↓
     AccumulationSurfaceBehaviour
                  ↓
        HemisphereAccumulator
                  ↓
             HeightField
                  ↓ dirty GridRect
        HeightFieldMeshView → one Mesh
```

- `PromVR.MaterialAccumulation.Core` — чистый C# с `noEngineReferences: true`.
- `PromVR.MaterialAccumulation.Unity` — ввод, lifecycle, coordinate conversion и Mesh view.
- `PromVR.MaterialAccumulation.Presentation` — безаллокционное per-frame обновление HUD, локализация RU/EN и сохранение выбранного языка; слой зависит только от read-only API controller-а.
- Preview полусферы — один переиспользуемый объект; порции материала не создают GameObject/Mesh.
- Simulation и Mesh sync размечены отдельными `ProfilerMarker`.

Прямые package-зависимости сокращены до реально используемых официальных пакетов: URP, Input System, uGUI и Test Framework; IDE integrations остаются Editor-only.

## Проверенные результаты

Проверено 2026-08-25 на Unity `6000.3.10f1`, Windows, NVIDIA GeForce RTX 4070 Laptop GPU:

| Проверка | Результат |
|---|---|
| Компиляция Core/Unity/Presentation/tests | успешно |
| Edit Mode | `13/13 Passed` |
| Play Mode | `2/2 Passed` |
| Steady hot-path allocation guard | `0 B` за 90 последовательных swept updates после прогрева |
| Runtime object stability | число material GameObject и runtime Mesh не растёт |
| Windows Standalone Build | `95.1 MB`, `145.5 s` на холодной изолированной копии, успешно |
| Test-only visual render | успешно, кадр выше |

Allocation guard измеряет синхронный участок `Core + dirty Mesh sync` на текущем потоке. Это сильная автоматическая регрессия, но не подменяет обязательный финальный 60-секундный Profiler capture без Deep Profile.

## Структура проекта

```text
Assets/_Project/
├── Materials/
├── Scenes/
├── Scripts/
│   ├── Presentation/
│   └── Runtime/
│       ├── Core/
│       └── Unity/
├── Settings/Rendering/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

Полный технический источник правды — [`PROJECT.md`](PROJECT.md), правила работы — [`AGENTS.md`](AGENTS.md), измеренное время — [`TIMELOG.md`](TIMELOG.md).

## Основные ограничения

- Height field хранит одну высоту на XZ-точку: нависания, пещеры и раздельные слои невозможны.
- Минимальная деталь и зубчатость контура ограничены шагом сетки `128 × 128` quads.
- Это визуальная модель накопления, а не физика сыпучего материала с осыпанием и сохранением объёма.
- CPU-стоимость растёт с resolution, площадью зоны и длиной sweep за кадр.
- Базовая версия рассчитана на одну кисть и не обновляет MeshCollider.
- Surface ожидает единичный scale, чтобы локальные метры и радиус совпадали.
- Статичный тёмный cover скрывает первые `2 mm` высоты, чтобы нулевой оранжевый Mesh не выглядел уже накопленным материалом.

## Разумные улучшения

1. Dirty tiles/chunks для больших поверхностей.
2. Burst Jobs с persistent buffers после измерения CPU bottleneck.
3. GPU height map для существенно больших разрешений.
4. Сериализация height field и undo/redo.
5. Несколько brush profiles при сохранении монотонного Core-контракта.
6. Упрощённый collider с редким обновлением, если появится gameplay-физика.

## Осталось перед отправкой

- записать 60–90 секунд видео по сценарию из `PROJECT.md`;
- сделать 60-секундный Editor/Development Build Profiler capture без Deep Profile и записать CPU median/p95;
- добавить ссылку на видео и фактические profiler numbers в этот README.
