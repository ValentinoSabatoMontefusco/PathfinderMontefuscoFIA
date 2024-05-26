using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFrontier<T>
{
    
    void Add(T item);
    int Count();
    T Extract();

    bool Contains(T item);
    void UpdateItem(T item);
}

public class StackFrontier : IFrontier<GridNode>
{
    private Stack<GridNode> stackFrontier;
    
    
    public StackFrontier()
    {
        this.stackFrontier = new Stack<GridNode>();
    }
    
    public void Add(GridNode node)
    {
        stackFrontier.Push(node);
    }

    public int Count()
    {
        return stackFrontier.Count;
    }

    public GridNode Extract()
    {
        return stackFrontier.Pop();
    }

    public bool Contains(GridNode node)
    {
        return stackFrontier.Contains(node);
    }

    public void UpdateItem(GridNode node)
    {
        throw new System.NotImplementedException();
    }
}

public class QueueFrontier : IFrontier<GridNode>
{
    private Queue<GridNode> queueFrontier;
    public QueueFrontier()
    {
        queueFrontier = new();
    }
    public void Add(GridNode node)
    {
        queueFrontier.Enqueue(node);
    }

    public int Count()
    {
       
        return queueFrontier.Count;
    }

    public GridNode Extract()
    {
        return queueFrontier.Dequeue();
    }

    public bool Contains(GridNode node)
    {
        return queueFrontier.Contains(node);
    }

    public void UpdateItem(GridNode item)
    {
        throw new System.NotImplementedException();
    }
}

public class HeapFrontier : IFrontier<GridNode>
{
    private Heap<GridNode> heapFrontier;
    public HeapFrontier()
    {
        heapFrontier = new();
    }

    public void Add(GridNode node)
    {
        heapFrontier.Add(node);
    }

    public int Count()
    {
        return heapFrontier.Count;
    }

    public GridNode Extract()
    {
        return heapFrontier.Extract();
    }

    public bool Contains(GridNode node)
    {
        return heapFrontier.Contains(node);
    }
    public void UpdateItem(GridNode node)
    {
        heapFrontier.UpdateItem(node);
    }
}
