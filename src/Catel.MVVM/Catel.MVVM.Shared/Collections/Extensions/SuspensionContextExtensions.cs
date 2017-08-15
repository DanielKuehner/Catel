// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SuspensionContextExtensions.cs" company="Catel development team">
//   Copyright (c) 2008 - 2017 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Collections.Extensions
{
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// The suspension context extensions.
    /// </summary>
    internal static class SuspensionContextExtensions
    {
        #region Methods
        /// <summary>
        /// The create mixed bash event args list.
        /// </summary>
        /// <param name="suspensionContext">The suspension context.</param>
        /// <typeparam name="T">The type of collection item.</typeparam>
        /// <returns>The <see cref="ICollection{NotifyRangedCollectionChangedEventArgs}"/>.</returns>
        public static ICollection<NotifyRangedCollectionChangedEventArgs> CreateMixedBashEventArgsList<T>(this SuspensionContext<T> suspensionContext)
        {
            Argument.IsNotNull(nameof(suspensionContext), suspensionContext);
            Argument.IsValid(nameof(suspensionContext.Mode), suspensionContext.Mode, mode => mode == SuspensionMode.MixedBash);

            var i = 0;
            NotifyCollectionChangedAction? previousAction = null;
            var changedItems = new List<T>();
            var changedItemIndices = new List<int>();
            var eventArgsList = new List<NotifyRangedCollectionChangedEventArgs>();
            foreach (var action in suspensionContext.MixedActions)
            {
                // If action changed, create event args for remembered items
                if (previousAction.HasValue && action != previousAction.Value)
                {
                    // Create and add event args
                    eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(changedItems, changedItemIndices, SuspensionMode.MixedBash, previousAction.Value));

                    // Reset lists
                    changedItems = new List<T>();
                    changedItemIndices = new List<int>();
                }

                // Remember item and index
                changedItems.Add(suspensionContext.ChangedItems[i]);
                changedItemIndices.Add(suspensionContext.ChangedItemIndices[i]);

                // Update to current action
                previousAction = action;

                i++;
            }

            // Create event args for last item(s)
            if (changedItems.Count != 0)
            {
                // ReSharper disable once PossibleInvalidOperationException
                eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(changedItems, changedItemIndices, SuspensionMode.MixedBash, previousAction.Value));
            }

            return eventArgsList;
        }

        /// <summary>
        /// The create mixed consolidate event args list.
        /// </summary>
        /// <param name="suspensionContext">The suspension context.</param>
        /// <typeparam name="T">The type of collection item.</typeparam>
        /// <returns>The <see cref="ICollection{NotifyRangedCollectionChangedEventArgs}"/>.</returns>
        public static ICollection<NotifyRangedCollectionChangedEventArgs> CreateMixedConsolidateEventArgsList<T>(this SuspensionContext<T> suspensionContext)
        {
            Argument.IsNotNull(nameof(suspensionContext), suspensionContext);
            Argument.IsValid(nameof(suspensionContext.Mode), suspensionContext.Mode, mode => mode == SuspensionMode.MixedConsolidate);

            var newItemIndices = new List<int>();
            var newItems = new List<T>();
            var oldItemIndices = new List<int>();
            var oldItems = new List<T>();

            // Consolidate events
            var i = 0;
            foreach (var action in suspensionContext.MixedActions)
            {
                var item = suspensionContext.ChangedItems[i];
                var index = suspensionContext.ChangedItemIndices[i];

                if (action == NotifyCollectionChangedAction.Add)
                {
                    var insertRequired = IsInsertRequired(item, index, oldItems, oldItemIndices);
                    if (insertRequired)
                    {
                        newItems.Add(item);
                        newItemIndices.Add(index);
                    }
                }
                else if (action == NotifyCollectionChangedAction.Remove)
                {
                    var removeRequired = IsRemoveRequired(item, index, newItems, newItemIndices);
                    if (removeRequired)
                    {
                        oldItems.Add(item);
                        oldItemIndices.Add(index);
                    }
                }

                i++;
            }

            // Create required event args
            var eventArgsList = new List<NotifyRangedCollectionChangedEventArgs>();
            if (newItems.Count != 0)
            {
                eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(newItems, newItemIndices, SuspensionMode.MixedConsolidate, NotifyCollectionChangedAction.Add));
            }
            if (oldItems.Count != 0)
            {
                eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(oldItems, oldItemIndices, SuspensionMode.MixedConsolidate, NotifyCollectionChangedAction.Remove));
            }

            return eventArgsList;
        }

        /// <summary>
        /// The is mixed mode.
        /// </summary>
        /// <param name="suspensionContext">The suspension context.</param>
        /// <typeparam name="T">The type of collection item.</typeparam>
        /// <returns><c>True</c> if <see cref="SuspensionMode"/> is one of the mixed modes; otherwise, <c>false</c>.</returns>
        public static bool IsMixedMode<T>(this SuspensionContext<T> suspensionContext)
        {
            Argument.IsNotNull(nameof(suspensionContext), suspensionContext);

            return suspensionContext.Mode.IsMixedMode();
        }

        #region Helper functions for MixedConsolidate
        ///// <summary>
        ///// Synchronize the list with the suspenstion context.
        ///// </summary>
        ///// <typeparam name="T">The type of item.</typeparam>
        //private static void SynchronizeFromSuspensionContext<T>()
        //{
        //    Synchronize<T>((idx, value) => base.InsertItem(idx, value), idx => base.RemoveItem(idx));
        //}

        /// <summary>
        /// Checks if insert is required.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="index">The item index</param>
        /// <param name="oldItems">The old Items.</param>
        /// <param name="oldItemIndices">The old Item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if its required; otherwise <c>false</c></returns>
        private static bool IsInsertRequired<T>(T item, int index, List<T> oldItems, List<int> oldItemIndices)
        {
            return !TryRemoveItemFromOldItems(item, index, oldItems, oldItemIndices);
        }

        /// <summary>
        /// Checks if insert is required.
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="index">The item index</param>
        /// <param name="newItems">The new Items.</param>
        /// <param name="newItemIndices">The new Item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if its required; otherwise <c>false</c></returns>
        private static bool IsRemoveRequired<T>(T item, int index, List<T> newItems, List<int> newItemIndices)
        {
            return !TryRemoveItemFromNewItems(item, index, newItems, newItemIndices);
        }

        /// <summary>
        /// Tries to remove the item from new items
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The item index</param>
        /// <param name="newItems">The new Items.</param>
        /// <param name="newItemIndices">The new Item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if removed, otherwise <c>false</c>.</returns>
        private static bool TryRemoveItemFromNewItems<T>(T item, int index, List<T> newItems, List<int> newItemIndices)
        {
            return TryRemoveItems(item, index, newItems, newItemIndices);
        }

        /// <summary>
        /// Tries to remove the item from old items
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The index.</param>
        /// <param name="oldItems">The old Items.</param>
        /// <param name="oldItemIndices">The old Item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if removed, otherwise <c>false</c>.</returns>
        private static bool TryRemoveItemFromOldItems<T>(T item, int index, List<T> oldItems, List<int> oldItemIndices)
        {
            return TryRemoveItems(item, index, oldItems, oldItemIndices);
        }

        ///// <summary>
        ///// Synchronize
        ///// </summary>
        ///// <param name="insertSyncAction">The insert synchronization action</param>
        ///// <param name="removeSyncAction">The remove synchronization action</param>
        ///// <typeparam name="T">The type of item.</typeparam>
        //private static void Synchronize<T>(Action<int, T> insertSyncAction, Action<int> removeSyncAction)
        //{
        //    SynchronizeInserts(insertSyncAction);
        //    SynchronizeRemoves<T>(removeSyncAction);
        //}

        /// <summary>
        /// Tries to remove the item from the given <paramref name="items"/>
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The index</param>
        /// <param name="items">The items</param>
        /// <param name="itemIndices">The item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if removed, otherwise <c>false</c>.</returns>
        private static bool TryRemoveItems<T>(T item, int index, List<T> items, List<int> itemIndices)
        {
            if (itemIndices.Count == 0)
            {
                return false;
            }

            if (index != -1)
            {
                // TODO: Improve the performace of this operations.

                var itemIdx = itemIndices.LastIndexOf(index);
                if (itemIdx == -1 || !Equals(item, items[itemIdx]))
                {
                    return false;
                }

                items.RemoveAt(itemIdx);
                itemIndices.RemoveAt(itemIdx);

                return true;
            }

            var newIdx = items.LastIndexOf(item);
            if (newIdx > -1)
            {
                items.RemoveAt(newIdx);
                itemIndices.RemoveAt(newIdx);

                return true;
            }

            return false;
        }

        ///// <summary>
        ///// Synchronize from old items
        ///// </summary>
        ///// <param name="insertSyncAction">The remove sync action</param>
        ///// <param name="newItems">The new Items.</param>
        ///// <param name="newItemIndices">The new Item Indices.</param>
        ///// <typeparam name="T">The type of item.</typeparam>
        //private static void SynchronizeInserts<T>(Action<int, T> insertSyncAction, List<T> newItems, List<int> newItemIndices)
        //{
        //    Argument.IsNotNull(nameof(insertSyncAction), insertSyncAction);

        //    if (newItems.Count <= 0)
        //    {
        //        return;
        //    }

        //    for (var i = 0; i < newItems.Count; i++)
        //    {
        //        insertSyncAction(newItemIndices[i], newItems[i]);
        //    }
        //}

        ///// <summary>
        ///// Synchronize from old items
        ///// </summary>
        ///// <param name="removeSyncAction">The remove sync action</param>
        ///// <param name="oldItems">The old Items.</param>
        ///// <param name="oldItemIndices">The old Item Indices.</param>
        ///// <typeparam name="T">The type of item.</typeparam>
        //private static void SynchronizeRemoves<T>(Action<int> removeSyncAction, List<T> oldItems, List<int> oldItemIndices)
        //{
        //    Argument.IsNotNull(nameof(removeSyncAction), removeSyncAction);

        //    if (oldItems.Count <= 0)
        //    {
        //        return;
        //    }

        //    var sortedDictionary = new SortedDictionary<int, T>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        //    for (var i = 0; i < oldItems.Count; i++)
        //    {
        //        sortedDictionary.Add(oldItemIndices[i], oldItems[i]);
        //    }

        //    var idx = 0;
        //    foreach (var pair in sortedDictionary)
        //    {
        //        oldItemIndices[idx] = pair.Key;
        //        oldItems[idx++] = pair.Value;

        //        removeSyncAction(pair.Key);
        //    }
        //}
        #endregion Helper functions for MixedConsolidate
        #endregion Methods
    }
}