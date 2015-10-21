﻿// (c) Copyright 2014 ExRam GmbH & Co. KG http://www.exram.de
//
// Licensed using Microsoft Public License (Ms-PL)
// Full License description can be found in the LICENSE
// file.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;

namespace ExRam.ReactiveCollections
{
    public class SortedSetReactiveCollectionSource<T> :
        ReactiveCollectionSource<SortedSetChangedNotification<T>>,
        IList<T>,
        IList,
        ISet<T>
    {
        public SortedSetReactiveCollectionSource() : this(Comparer<T>.Default)
        {
        }

        public SortedSetReactiveCollectionSource(IComparer<T> comparer) : base(new SortedSetChangedNotification<T>(ImmutableSortedSet.Create(comparer), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty))
        {
            Contract.Requires(comparer != null);
        }

        public void Add(T value)
        {
            var newSet = this.Current.Add(value);
            if (newSet != this.Current)
                this.Subject.OnNext(new SortedSetChangedNotification<T>(newSet, NotifyCollectionChangedAction.Add, ImmutableList<T>.Empty, ImmutableList.Create(value)));
        }

        public void AddRange(IEnumerable<T> items)
        {
            Contract.Requires(items != null);

            var newItems = items.ToImmutableList();
            if (!newItems.IsEmpty)
            {
                var builder = this.Current.ToBuilder();

                foreach (var item in newItems)
                {
                    builder.Add(item);
                }

                var newSet = builder.ToImmutable();

                if (this.Current != newSet)
                    this.Subject.OnNext(new SortedSetChangedNotification<T>(builder.ToImmutable(), NotifyCollectionChangedAction.Add, ImmutableList<T>.Empty, newItems));
            }
        }

        public void Clear()
        {
            if (!this.Current.IsEmpty)
                this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.Clear(), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty));
        }

        public void Except(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.Except(other), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty));
        }

        public void Intersect(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.Intersect(other), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty));
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return this.Current.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return this.Current.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return this.Current.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return this.Current.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return this.Current.Overlaps(other);
        }

        public void Remove(T value)
        {
            this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.Remove(value), NotifyCollectionChangedAction.Remove, ImmutableList.Create(value), ImmutableList<T>.Empty));
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return this.Current.SetEquals(other);
        }

        public void SymmetricExcept(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.SymmetricExcept(other), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty));
        }

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            return this.Current.TryGetValue(equalValue, out actualValue);
        }

        public void Union(IEnumerable<T> other)
        {
            Contract.Requires(other != null);

            this.Subject.OnNext(new SortedSetChangedNotification<T>(this.Current.Union(other), NotifyCollectionChangedAction.Reset, ImmutableList<T>.Empty, ImmutableList<T>.Empty));
        }

        #region IList<T> implementation
        public int IndexOf(T item)
        {
            return this.Current.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get
            {
                return this.Current[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }
        #endregion

        #region ICollection<T> implementation
        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        void ICollection<T>.Clear()
        {
            this.Clear();
        }

        public bool Contains(T item)
        {
            return this.Current.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)this.Current).CopyTo(array, arrayIndex);
        }

        public int Count => this.Current.Count;

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item)
        {
            var oldList = this.Current;
            this.Remove(item);

            return (this.Current != oldList);
        }
        #endregion

        #region IEnumerable<T> implemenation
        public IEnumerator<T> GetEnumerator()
        {
            return this.Current.GetEnumerator();
        }
        #endregion

        #region IList implementation
        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            this.Remove((T)value);
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            this.CopyTo((T[])array, index);
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new NotSupportedException();
        }

        bool ISet<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        private ImmutableSortedSet<T> Current
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableSortedSet<T>>() != null);

                return this.Subject.Value.Current;
            }
        }
    }
}
