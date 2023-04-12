using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Heap<T> :ICollection where T : IHeapItem<T>
{

    T[] items;
    private int currentItemCount = 0;

    public Heap(int maxSize) {

        items = new T[maxSize];
    }

    public void Add (T item)
    {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        Sortup(item);
        currentItemCount++;

    }

    public void Sortup(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (item.CompareTo(items[parentIndex]) > 0)
        {
            Swap(item, items[parentIndex]);
            parentIndex = (item.HeapIndex - 1) / 2;
        }

        
    }

    public void UpdateItem(T item)
    {
        Sortup(item);
    }

    public T Extract()
    {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        Sortdown(items[0]);


        return firstItem;
    }

    public void Sortdown(T item)
    {
        int leftChildIndex;
        int rightChildIndex;
        int swapIndex;

        while (true)
        {
            leftChildIndex = item.HeapIndex * 2 + 1;
            rightChildIndex = item.HeapIndex * 2 + 2;
            swapIndex = 0;

            if (leftChildIndex < currentItemCount)
            {
                swapIndex = leftChildIndex;
                if (rightChildIndex < currentItemCount)
                    if (items[leftChildIndex].CompareTo(items[rightChildIndex]) < 0)
                        swapIndex = rightChildIndex;

                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else
                    return;
            }
            else
                return;

            
            
        }

    }

    public bool Contains (T item)                           // ???????
    {
        return Equals(items[item.HeapIndex], item);

    }
    
    public int Count
    {
        get
        {
            return currentItemCount;
        }
    }

    bool ICollection.IsSynchronized => throw new NotImplementedException();

    public object SyncRoot => throw new NotImplementedException();

    public void Swap (T item1, T item2)
    {
        items[item1.HeapIndex] = item2;
        items[item2.HeapIndex] = item1;
        int tempIndex = item1.HeapIndex;
        item1.HeapIndex = item2.HeapIndex;
        item2.HeapIndex = tempIndex;

    }

    public void CopyTo(Array a, int n)
    {
        throw new NotImplementedException();
    }

    public bool IsSynchronized()
    {
        return false;
    }

    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

public interface IHeapItem<T> : IComparable<T>  
{
    int HeapIndex
    {
        get;
        set;
    }

}
