using System;

namespace LAMP.Interfaces;

public interface IAction
{
    public void Do();
    public void Undo();
    public String Description { get; }
}
