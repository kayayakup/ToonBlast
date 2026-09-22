public static class SpecialBlockManager
{
    private static int activeCount = 0;

    public static void StartSpecial()
    {
        activeCount++;
    }

    public static void EndSpecial()
    {
        activeCount--;
        if (activeCount <= 0)
        {
            activeCount = 0;
            if (FillManager.Instance != null)
            {
                FillManager.Instance.Fill();
            }
            RocketBlock.EndAllRocketEvents();
            BombBlock.EndAllBombEvents();
            ColorBombBlock.EndAllColorBombEvents();
        }
    }

    public static void Reset()
    {
        activeCount = 0;
    }
}
