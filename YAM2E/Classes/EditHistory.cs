using LAMP.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace LAMP.Classes;

public class EditHistory
{
    //Fields
    private Stack<IAction> UndoStack = new();
    private Stack<IAction> RedoStack = new();

    public event System.EventHandler AddedAction;
    public event System.EventHandler UndoRedo;

    public bool canUndo => UndoStack.Count > 0;
    public bool canRedo => RedoStack.Count > 0;

    public string[] undoDescriptions
    {
        get
        {
            List<IAction> stackList = UndoStack.ToList();
            string[] result = new string[UndoStack.Count];
            for (int i = 0; i < UndoStack.Count; i++) result[i] = stackList[i].Description;
            return result;
        }
    }
    public string[] redoDescriptions
    {
        get
        {
            List<IAction> stackList = RedoStack.ToList();
            string[] result = new string[RedoStack.Count];
            for (int i = 0; i < RedoStack.Count; i++) result[i] = stackList[i].Description;
            return result;
        }
    }

    public void Undo()
    {
        if (!canUndo) return;

        IAction last = UndoStack.Pop();
        last.Undo();
        RedoStack.Push(last);

        UndoRedo?.Invoke(this, new System.EventArgs());
    }

    public void Redo()
    {
        if (!canRedo) return;

        IAction last = RedoStack.Pop();
        last.Do();
        UndoStack.Push(last);

        UndoRedo?.Invoke(this, new System.EventArgs());
    }

    public void AddActionToHistory(IAction action)
    {
        UndoStack.Push(action);
        RedoStack.Clear();
        AddedAction?.Invoke(this, new System.EventArgs());
    }
}
