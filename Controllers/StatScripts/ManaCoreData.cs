using Godot;
using System.Collections.Generic;

public enum CoreElement
{
    Mana,       // Нулевое
    Fire,
    Water,
    Air,
    Earth,
    Lightning,
    Nature,
    Time,
    Light,
    Darkness
}

public enum CoreRank
{
    OneStar = 1,
    TwoStar = 2,
    ThreeStar = 3,
    FourStar = 4,
    FiveStar = 5
}

[GlobalClass]
public partial class ManaCoreData : Resource
{
    [Export] public string CoreName { get; set; }
    [Export] public CoreElement Element { get; set; }
    [Export] public CoreRank Rank { get; set; } = CoreRank.OneStar;
    [Export] public bool IsShattered { get; set; } = false; // Уничтожено ли ядро
    [Export] public bool IsCursed { get; set; } = false;    // Проклято ли (для Охотника)

    // Список ID модификаторов, которые открывает это ядро
    public List<string> UnlockedModifiers { get; set; } = new();

    // Уникальное свойство для 5-звёздного ядра (только Маг-Калека)
    [Export] public string LegendaryPropertyId { get; set; }

    // Бонус к максимальной мане от этого ядра (зависит от ранга)
    public int GetManaBonus()
    {
        return Rank switch
        {
            CoreRank.OneStar => 10,
            CoreRank.TwoStar => 20,
            CoreRank.ThreeStar => 40,
            CoreRank.FourStar => 70,
            CoreRank.FiveStar => 100,
            _ => 0
        };
    }

    // Количество слотов в конструкторе, которые даёт это ядро
    public int GetSlotCount()
    {
        if (Element == CoreElement.Mana)
        {
            // Ядро Маны даёт 1 универсальный слот (для обычного мага)
            // Для Мага-Калеки будет переопределено
            return 1;
        }
        return (int)Rank; // 1 звезда = 1 слот, 5 звёзд = 5 слотов
    }
}