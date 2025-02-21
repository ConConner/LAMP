using System.Collections.Generic;
using LAMP.Interfaces;

namespace LAMP.Actions;

internal class GroupedAction : IAction
{
    public List<IAction> Actions = new List<IAction>();
    public string Description
    {
        get
        {
            string name = Actions[0].Description;
            return $"{name} ({Actions.Count})";
        }
    }

    public void Do()
    {
        foreach (IAction action in Actions) { action.Do(); }
    }

    public void Undo()
    {
        Actions.Reverse();
        foreach (IAction action in Actions) { action.Undo(); }
        Actions.Reverse();
    }
}
