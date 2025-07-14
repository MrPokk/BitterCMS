# Документация BitterCMS

**BitterCMS** — это фреймворк для управления контентом в Unity на основе ECS-подхода.

### Главная Особенность

Все Entity могут представляться в двух форматах: XML или формате C#-кода. Для просмотра XML создан собственный инспектор.

## Ключевые компоненты
### Система сущностей
#### CMSEntityCore

CMSEntityCore — Базовый класс для всех сущностей.

```csharp
// Пример использования:
[Serializable]
public class ExampleEntity : CMSEntityCore
{
    public ExampleEntity()
    {
        AddComponent<ExampleComponent>();
    }
}
```

### Система компонентов
#### IEntityComponent - Интерфейс для компонента

Является контейнером для данных пользователя и не должен содержать логику (допускаются минимальные хелперы, но не куски основной логики):

```csharp
// Пример использования:
[Serializable]
public class ExampleComponent : IEntityComponent
{
    public int Data;
}
```

#### Базовые компоненты
#### ViewComponent

Этот компонент нужен для связи CMSEntity и Unity, является лишь отображением Entity через Monobehaviour Unity.

```csharp
// Пример использования:
[Serializable]
public class ExampleEntity : CMSEntityCore
{
    public ExampleEntity()
    {
        AddComponent<ViewComponent>().Init(new (ViewDatabase.Get<ExampleView>()));
        AddComponent<ExampleComponent>();
    }
}
```
ВАЖНО!
ExampleView — Это класс наследуемый от CMSViewCore и он является **MonoBehaviour** (Prefab на котором находится ExampleView)

_Все CMSViewCore, которые можно использовать, отображаются в CMSEditor.
Берутся по пути Resources/Prefabs/Views/_

### Система презентеров
#### CMSPresenterCore

Управляет связями "сущность — представление" и предоставляет функционал для создания/фильтрации сущностей.

#### Особенности

Есть возможность создать новый Entity либо создать Entity из XML-файла.
CMSRuntime — это глобальный список всех существующих Presenter.

**SpawnFromDB — берет сущность из XML, если не может, то создаёт новую.**

###### Создания сущности из XML 
```csharp
// Создание сущности из XML:
var entity = CMSRuntimer.GetPresenter<ExamplePresent>().SpawnFromDB(typeof(ExampleEntity));
```

###### Создания новой сущности (Вызывает конструктор у данного класса)
```csharp
// Создание сущности:
var entity = CMSRuntimer.GetPresenter<ExamplePresent>().SpawnEntity(typeof(ExampleEntity));
```

#### Реализация Presenter
Для создать Presenter класс надо унаследовать от CMSPresenterCore.

#### Особенности
Также есть возможность **ограничить типы,** которые может реализовывать Presenter (см.пример)

```csharp
// Без ограничений по типам
public class ExamplePresent : CMSPresenterCore
{}

// С ограничений по типам
public class ExamplePresentLimited : CMSPresenterCore
{
    public ExamplePresentLimited() : base(typeof(ExampleEntity))
    { }
}
```

#### Фильтрация сущностей
Вы можете отфильтровать сущности в Presenter по компонентам:
```csharp
// Все сущности с HealthComponent но без ShieldComponent
CMSRuntimer.GetPresenter<ExamplePresent>().FilterEntities(
       requiredComponents: new[] { typeof(HealthComponent) },
       excludedComponents: new[] { ShieldComponent });

// Все сущности с HealthComponent и ShieldComponent
CMSRuntimer.GetPresenter<ExamplePresent>().FilterEntities(typeof(HealthComponent), typeof(ShieldComponent));
```

### Система интеракций
#### InteractionCore

Это система, через которую реализуется основная логика игры. Все базовые действия представлены в виде интерфейсов.

Часто используемые:
- IEnterInStart
- IEnterInUpdate
- IEnterInUpdate
- IExitInGame

Также при помощи Priority можно указывать порядок выполнения. Базовый порядок это Средний (Priority.Medium).
Пример реализации Interaction и базовых действий.
```csharp
public class ExampleInteraction : InteractionCore, IEnterInUpdate, IEnterInStart, IExitInGame, IEnterInPhysicUpdate
{
    // Указания приоритета
    public override Priority PriorityInteraction => Priority.High;

    public void Start()
    { }

    public void Update(float timeDelta)
    { }

    public void PhysicUpdate(float timeDelta)
    { }

    public void Stop()
    { }
}
```

#### Добавления своих интеракций

Пример добавления свой интеракции в Root.
```csharp
public class Root : RootMonoBehavior
{
    protected override void GlobalStart()
    {
        // Получения интеракций и вызов у них метода
        foreach (var interaction in InteractionCache<IExampleInteraction>.AllInteraction)
        {
            interaction.ExampleVoid();
        }
    }
    
    //Добавления новой интеракции
    protected override void FindExtraInteraction(Interaction interaction)
    {
        interaction.FindAll<IExampleInteraction>();
    }
}

// Интеракция
public interface IExampleInteraction
{
    public void ExampleVoid();
}
```

## Интеграция с редактором
### Возможности редактора

BitterCMS предоставляет несколько инструментов редактора:

1. **Просмотр баз данных** - Все зарегистрированные представления и сущности.
2. **Инспектор** - Редактирование XML-файлов сущностей.
3. **Настройки** - Конфигурация BitterCMS.

Доступ через меню `CMS/CMS CENTER`.

## ВАЖНЫЙ ОСОБЕННОСТИ 
1. Все Файлы XML берутся по пути _Resources/CMS/Entities/_
2. Все префабы CMSViewCore берутся по пути _Resources/Prefabs/Views/_
