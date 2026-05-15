using Godot;
public static class FactionManager
{
    public static bool AreHostile(string factionA, string factionB)
    {
      if (factionA == factionB) return false;
        return true;
    }

    public static Color GetColor(string factionId)
    {
        switch (factionId)
        {
            case "shadow_cult": return Colors.Red;
            case "mercenary":   return Colors.CornflowerBlue;
            case "neutral":     return Colors.Gray;
            default:            return Colors.White;
        }
    }
}