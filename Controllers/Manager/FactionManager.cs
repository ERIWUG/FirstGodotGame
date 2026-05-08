public static class FactionManager
{
    public static bool AreHostile(string factionA, string factionB)
    {
      if (factionA == factionB) return false;
        return true;
    }
}