﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;

namespace ExRam.ReactiveCollections
{
    internal abstract class TransformationListReactiveCollection<TSource, TResult, TCollection, TNotification> : IReactiveCollection<TNotification>
        where TCollection : IReactiveCollectionSource<TNotification>
        where TNotification : ICollectionChangedNotification
    {
        protected TransformationListReactiveCollection(IReactiveCollection<ICollectionChangedNotification<TSource>> source, Predicate<TSource> filter, Func<TSource, TResult> selector)
        {
            Contract.Requires(source != null);

            this.Source = source;
            this.Filter = filter;
            this.Selector = selector;

            this.Changes = Observable
                .Defer(() =>
                {
                    var syncRoot = new object();
                    var resultList = this.CreateCollection();

                    return Observable
                        .Using(
                            () => source
                                .Changes
                                .Subscribe(
                                    notification =>
                                    {
                                        var localSelector = selector ?? (x => (TResult)(object)x);
                                        var listNotification = notification as ListChangedNotification<TSource>;

                                        lock (syncRoot)
                                        {
                                            switch (notification.Action)
                                            {
                                                #region Add
                                                case NotifyCollectionChangedAction.Add:
                                                {
                                                    var filteredItems = filter != null
                                                        ? notification.NewItems.Where(x => filter(x))
                                                        : notification.NewItems;
                                                
                                                    var selectedItems = filteredItems.Select(localSelector);

                                                    if ((filter == null) && listNotification?.Index != null && this.CanHandleIndexes)
                                                        this.InsertRange(resultList, listNotification.Index.Value, selectedItems);
                                                    else
                                                        this.AddRange(resultList, selectedItems);

                                                    break;
                                                }
                                                #endregion

                                                #region Remove
                                                case NotifyCollectionChangedAction.Remove:
                                                {
                                                    if ((filter == null) && (listNotification?.Index != null) && this.CanHandleIndexes)
                                                        this.RemoveRange(resultList, listNotification.Index.Value, notification.OldItems.Count);
                                                    else
                                                    {
                                                        var filtered = filter != null
                                                            ? notification.OldItems.Where(x => filter(x))
                                                            : notification.OldItems;

                                                        this.RemoveRange(resultList, filtered.Select(localSelector));
                                                    }

                                                    break;
                                                }
                                                #endregion

                                                #region Replace
                                                case NotifyCollectionChangedAction.Replace:
                                                {
                                                    if ((notification.OldItems.Count == 1) && (notification.NewItems.Count == 1))
                                                    {
                                                        var wasIn = filter?.Invoke(notification.OldItems[0]) ?? true;
                                                        var getsIn = filter?.Invoke(notification.NewItems[0]) ?? true;

                                                        if (wasIn && getsIn)
                                                        {
                                                            var newItem = localSelector(notification.NewItems[0]);
                                                        
                                                            if ((filter == null) && (listNotification?.Index != null) && this.CanHandleIndexes)
                                                                this.SetItem(resultList, listNotification.Index.Value, newItem);
                                                            else
                                                            {
                                                                var oldItem = localSelector(notification.OldItems[0]);

                                                                this.Replace(resultList, oldItem, newItem);
                                                            }
                                                        }
                                                        else if (wasIn)
                                                            this.RemoveRange(resultList, notification.OldItems.Select(localSelector));
                                                        else if (getsIn)
                                                            this.AddRange(resultList, notification.NewItems.Select(localSelector));
                                                    }
                                                    else
                                                    {
                                                        if ((filter == null) && (listNotification?.Index != null) && this.CanHandleIndexes)
                                                        {
                                                            this.RemoveRange(resultList, listNotification.Index.Value, notification.OldItems.Count);
                                                            this.InsertRange(resultList, listNotification.Index.Value, notification.NewItems.Select(localSelector));
                                                        }
                                                        else
                                                        {
                                                            var removedItems = filter != null
                                                                ? notification.OldItems.Where(x => filter(x))
                                                                : notification.OldItems;

                                                            var addedItems = filter != null
                                                                ? notification.NewItems.Where(x => filter(x))
                                                                : notification.NewItems;

                                                            this.RemoveRange(resultList, removedItems.Select(localSelector));
                                                            this.AddRange(resultList, addedItems.Select(localSelector));
                                                        }
                                                    }

                                                    break;
                                                }
                                                #endregion

                                                #region default
                                                default:
                                                {
                                                    this.Clear(resultList);

                                                    var addedItems = filter != null
                                                        ? notification.Current.Where(x => filter(x))
                                                        : notification.Current;

                                                    this.AddRange(resultList, addedItems.Select(localSelector));

                                                    break;
                                                }
                                                #endregion
                                            }
                                        }
                                    }),

                            _ => resultList.ReactiveCollection.Changes);
                })
                .ReplayFresh(1)
                .RefCount()
                .Normalize();
        }

        public bool CanAddWhere()
        {
            return this.Selector == null;   //TODO: Add Comparer == null later.
        }

        public bool CanAddSelect()
        {
            return true;  //TODO: Add Comparer == null later.
        }

        public Predicate<TSource> Filter { get; }
        public Func<TSource, TResult> Selector { get; }
        public IObservable<TNotification> Changes { get; }
        public IReactiveCollection<ICollectionChangedNotification<TSource>> Source { get; }

        protected abstract bool CanHandleIndexes { get; }
        protected abstract void SetItem(TCollection collection, int index, TResult item);
        protected abstract void AddRange(TCollection collection, IEnumerable<TResult> items);
        protected abstract void InsertRange(TCollection collection, int index, IEnumerable<TResult> items);
        protected abstract void RemoveRange(TCollection collection, int index, int count);
        protected abstract void RemoveRange(TCollection collection, IEnumerable<TResult> items);
        protected abstract void Replace(TCollection collection, TResult oldItem, TResult newItem);
        protected abstract void Clear(TCollection collection);

        protected abstract TCollection CreateCollection();
    }
}
