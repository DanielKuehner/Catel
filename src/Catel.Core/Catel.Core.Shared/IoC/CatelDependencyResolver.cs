﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CatelDependencyResolver.cs" company="Catel development team">
//   Copyright (c) 2008 - 2015 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Catel.IoC
{
    using System;
    using Collections;
    using Logging;
    using Reflection;

    /// <summary>
    /// Implementation of the <see cref="IDependencyResolver"/> interface for Catel by wrapping the
    /// <see cref="ServiceLocator"/>.
    /// </summary>
    public class CatelDependencyResolver : IDependencyResolver
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IServiceLocator _serviceLocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CatelDependencyResolver" /> class.
        /// </summary>
        /// <param name="serviceLocator">The service locator.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="serviceLocator"/> is <c>null</c>.</exception>
        public CatelDependencyResolver(IServiceLocator serviceLocator)
        {
            Argument.IsNotNull("serviceLocator", serviceLocator);

            _serviceLocator = serviceLocator;
        }

        /// <summary>
        /// Determines whether the specified type with the specified tag can be resolved.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tag">The tag.</param>
        /// <returns><c>true</c> if the specified type with the specified tag can be resolved; otherwise, <c>false</c>.</returns>
        public bool CanResolve(Type type, object tag = null)
        {
            Argument.IsNotNull("type", type);

            return _serviceLocator.IsTypeRegistered(type, tag);
        }

        /// <summary>
        /// Determines whether all types specified can be resolved. Though <see cref="ResolveAll"/> will return <c>null</c>
        /// at the array index when a type cannot be resolved, this method will actually check whether all the specified types
        /// are registered.
        /// <para />
        /// It is still possible to call <see cref="ResolveAll"/>, even when this method returns <c>false</c>.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <returns><c>true</c> if all types specified can be resolved; otherwise, <c>false</c>.</returns>
        [ObsoleteEx(ReplacementTypeOrMember = "CanResolveMultiple", TreatAsErrorFromVersion = "5.2", RemoveInVersion = "6.0")]
        public bool CanResolveAll(Type[] types)
        {
            return CanResolveMultiple(types);
        }

        /// <summary>
        /// Determines whether all types specified can be resolved. Though <see cref="ResolveMultiple"/> will return <c>null</c>
        /// at the array index when a type cannot be resolved, this method will actually check whether all the specified types
        /// are registered.
        /// <para />
        /// It is still possible to call <see cref="ResolveMultiple"/>, even when this method returns <c>false</c>.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <returns><c>true</c> if all types specified can be resolved; otherwise, <c>false</c>.</returns>
        public bool CanResolveMultiple(Type[] types)
        {
            Argument.IsNotNull("types", types);

            if (types.Length == 0)
            {
                return true;
            }

            return _serviceLocator.AreAllTypesRegistered(types);
        }

        /// <summary>
        /// Resolves the specified type with the specified tag.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tag">The tag.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="type" /> is <c>null</c>.</exception>
        /// <exception cref="TypeNotRegisteredException">The type is not found in any container.</exception>
        /// <returns>The resolved object.</returns>
        public object Resolve(Type type, object tag = null)
        {
            Argument.IsNotNull("type", type);

            return _serviceLocator.ResolveType(type, tag);
        }

        /// <summary>
        /// Resolves the specified types with the specified tag.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>A list of resolved types. If one of the types cannot be resolved, that location in the array will be <c>null</c>.</returns>
        [ObsoleteEx(ReplacementTypeOrMember = "ResolveMultiple", TreatAsErrorFromVersion = "5.2", RemoveInVersion = "6.0")]
        public object[] ResolveAll(Type[] types, object tag = null)
        {
            return ResolveMultiple(types, tag);
        }

        /// <summary>
        /// Resolves the specified types with the specified tag.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>A list of resolved types. If one of the types cannot be resolved, that location in the array will be <c>null</c>.</returns>
        public object[] ResolveMultiple(Type[] types, object tag = null)
        {
            Argument.IsNotNull("types", types);

            if (types.Length == 0)
            {
                return ArrayShim.Empty<object>();
            }

            int typeCount = types.Length;
            var resolvedTypes = new object[typeCount];

            lock (_serviceLocator)
            {
                for (int i = 0; i < typeCount; i++)
                {
                    try
                    {
                        resolvedTypes[i] = Resolve(types[i], tag);
                    }
                    catch (TypeNotRegisteredException ex)
                    {
                        Log.Debug(ex, "Failed to resolve type '{0}', returning null", ex.RequestedType.GetSafeFullName(false));
                    }
                }
            }

            return resolvedTypes;
        }
    }
}