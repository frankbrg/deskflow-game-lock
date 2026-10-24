using System;

internal static class ConfinementTests
{
    private static Native.RECT Rect(int left, int top, int right, int bottom)
    {
        return new Native.RECT { Left = left, Top = top, Right = right, Bottom = bottom };
    }
    private static int Main()
    {
        int failures = 0;
        Native.RECT desktop = Rect(-1920, -400, 2560, 1440);
        Native.RECT game = Rect(0, 0, 2560, 1440);
        Check("A fullscreen video with an unrestricted pointer stays unlocked", false, Native.IsConfinement(desktop, desktop, game), ref failures);
        Check("A game confined to one monitor is detected", true, Native.IsConfinement(game, desktop, game), ref failures);
        Check("A windowed game is detected", true, Native.IsConfinement(Rect(108, 132, 892, 692), desktop, Rect(100, 100, 900, 700)), ref failures);
        Check("Negative monitor coordinates are supported", true, Native.IsConfinement(Rect(-1920, -400, 0, 1040), desktop, Rect(-1920, -400, 0, 1040)), ref failures);
        Check("A clip belonging to a different window is ignored", false, Native.IsConfinement(Rect(-1900, 0, -300, 900), desktop, game), ref failures);
        Check("Single-monitor fullscreen is conservatively treated as unrestricted", false, Native.IsConfinement(game, game, game), ref failures);
        Check("A one-pixel FPS confinement is detected", true, Native.IsConfinement(Rect(1280, 720, 1281, 721), desktop, game), ref failures);
        Check("An empty clip cannot trigger a lock", false, Native.IsConfinement(Rect(0, 0, 0, 0), desktop, game), ref failures);
        Check("A reversed clip cannot trigger a lock", false, Native.IsConfinement(Rect(100, 100, 0, 0), desktop, game), ref failures);
        Check("Unavailable desktop dimensions cannot trigger a lock", false, Native.IsConfinement(game, Rect(0, 0, 0, 0), game), ref failures);
        Console.WriteLine(failures == 0 ? "All 10 confinement regression checks passed." : failures + " checks failed.");
        return failures == 0 ? 0 : 1;
    }
    private static void Check(string description, bool expected, bool actual, ref int failures)
    {
        if (expected != actual) failures++;
        Console.WriteLine((expected == actual ? "PASS " : "FAIL ") + description);
    }
}
