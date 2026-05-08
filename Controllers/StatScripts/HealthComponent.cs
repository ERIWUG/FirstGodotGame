using Godot;
using System;

public partial class HealthComponent : Node
{
    // --- Сигналы ---
    [Signal] public delegate void HealthChangedEventHandler(float currentHealth, float maxHealth);
    [Signal] public delegate void HealthDepletedEventHandler();
    [Signal] public delegate void DamagedEventHandler(float amount, float currentHealth);
    [Signal] public delegate void HealedEventHandler(float amount, float currentHealth);

    // --- Экспортируемые поля ---
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public bool StartAtMaxHealth { get; set; } = true;

    // --- Поля регенерации ---
    [Export] public bool EnableRegeneration { get; set; } = true;
    [Export] public float RegenPerSecond { get; set; } = 2.0f;
    [Export] public float RegenDelayAfterDamage { get; set; } = 3.0f;

    // --- Поля связи со статами ---
    [Export] public bool UseStatsForMaxHealth { get; set; } = false;
    [Export] public float BaseHealth { get; set; } = 10f;
    [Export] public float HealthPerConstitution { get; set; } = 2f;

    // --- Приватные поля ---
    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            var previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

            if (_currentHealth <= 0 && previousHealth > 0)
                EmitSignal(SignalName.HealthDepleted);
        }
    }

    private CombatStatsComponent _stats;
    private Timer _regenTimer;
    private float _regenCooldown;

    public override void _Ready()
    {
        // Создаём таймер регенерации
        _regenTimer = new Timer();
        _regenTimer.WaitTime = 1.0f;
        _regenTimer.Timeout += OnRegenTick;
        AddChild(_regenTimer);

        // Если нужно использовать статы для расчёта MaxHealth
        if (UseStatsForMaxHealth)
        {
            _stats = GetNode<CombatStatsComponent>("../CombatStatsComponent");
            if (_stats != null)
            {
                _stats.StatChanged += OnStatChanged;
                UpdateMaxHealthFromStats();
            }
            else
            {
                GD.PrintErr($"{GetParent().Name}: UseStatsForMaxHealth = true, но CombatStatsComponent не найден!");
            }
        }

        // Устанавливаем начальное здоровье
        if (StartAtMaxHealth)
            CurrentHealth = MaxHealth;
        else
            CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);

        // Запускаем регенерацию при необходимости
        if (EnableRegeneration && CurrentHealth < MaxHealth)
            _regenTimer.Start();
    }

    public override void _Process(double delta)
    {
        if (_regenCooldown > 0)
        {
            _regenCooldown -= (float)delta;
            if (_regenCooldown <= 0 && EnableRegeneration && CurrentHealth < MaxHealth && !_regenTimer.IsStopped())
                _regenTimer.Start();
        }
    }

    private void OnRegenTick()
    {
        if (!EnableRegeneration) return;
        if (_regenCooldown > 0) return;
        if (CurrentHealth >= MaxHealth)
        {
            _regenTimer.Stop();
            return;
        }

        Heal(RegenPerSecond);
    }

    private void OnStatChanged(string statName, float newValue)
    {
        if (statName == "Constitution")
            UpdateMaxHealthFromStats();
    }

    private void UpdateMaxHealthFromStats()
    {
        if (_stats == null) return;

        float constitution = _stats.Constitution;
        float newMax = BaseHealth + (constitution * HealthPerConstitution);
        MaxHealth = Mathf.Max(1, newMax);

        if (CurrentHealth > MaxHealth)
            CurrentHealth = MaxHealth;

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public virtual void Damage(float amount)
    {
        if (amount <= 0) return;

        var previousHealth = CurrentHealth;
        CurrentHealth -= amount;

        var actualDamage = previousHealth - CurrentHealth;
        EmitSignal(SignalName.Damaged, actualDamage, CurrentHealth);
        DamageNumbers.Show(GetParent<Node2D>().GlobalPosition, actualDamage, new Color(1, 0.3f, 0.3f)); // светло-красный

        // Сброс задержки регенерации
        _regenCooldown = RegenDelayAfterDamage;
        _regenTimer.Stop();

        GD.Print($"{GetParent().Name} получил {actualDamage} урона. Осталось {CurrentHealth} HP.");
    }

    public virtual void Heal(float amount)
    {
        if (amount <= 0) return;

        var previousHealth = CurrentHealth;
        CurrentHealth += amount;

        var actualHeal = CurrentHealth - previousHealth;
        EmitSignal(SignalName.Healed, actualHeal, CurrentHealth);
        DamageNumbers.Show(GetParent<Node2D>().GlobalPosition, actualHeal, new Color(0.3f, 1, 0.3f)); // светло-зелёный

        GD.Print($"{GetParent().Name} исцелён на {actualHeal} HP. Текущее здоровье: {CurrentHealth}.");
    }

    public void SetHealth(float newHealth)
    {
        CurrentHealth = newHealth;
    }

    public float GetHealthPercent()
    {
        if (MaxHealth <= 0) return 0;
        return CurrentHealth / MaxHealth;
    }

    public void StartRegeneration()
    {
        if (!EnableRegeneration) return;
        _regenCooldown = 0;
        if (CurrentHealth < MaxHealth)
            _regenTimer.Start();
    }

    public void StopRegeneration()
    {
        _regenTimer.Stop();
    }
}