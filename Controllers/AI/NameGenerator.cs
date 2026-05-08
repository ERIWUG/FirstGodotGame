using Godot;
using System.Collections.Generic;

public static class NameGenerator
{
    private static List<string> _firstNames = new List<string>
    {
        "Арик", "Векс", "Ирис", "Грон", "Мира", "Тарк", "Лин", "Орфей", "Стич", "Бракс",
        "Финн", "Кира", "Зейн", "Руна", "Гектор", "Соль", "Люций", "Астра", "Рейк", "Никс"
    };

    private static List<string> _titles = new List<string>
    {
        "Железный", "Хитрый", "Стремительный", "Мрачный", "Яростный", "Теневой", "Стальной",
        "Безжалостный", "Отважный", "Мудрый", "Кровавый", "Непоколебимый", "Пылкий", "Шепчущий"
    };

    public static string Generate()
    {
        // Защита: если список пуст (не должно случиться), возвращаем запасное имя
        if (_firstNames.Count == 0) return "Безымянный";
        
        string name = _firstNames[GD.RandRange(0, _firstNames.Count - 1)];
        
        // 30% шанс получить титул, если список не пуст
        if (_titles.Count > 0 && GD.Randf() < 0.3f)
        {
            string title = _titles[GD.RandRange(0, _titles.Count - 1)];
            return $"{name} {title}";
        }
        return name;
    }


    private static List<string> _squadPrefixes = new List<string>
    {
        "Кровавые", "Железные", "Теневые", "Стальные", "Бессмертные",
        "Неистовые", "Мрачные", "Благородные", "Дикие", "Хитрые"
    };

    private static List<string> _squadSuffixes = new List<string>
    {
        "Ястребы", "Воины", "Когти", "Клыки", "Стражи",
        "Мечники", "Вороны", "Змеи", "Волки", "Призраки"
    };

    public static string GenerateSquadName()
    {
        string prefix = _squadPrefixes[GD.RandRange(0, _squadPrefixes.Count - 1)];
        string suffix = _squadSuffixes[GD.RandRange(0, _squadSuffixes.Count - 1)];
        return $"{prefix} {suffix}";
    }
}