using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class ResourceComponent : Node
{
    // --- Сигналы ---
    [Signal] public delegate void ResourceChangedEventHandler(string resourceId, float current, float max);
    [Signal] public delegate void ResourceDepletedEventHandler(string resourceId);

    // --- Внутренний класс для данных ресурса ---
    private class ResourceData
    {
        public float Current;
        public float Max;
        public float RegenPerSecond;
        public float RegenDelay;
        public float CooldownRemaining;

        public bool SmoothRegen;
    }

    private Dictionary<string, ResourceData> _resources = new();
    private Timer _regenTimer;

    // --- Настройки по умолчанию ---
    [Export] public float DefaultRegenPerSecond { get; set; } = 0f;
    [Export] public float DefaultRegenDelay { get; set; } = 3.0f;

    // --- Базовые максимумы (редактор) ---
    [Export] public float BaseMP { get; set; } = 50f;
    [Export] public float BaseStamina { get; set; } = 20f;

    // --- Связь со статами ---
    [Export] public bool UseStatsForMax { get; set; } = false;
    [Export] public float MPPerIntelligence { get; set; } = 2f;
    [Export] public float StaminaPerConstitution { get; set; } = 3f;

    // --- Приватные поля ---
    private CombatStatsComponent _stats;

    public override void _Ready()
    {
        // Создаём таймер регенерации
        _regenTimer = new Timer();
        _regenTimer.WaitTime = 1.0f;
        _regenTimer.Timeout += OnRegenTick;
        AddChild(_regenTimer);

        // Инициализируем ресурсы
        InitializeResource("MP", BaseMP, DefaultRegenPerSecond, DefaultRegenDelay);
        InitializeResource("Stamina", BaseStamina, DefaultRegenPerSecond, DefaultRegenDelay);

        // Если нужно использовать статы для максимумов
        if (UseStatsForMax)
        {
            _stats = GetNode<CombatStatsComponent>("../CombatStatsComponent");
            if (_stats != null)
            {
                _stats.StatChanged += OnStatChanged;
                UpdateMaxFromStats();
            }
            else
            {
                GD.PrintErr($"{Owner.Name}: UseStatsForMax = true, но CombatStatsComponent не найден!");
            }
        }


        
         if (_resources.ContainsKey("MP"))
        {
            var mpData = _resources["MP"];
            mpData.RegenPerSecond = 5.0f;   // 5 единиц в секунду
            mpData.RegenDelay = 2.0f;
            mpData.SmoothRegen = true;       // задержка 2 секунды
        }

      
    }

    private void InitializeResource(string id, float max, float regenPerSecond, float regenDelay)
    {
        if (!_resources.ContainsKey(id))
        {
            _resources[id] = new ResourceData
            {
                Max = max,
                Current = max,
                RegenPerSecond = regenPerSecond,
                RegenDelay = regenDelay,
                CooldownRemaining = 0
            };
        }
    }

    public override void _Process(double delta)
    {
        bool anyCooldown = false;
        bool hasSmoothRegen = false;   // есть ли плавно восстанавливающийся ресурс
        bool hasDiscreteRegen = false; // есть ли ресурс, требующий таймера

        foreach (var kvp in _resources)
        {
            var data = kvp.Value;

            // Уменьшаем задержку
            if (data.CooldownRemaining > 0)
            {
                data.CooldownRemaining -= (float)delta;
                anyCooldown = true;
                continue; // пока задержка активна — восстановления нет
            }

            // Проверяем, нужно ли восстанавливать
            if (data.RegenPerSecond <= 0 || data.Current >= data.Max)
                continue;

            if (data.SmoothRegen)
            {
                // Плавное: добавляем порцию прямо сейчас
                float toAdd = data.RegenPerSecond * (float)delta;
                Restore(kvp.Key, toAdd);
                hasSmoothRegen = true;
            }
            else
            {
                // Дискретное: будет восстановлено таймером
                hasDiscreteRegen = true;
            }
        }

        // Управление таймером (только для дискретных ресурсов)
        if (!anyCooldown && hasDiscreteRegen)
        {
            if (_regenTimer.IsStopped())
                _regenTimer.Start();
        }
        else if (hasSmoothRegen && !hasDiscreteRegen)
        {
            // Все регенерирующие ресурсы – плавные, таймер не нужен
            _regenTimer.Stop();
        }
        else if (!hasDiscreteRegen)
        {
            _regenTimer.Stop();
        }
    }

    private void OnRegenTick()
    {
        GD.Print($"[Тик регенерации] Сработал в {DateTime.Now:T}");
        bool anyRegen = false;
        foreach (var kvp in _resources)
        {
            var data = kvp.Value;
            if (data.RegenPerSecond <= 0) continue;
            if (data.CooldownRemaining > 0) continue;
            if (data.Current >= data.Max) continue;

            Restore(kvp.Key, data.RegenPerSecond);
            anyRegen = true;
        }

        if (!anyRegen)
            _regenTimer.Stop();
    }

   

    // --- Обработка изменений статов ---
    private void OnStatChanged(string statName, float newValue)
    {
        if (statName == "Intelligence" || statName == "Constitution")
            UpdateMaxFromStats();
    }

    private void UpdateMaxFromStats()
    {
        if (_stats == null) return;

        // Мана от Интеллекта
        if (_resources.ContainsKey("MP"))
        {
            float newMaxMP = BaseMP + _stats.Intelligence * MPPerIntelligence;
            SetMax("MP", newMaxMP);
        }

        // Выносливость от Телосложения
        if (_resources.ContainsKey("Stamina"))
        {
            float newMaxStamina = BaseStamina + _stats.Constitution * StaminaPerConstitution;
            SetMax("Stamina", newMaxStamina);
        }
    }

    // --- Публичные методы ---
    public float GetCurrent(string id)
    {
        return _resources.TryGetValue(id, out var data) ? data.Current : 0f;
    }

    public float GetMax(string id)
    {
        return _resources.TryGetValue(id, out var data) ? data.Max : 0f;
    }

    public bool HasEnough(string id, float amount)
    {
        return GetCurrent(id) >= amount;
    }

    public bool Consume(string id, float amount)
    {
        if (amount <= 0) return true;
        if (!_resources.TryGetValue(id, out var data)) return false;
        if (data.Current < amount) return false;

        data.Current -= amount;
        data.CooldownRemaining = data.RegenDelay;
        EmitSignal(SignalName.ResourceChanged, id, data.Current, data.Max);

        if (data.Current <= 0)
            EmitSignal(SignalName.ResourceDepleted, id);

        
        return true;
    }

    public void Restore(string id, float amount)
    {
        if (amount <= 0) return;
        if (!_resources.TryGetValue(id, out var data)) return;

        float oldCurrent = data.Current;
        data.Current = Mathf.Min(data.Current + amount, data.Max);
        EmitSignal(SignalName.ResourceChanged, id, data.Current, data.Max);

      
           
    }

    public void SetCurrent(string id, float value)
    {
        if (!_resources.TryGetValue(id, out var data)) return;
        data.Current = Mathf.Clamp(value, 0, data.Max);
        EmitSignal(SignalName.ResourceChanged, id, data.Current, data.Max);
        if (data.Current <= 0)
            EmitSignal(SignalName.ResourceDepleted, id);
    }
    public void SetMax(string id, float newMax)
    {
        if (!_resources.TryGetValue(id, out var data)) return;
        data.Max = Mathf.Max(1, newMax);
        if (data.Current > data.Max)
            data.Current = data.Max;
        EmitSignal(SignalName.ResourceChanged, id, data.Current, data.Max);
    }

    public void SetRegenSettings(string id, float regenPerSecond, float regenDelay)
    {
        if (_resources.TryGetValue(id, out var data))
        {
            data.RegenPerSecond = regenPerSecond;
            data.RegenDelay = regenDelay;
           
        }
    }

    public string[] GetResourceIds()
    {
        string[] ids = new string[_resources.Count];
        _resources.Keys.CopyTo(ids, 0);
        return ids;
    }
}