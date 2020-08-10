using System.Collections.Generic;
using Common.Caches.Hashes;

namespace Common.Caches
{
  public class LRU<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
  {
    private readonly Dictionary<TKey, int> myStore;
    private readonly Node[] myNodes;
    private int myNodeIndex;

    private int myHead;
    private int myTail;

    public int Capacity { get; }
    public bool IsFixedSize => true;

    public LRU(int capacity, IEqualityComparer<TKey> comparer)
    {
      Capacity = HashUtil.GetPrime(capacity);
      myStore = new Dictionary<TKey, int>(Capacity, comparer);
      myNodes = new Node[Capacity + 1];
      myNodeIndex = 0;
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
      if (myNodeIndex != 0 && myStore.TryGetValue(key, out var node))
      {
        MoveToTail(node);
        SetValue(node, value);
        return;
      }

      if (myNodeIndex == Capacity)
        RemoveHead(); // todo insert instead head

      var oldTail = myTail;
      var newNode = ++myNodeIndex;
      ref var newNodeRef = ref GetNode(newNode);
      newNodeRef.Prev = oldTail;
      newNodeRef.Value = value;
      newNodeRef.Key = key;
      if (oldTail != 0)
        SetNext(oldTail, newNode);

      if (myHead == 0)
        myHead = newNode;
      myTail = newNode;

      myStore.Add(key, newNode);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      if (myStore.TryGetValue(key, out var node))
      {
        MoveToTail(node);
        value = GetValue(node);
        return true;
      }

      value = default!; // todo
      return false;
    }

    public bool Remove(TKey key)
    {
      if (!myStore.Remove(key, out var node))
        return false;

      var lastNodeIndex = myNodeIndex--; 
      if (lastNodeIndex == 1)
      {
        Clear(node);
        return true;
      }

      ref var lastNodeRef = ref GetNode(lastNodeIndex);
      ref var nodeRef = ref GetNode(node);

      var nodePrev = nodeRef.Prev;
      var nodeNext = nodeRef.Next;
      
      SetNext(nodePrev, nodeNext);
      SetPrev(nodeNext, nodePrev);

      myNodes[node] = lastNodeRef;
      SetNext(lastNodeRef.Prev, node);
      SetPrev(lastNodeRef.Next, node);
      
      Clear(lastNodeIndex);
      return true;
    }

    private void MoveToTail(int node)
    {
      ref var nodeRef = ref GetNode(node);
      
      var next = nodeRef.Next;
      if (next == 0)
      {
        return;
      }

      var prev = nodeRef.Prev;
      if (prev == 0)
      {
        myHead = next;
      }
      else
      {
        SetNext(prev, next);
        SetPrev(next, prev);
      }

      var tail = myTail;
      SetNext(tail, node);
      nodeRef.Prev = tail;
      nodeRef.Next = 0;
      myTail = node;
    }

    private void RemoveHead()
    {
      var oldHead = myHead;
      ref var head = ref GetNode(oldHead);
      
      myStore.Remove(head.Key);
      SetPrev(myHead = head.Next, 0);
      
      Clear(oldHead);
      myNodeIndex--;
    }

    private ref Node GetNode(int index) => ref myNodes[index];
    private void Clear(int index) => myNodes[index] = new Node();
    
    private void SetValue(int index, TValue value) => GetNode(index).Value = value;
    private TValue GetValue(int index) => GetNode(index).Value;

    private void SetNext(int index, int nextIndex) => GetNode(index).Next = nextIndex;
    private void SetPrev(int index, int prevIndex) => GetNode(index).Prev = prevIndex;

    private struct Node
    {
      public int Prev;
      public int Next;

      public TValue Value;
      public TKey Key;
    }
  }
}