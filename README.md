# Документация BitterCMS

BitterCMS — это фреймворк для управления контентом в Unity на основе ECS подхода

### Особенности 

Все Entity могут представлятся в двух форматах XML или формате C# кода для просмотра XML создан собственный инспектор

## Ключевые компоненты

### Система сущностей

#### CMSEntityCore
Базовый класс для всех сущностей;


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
Этот компонен нужен для связи CMSEntity и Unity он связывается отображения тоесть Prefab с данными 

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

ExampleView — Это класс наследуеммый от CMSViewCore и он является **MonoBehaviour** (Prefab на котором находится ExampleView)

_ВСЕ CMSViewCore которые можно использовать отображаются в CMSEditor_
_Берутся по пути Resources/Prefabs/Views/_

### Система презентеров

#### CMSPresenterCore
Управляет связями сущность — представление и предоставляет функционал для создания/фильтрации сущностей.

##### Особенность
Есть возможность создать новый Entity либо создать Еntity из XML файла.
CMSRuntimer — это глобальный список всех существующих Presenter


**SpawnFromDB — Беред сущность из XML если не может то создаёт новую**

###### Создания сущности из XML 
```csharp
// Создание сущности из XML:
var entity = CMSRuntimer.GetPresenter<ExamplePresent>().SpawnFromDB(typeof(ExampleEntity));
```

###### Создания новой сущности
```csharp
// Создание сущности:
var entity = CMSRuntimer.GetPresenter<ExamplePresent>().SpawnEntity(typeof(ExampleEntity));
```

#### Реализация Presenter
Вы можете **ограничить типа** которые может создавать Presenter

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
Вы можете отфильтровать сущностей в Presenter по компонентам 

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
Это система через которую реализуется основаня логика игры. Все базовые действие представленны ввиде интерфейсов

Часто используемыен:
- IEnterInStart
- IEnterInUpdate
- IEnterInUpdate
- IExitInGame

Также при помощи Priority можно узавывать важность Interaction

Пример реализации Interaction и базовых действий 
```csharp
public class ExampleInteraction : InteractionCore, IEnterInUpdate, IEnterInStart, IExitInGame, IEnterInPhysicUpdate
{
    public override Priority PriorityInteraction => Priority.Medium;

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

Пример добавления свой интеракции в Root
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
    
    //Добавления новой инеракци
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

CMS предоставляет несколько инструментов редактора:

1. **Просмотр баз данных** - Все зарегистрированные представления и сущности
2. **Инспектор** - Редактирование XML-файлов сущностей
3. **Настройки** - Конфигурация CMS

Доступ через меню `CMS/CMS CENTER`.


## ВАЖНЫЙ ОСОБЕННОСТИ 
1. Все Файлы XML берутся по пути Resources/CMS/Entities/
2. Все префабы View берутся по пути Resources/Prefabs/Views/
