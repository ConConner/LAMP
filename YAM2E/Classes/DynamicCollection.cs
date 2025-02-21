using System;
using System.Collections.Generic;
using System.Linq;

namespace LAMP.Classes;


public class DynamicCollection<T>
{
    public DynamicCollection(List<T> data, bool strict = false) : this(strict)
    {
        this.Data = data;
    }
    public DynamicCollection(bool strict = false)
    {
        enforeRectangleLayout = strict;
    }

    // Fields
    private List<T> Data = new List<T>();
    private bool enforeRectangleLayout = false;

    // Properties
    public int Size
    {
        get => Data.Count;
    }

    public int Width 
    {
        get => width;
        set
        {
            if (enforeRectangleLayout && (double)Size / (double)value % 1 != 0) throw new NotDivisibleIntoRectangleException("Width has to be set in a way it creates a whole rectangle");
            width = value;
        }
    }
    private int width;

    public int Height => Size / Width;

    //Indexers
    public T this[int x, int y]
    {
        get
        {
            validateIndex(x, y);
            return Data[y * Width + x];
        }
        set
        {
            validateIndex(x, y);
            Data[y * Width + x] = value;
        }
    }
    public T this[int x]
    {
        get => Data[x];
        set => Data[x] = value;
    }

    //Methods
    private void validateIndex(int x, int y)
    {
        if (Width < 1) throw new WidthNotSetException($"Width must not be less than 1. Width is {Width}.");
        if (x >= Width) throw new IndexOutOfRangeException($"{x} is bigger than the Width: {Width}");
        if (y >= Height) throw new IndexOutOfRangeException($"{y} is bigger than the Height: {Height}");
        if (y * Width + Height >= Size) throw new IndexOutOfRangeException($"Index is out of bounds of the Collection");
    }

    //Operators
    public static implicit operator DynamicCollection<T>(T[] inputList) => new DynamicCollection<T>(inputList.ToList());
    public static explicit operator DynamicCollection<T>(T[,] inputArray)
    {
        throw new NotImplementedException();
    }
}



public class WidthNotSetException : Exception
{
    public WidthNotSetException(string message = "") : base(message) { }
}
public class NotDivisibleIntoRectangleException : Exception
{
    public NotDivisibleIntoRectangleException(string message = "") : base(message) { }
}