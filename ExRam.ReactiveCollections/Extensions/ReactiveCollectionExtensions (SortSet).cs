﻿// (c) Copyright 2014 ExRam GmbH & Co. KG http://www.exram.de
//
// Licensed using Microsoft Public License (Ms-PL)
// Full License description can be found in the LICENSE
// file.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;

namespace ExRam.ReactiveCollections
{
    public static partial class ReactiveCollectionExtensions
    {
        #region SortReactiveSortedList
        private sealed class SortedSetReactiveCollection<TSource> : IReactiveCollection<SortedSetChangedNotification<TSource>>
        {
            public SortedSetReactiveCollection(IObservable<ICollectionChangedNotification<TSource>> source, IComparer<TSource> comparer)
            {
                Contract.Requires(source != null);
                Contract.Requires(comparer != null);

                this.Changes = Observable
                    .Defer(
                        () =>
                        {
                            var syncRoot = new object();
                            var resultList = new SortedSetReactiveCollectionSource<TSource>(comparer);

                            return Observable.Using(
                                () => source.Subscribe(
                                    notification =>
                                    {
                                        lock (syncRoot)
                                        {
                                            switch (notification.Action)
                                            {
                                                case (NotifyCollectionChangedAction.Add):
                                                {
                                                    if (notification.NewItems.Count == 1)
                                                        resultList.Add(notification.NewItems[0]);
                                                    else
                                                        resultList.AddRange(notification.NewItems);

                                                    break;
                                                }

                                                case (NotifyCollectionChangedAction.Remove):
                                                {
                                                    foreach (var value in notification.OldItems)
                                                    {
                                                        resultList.Remove(value);
                                                    }

                                                    break;
                                                }

                                                case (NotifyCollectionChangedAction.Replace):
                                                {
                                                    foreach (var value in notification.OldItems)
                                                    {
                                                        resultList.Remove(value);
                                                    }

                                                    resultList.AddRange(notification.NewItems);

                                                    break;
                                                }

                                                default:
                                                {
                                                    resultList.Clear();
                                                    resultList.AddRange(notification.Current);

                                                    break;
                                                }
                                            }
                                        }
                                    }),

                                    _ => resultList.ReactiveCollection.Changes);
                        })
                    .Replay(1)
                    .RefCount()
                    .Normalize();
            }

            public IObservable<SortedSetChangedNotification<TSource>> Changes { get; }
        }
        #endregion

        public static IReactiveCollection<SortedSetChangedNotification<TSource>> SortSet<TSource>(this IReactiveCollection<ICollectionChangedNotification<TSource>> source)
        {
            Contract.Requires(source != null);
            Contract.Ensures(Contract.Result<IReactiveCollection<SortedSetChangedNotification<TSource>>>() != null);

            return source.SortSet(Comparer<TSource>.Default);
        }

        public static IReactiveCollection<SortedSetChangedNotification<TSource>> SortSet<TSource>(this IReactiveCollection<ICollectionChangedNotification<TSource>> source, IComparer<TSource> comparer)
        {
            Contract.Requires(source != null);
            Contract.Requires(comparer != null);
            Contract.Ensures(Contract.Result<IReactiveCollection<SortedSetChangedNotification<TSource>>>() != null);

            return new SortedSetReactiveCollection<TSource>(source.Changes, comparer);
        }
    }
}
