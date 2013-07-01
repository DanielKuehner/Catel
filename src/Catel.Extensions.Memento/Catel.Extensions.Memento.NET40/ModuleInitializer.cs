﻿using Catel.IoC;
using Catel.Memento;

/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        var serviceLocator = ServiceLocator.Default;

        if (!serviceLocator.IsTypeRegistered<IMementoService>())
        {
            serviceLocator.RegisterInstance(MementoService.Default);
        }
    }
}