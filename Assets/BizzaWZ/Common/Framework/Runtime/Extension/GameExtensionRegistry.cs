using System;
using System.Collections.Generic;
using Bizza.Loading;

public static class GameExtensionRegistry
{
    private static readonly List<Action> StartupHooks = new();
    private static readonly List<Action<GameInstance>> GameModuleHooks = new();
    private static readonly List<Action<LoadingProcedure>> LoadingProcedureHooks = new();
    private static readonly List<Action> PreloadHooks = new();
    private static readonly List<Func<bool>> TeachEnableProviders = new();
    private static readonly List<Func<bool>> UiBackBlockProviders = new();

    public static void RegisterStartupHook(Action hook)
    {
        if (hook == null || StartupHooks.Contains(hook))
        {
            return;
        }

        StartupHooks.Add(hook);
    }

    public static void RegisterGameModuleHook(Action<GameInstance> hook)
    {
        if (hook == null || GameModuleHooks.Contains(hook))
        {
            return;
        }

        GameModuleHooks.Add(hook);
    }

    public static void RegisterLoadingProcedureHook(Action<LoadingProcedure> hook)
    {
        if (hook == null || LoadingProcedureHooks.Contains(hook))
        {
            return;
        }

        LoadingProcedureHooks.Add(hook);
    }

    public static void RegisterPreloadHook(Action hook)
    {
        if (hook == null || PreloadHooks.Contains(hook))
        {
            return;
        }

        PreloadHooks.Add(hook);
    }

    public static void RegisterTeachEnableProvider(Func<bool> provider)
    {
        if (provider == null || TeachEnableProviders.Contains(provider))
        {
            return;
        }

        TeachEnableProviders.Add(provider);
    }

    public static void RegisterUiBackBlockProvider(Func<bool> provider)
    {
        if (provider == null || UiBackBlockProviders.Contains(provider))
        {
            return;
        }

        UiBackBlockProviders.Add(provider);
    }

    public static bool ShouldEnableTeach()
    {
        foreach (Func<bool> provider in TeachEnableProviders)
        {
            if (provider.Invoke())
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldBlockUiBack()
    {
        foreach (Func<bool> provider in UiBackBlockProviders)
        {
            if (provider.Invoke())
            {
                return true;
            }
        }

        return false;
    }

    public static void RunStartupHooks()
    {
        foreach (Action hook in StartupHooks)
        {
            hook.Invoke();
        }
    }

    public static void RunGameModuleHooks(GameInstance gameInstance)
    {
        if (gameInstance == null)
        {
            return;
        }

        foreach (Action<GameInstance> hook in GameModuleHooks)
        {
            hook.Invoke(gameInstance);
        }
    }

    public static void RunLoadingProcedureHooks(LoadingProcedure procedure)
    {
        if (procedure == null)
        {
            return;
        }

        foreach (Action<LoadingProcedure> hook in LoadingProcedureHooks)
        {
            hook.Invoke(procedure);
        }
    }

    public static void RunPreloadHooks()
    {
        foreach (Action hook in PreloadHooks)
        {
            hook.Invoke();
        }
    }
}
