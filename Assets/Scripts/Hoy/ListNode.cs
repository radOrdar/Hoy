namespace Hoy
{
    public class ListNode<T>
    {
        public ListNode<T> Next;
        public T Value { get; }

        public ListNode(T val)
        {
            Value = val;
        }
    }
}
