using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum searchAlgorithm
{
    BFGreedy,
    BFS,
    Astar,
    UniformCost,
    DFS,
    IDDFS,
    RecursiveDFS,
    BeamSearch,
    IDAstar,
    RBFS
};

public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey :  System.IComparable<TKey>
{
    public int Compare(TKey x, TKey y)
    {
        int result = x.CompareTo(y);

        // Handle equality as being greater to allow duplicate keys
        return result == 0 ? 1 : result;
    }
}



public static class Shuffler
{
    public static ICollection<T> ShuffleCollection<T>(ICollection<T> coll, System.Random rng)
    {
        
        
        // Convert the collection to a list to access elements by index
        List<T> list = coll.ToList();

       

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            
            // Swap elements in the list
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        // Copy the shuffled list back into the collection if needed
        

        return list;
        
    }
}