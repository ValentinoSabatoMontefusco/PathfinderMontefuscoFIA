using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Heap<T> : ICollection where T : IHeapItem<T>
{
    //List<T> items;
    private class Entry
    {
        public T item;
        public int value;

        public Entry(T item, int value)
        {
            this.item = item;
            this.value = value;
        }
    }

    Entry[] entries;
    private int currentItemCount = 0;
    static int maxSize= 150*150;

    public Heap() {

        entries = new Entry[maxSize];
    }

    public void Add (T item, int value)
    {
        item.HeapIndex = currentItemCount;
        
        //items.Add(item);
        entries[currentItemCount] = new Entry(item, value); 
        Sortup(entries[item.HeapIndex]);
        currentItemCount++;

    }

    private void Sortup(Entry entry)
    {

        int index = entry.item.HeapIndex;

        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (entry.value >= entries[parentIndex].value)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }


        //int parentIndex = (entry.item.HeapIndex - 1) / 2;

        //while (entry.value < entries[parentIndex].value)
        //{
        //    Swap(entry, entries[parentIndex]);
        //    parentIndex = (entry.item.HeapIndex - 1) / 2;
        //}

        
    }

    public void UpdateItem(T item, int value)
    {
        entries[item.HeapIndex].value = value;
        Sortup(entries[item.HeapIndex]);

    }

    public T Extract()
    {
        if (currentItemCount == 0)
            throw new InvalidOperationException();

        T firstItem = entries[0].item;
        currentItemCount--;
        entries[0] = entries[currentItemCount];
        entries[0].item.HeapIndex = 0;
        Sortdown(entries[0]);


        return firstItem;
    }

        private void Sortdown(Entry entry)
        {
        int index = entry.item.HeapIndex;
        int leftChildIndex;
        int rightChildIndex;

        int swapIndex;

        while (true)
        {
            leftChildIndex = index * 2 + 1;
            rightChildIndex = index * 2 + 2;
            swapIndex = index;

            if (leftChildIndex < currentItemCount && entries[leftChildIndex].value < entries[swapIndex].value)
                swapIndex = leftChildIndex;

            if (rightChildIndex < currentItemCount && entries[leftChildIndex].value < entries[swapIndex].value)
                swapIndex = rightChildIndex;

            if (swapIndex == index)
                break;

            Swap(index, swapIndex);
            index = swapIndex;

            //if (leftChildIndex < currentItemCount)
            //{
            //    swapIndex = leftChildIndex;
            //    if (rightChildIndex < currentItemCount)
            //        if (entries[leftChildIndex].value > entries[rightChildIndex].value)
            //            swapIndex = rightChildIndex;

            //    if (entry.value > entries[swapIndex].value)
            //        Swap(entry, entries[swapIndex]);
            //    else
            //        return;
            //}
            //else
            //    return;

            
            
        }

    }
    private void Swap(int index1, int index2)
    {
        Entry temp = entries[index1];
        entries[index1] = entries[index2];
        entries[index2] = temp;
        entries[index1].item.HeapIndex = index1;
        entries[index2].item.HeapIndex = index2;        
        
        //entries[pair1.item.HeapIndex] = pair2;
        //entries[pair2.item.HeapIndex] = pair1;
        //int tempIndex = pair1.item.HeapIndex;
        //pair1.item.HeapIndex = pair2.item.HeapIndex;
        //pair2.item.HeapIndex = tempIndex;

    }

    public bool Contains (T item)                           // ???????
    {
        return Equals(entries[item.HeapIndex], item);

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
