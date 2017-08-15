// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SuspensionContextExtensions.cs" company="Catel development team">
//   Copyright (c) 2008 - 2017 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Catel.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// The suspension context extensions.
    /// </summary>
    internal static class SuspensionContextExtensions
    {
        #region Methods
        /// <summary>
        /// The create event args list.
        /// </summary>
        /// <param name="suspensionContext">The suspension context.</param>
        /// <typeparam name="T">The type of collection item.</typeparam>
        /// <returns>The <see cref="ICollection{NotifyRangedCollectionChangedEventArgs}"/>.</returns>
        public static ICollection<NotifyRangedCollectionChangedEventArgs> CreateEventArgsList<T>(this SuspensionContext<T> suspensionContext)
        {
            // No suspension context is the same as None mode
            var mode = suspensionContext?.Mode ?? SuspensionMode.None;

            // Fast return for no items in not None modes
            // ReSharper disable once PossibleNullReferenceException
            if (mode != SuspensionMode.None && suspensionContext.ChangedItems.Count == 0)
            {
                return new NotifyRangedCollectionChangedEventArgs[] { };
            }

            // Determine creation depending on mode
            ICollection<NotifyRangedCollectionChangedEventArgs> eventArgsList = new List<NotifyRangedCollectionChangedEventArgs>();
            switch (mode)
            {
                case SuspensionMode.None:
                    {
                        eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs());
                        break;
                    }

                case SuspensionMode.Adding:
                case SuspensionMode.Removing:
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(suspensionContext.ChangedItems, suspensionContext.ChangedItemIndices, mode));
                        break;
                    }

                case SuspensionMode.Mixed:
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        eventArgsList.Add(new NotifyRangedCollectionChangedEventArgs(suspensionContext.ChangedItems, suspensionContext.ChangedItemIndices, suspensionContext.MixedActions));
                        break;
                    }

                case SuspensionMode.MixedBash:
                    {
                        eventArgsList = suspensionContext.CreateMixedBashEventArgsList();
                        break;
                    }

                case SuspensionMode.MixedConsolidate:
                    {
                        eventArgsList = suspensionContext.CreateMixedConsolidateEventArgsList();
                        break;
                    }

                default:
                    {
                        // ReSharper disable once LocalizableElement
                        throw new ArgumentOutOfRangeException(nameof(mode), $"The suspension mode '{mode}' is unhandled.");
                    }
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

        /// <summary>
        /// The create mixed bash event args list.
        /// </summary>
        /// <param name="suspensionContext">The suspension context.</param>
        /// <typeparam name="T">The type of collection item.</typeparam>
        /// <returns>The <see cref="ICollection{NotifyRangedCollectionChangedEventArgs}"/>.</returns>
        private static ICollection<NotifyRangedCollectionChangedEventArgs> CreateMixedBashEventArgsList<T>(this SuspensionContext<T> suspensionContext)
        {
            Argument.IsNotNull(nameof(suspensionContext), suspensionContext);
            Argument.IsValid(nameof(suspensionContext.Mode), suspensionContext.Mode, mode => mode == SuspensionMode.MixedBash);

            var i = 0;
            var changedItems = new List<T>();
            var changedItemIndices = new List<int>();
            var previousAction = (NotifyCollectionChangedAction?)null;
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
        private static ICollection<NotifyRangedCollectionChangedEventArgs> CreateMixedConsolidateEventArgsList<T>(this SuspensionContext<T> suspensionContext)
        {
            Argument.IsNotNull(nameof(suspensionContext), suspensionContext);
            Argument.IsValid(nameof(suspensionContext.Mode), suspensionContext.Mode, mode => mode == SuspensionMode.MixedConsolidate);

            var i = 0;
            var newItemIndices = new List<int>();
            var newItems = new List<T>();
            var oldItemIndices = new List<int>();
            var oldItems = new List<T>();
            foreach (var action in suspensionContext.MixedActions)
            {
                // Get item and index
                var item = suspensionContext.ChangedItems[i];
                var index = suspensionContext.ChangedItemIndices[i];

                // Try to consolidate
                if (action == NotifyCollectionChangedAction.Add)
                {
                    if (TryRemoveItem(item, index, oldItems, oldItemIndices) == false)
                    {
                        newItems.Add(item);
                        newItemIndices.Add(index);
                    }
                }
                else if (action == NotifyCollectionChangedAction.Remove)
                {
                    if (TryRemoveItem(item, index, newItems, newItemIndices) == false)
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
        #region Helper functions for MixedConsolidate

        /// <summary>
        /// Tries to remove the item from the given <paramref name="items"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The index</param>
        /// <param name="items">The items</param>
        /// <param name="itemIndices">The item Indices.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns><c>true</c> if removed, otherwise <c>false</c>.</returns>
        private static bool TryRemoveItem<T>(T item, int index, List<T> items, List<int> itemIndices)
        {
            // Cannot remove if there are no items
            if (items.Count != 0)
            {
                return false;
            }

            // TODO: Improve the performance of these operations
            if (index != -1)
            {
                var itemIndex = itemIndices.LastIndexOf(index);
                if (itemIndex == -1 || !Equals(item, items[itemIndex]))
                {
                    return false;
                }

                items.RemoveAt(itemIndex);
                itemIndices.RemoveAt(itemIndex);

                return true;
            }
            else
            {
                var newIndex = items.LastIndexOf(item);
                if (newIndex > -1)
                {
                    items.RemoveAt(newIndex);
                    itemIndices.RemoveAt(newIndex);

                    return true;
                }

                return false;
            }
        }
        #endregion Helper functions for MixedConsolidate
        #endregion Methods
    }
}