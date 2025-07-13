using BitterCMS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

public class Interaction
{
    public List<InteractionCore> InteractionList { get; private set; } = new List<InteractionCore>();

    public void Init()
    {
        FindAllBaseInteraction();
    }
    private void FindAllBaseInteraction()
    {
        var allElement = ReflectionUtility.FindAllImplement<InteractionCore>();
        foreach (var element in allElement)
        {
            InteractionList.Add(Activator.CreateInstance(element) as InteractionCore);
        }
    }

    public List<T> FindAll<T>()
    {
        return InteractionCache<T>.FindAll(this);
    }

}
public static class InteractionCache<T>
{
    public static List<T> AllInteraction;
    private const int COUNT_INTERACTION = 64;

    public static List<T> FindAll(Interaction interact = null)
    {
        if (AllInteraction != null) 
            return AllInteraction;
        
        AllInteraction = new List<T>(COUNT_INTERACTION);
        if (interact == null)
            return AllInteraction;

        foreach (var element in interact.InteractionList)
        {
            if (element is T activator)
                AllInteraction.Add(activator);

            AllInteraction.Sort((x, y) => (int)((x as InteractionCore)!).PriorityInteraction -
                                          (int)((y as InteractionCore)!).PriorityInteraction);
        }
        return AllInteraction;
    }

    public static T GetInteraction()
    {
        return AllInteraction.FirstOrDefault(interaction => interaction != null);
    }
}
