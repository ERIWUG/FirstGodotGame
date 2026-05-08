using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BattleManager : Node
{
	[Export] public NodePath PlayerPath;
	[Export] public NodePath EnemyPath;

	private HealthComponent _playerHealth;
	private ResourceComponent _playerResources;
	private CombatStatsComponent _playerStats;
	private LevelComponent _playerLevel;
	private SpellData[] _quickSlots = new SpellData[5];
	private SkillComponent _playerSkill;

	private HealthComponent _enemyHealth;
	private CombatStatsComponent _enemyStats;
	private LevelComponent _enemyLevel; // если есть опыт за убийство

	private Node _enemy;
	private ProgressBar _enemyHPBar;
	private Label _enemyHPLabel;

	private Dictionary<string, Sprite2D> _activeEffectSprites = new();
	private Label _hpLabel;
	private Label _mpLabel;
	private Label _expLabel;
	private ProgressBar _hpBar;
	private ProgressBar _mpBar;

	private SpellCrafterComponent _crafter;
	private SpellData _lastCraftedSpell;

	private CanvasLayer _ui;
	private AttributeWindow _attrWindow;

	public override void _Ready()
	{
		_ui = GetParent().GetNode<CanvasLayer>("UI");
		// Получаем компонентыh
		var player = GetNode(PlayerPath);
		_playerHealth = player.GetNode<HealthComponent>("HealthComponent");
		_playerResources = player.GetNode<ResourceComponent>("ResourceComponent");
		_playerStats = player.GetNode<CombatStatsComponent>("CombatStatsComponent");
		_playerLevel = player.GetNode<LevelComponent>("LevelComponent");
		_playerSkill = player.GetNode<SkillComponent>("SkillComponent");

		_crafter = player.GetNode<SpellCrafterComponent>("SpellCrafterComponent");


		_enemy = GetNode(EnemyPath);
		_enemyHealth = _enemy.GetNode<HealthComponent>("HealthComponent");
		_enemyHealth.HealthDepleted += OnEnemyDied;
		_enemyStats = _enemy.GetNode<CombatStatsComponent>("CombatStatsComponent");
		_enemyLevel = _enemy.GetNodeOrNull<LevelComponent>("LevelComponent");
		_enemyHPBar = _ui.GetNode<ProgressBar>("EnemyStatusPanel/EnemyHPBar");
		_enemyHPLabel = _ui.GetNode<Label>("EnemyStatusPanel/EnemyHPLabel");



		// Находим UI элементы (предположим, они есть в сцене)
		_hpLabel = _ui.GetNode<Label>("HPLabel");
		_mpLabel = _ui.GetNode<Label>("MPLabel");
		_expLabel = _ui.GetNode<Label>("ExpLabel");
		_hpBar = _ui.GetNode<ProgressBar>("HPBar");
		_mpBar = _ui.GetNode<ProgressBar>("MPBar");

		// Подписываемся на сигналы для обновления UI
		_playerHealth.HealthChanged += OnPlayerHealthChanged;
		_playerResources.ResourceChanged += OnPlayerResourceChanged;
		_playerLevel.ExperienceChanged += OnExpChanged;
		_playerLevel.LevelUp += OnLevelUp;

		_enemyHealth.HealthChanged += OnEnemyHealthChanged;



		_ui.GetNode<Button>("OpenCrafterButton").Pressed += OnOpenCrafter;
		_ui.GetNode<Button>("CreateSpellButton").Pressed += OnCreateSpell;
		
		_ui.GetNode<Button>("AttackButton").Pressed += OnAttackButtonPressed;

		
		var enemyStatus = _enemy.GetNode<StatusEffectsComponent>("StatusEffectsComponent");
		enemyStatus.EffectApplied += OnEnemyEffectApplied;
		enemyStatus.EffectRemoved += OnEnemyEffectRemoved;

		OnEnemyHealthChanged(_enemyHealth.CurrentHealth, _enemyHealth.MaxHealth);

		SetupQuickSlots();

		var attrScene = GD.Load<PackedScene>("res://Scenes/AttributeWindow.tscn");
		_attrWindow = attrScene.Instantiate<AttributeWindow>();
		_ui.AddChild(_attrWindow);
		_attrWindow.Initialize(_playerLevel, _playerStats, _playerHealth, _playerResources);

		_playerLevel.LevelUp += (int level) =>
		{
			_attrWindow.UpdateUI(); // на случай, если очки уже начислены
			_attrWindow.PopupCentered();
			GetTree().Paused = true;
		};

		// Инициализируем UI
		UpdateUI();
	}
	#region EnemyControls

	private void OnEnemyDied()
	{
		GD.Print("Враг повержен!");

		// Даём опыт
		if (_enemyLevel != null)
		{
			int expReward = _enemyLevel.CurrentLevel * 50;
			_playerLevel.AddExperience(expReward);
		}
		else
		{
			_playerLevel.AddExperience(50);
		}

		// Возрождаем врага (или завершаем бой)
		_enemyHealth.SetHealth(_enemyHealth.MaxHealth);
	}

	private void OnEnemyHealthChanged(float current, float max)
	{
		_enemyHPBar.MaxValue = max;
		_enemyHPBar.Value = current;
		_enemyHPLabel.Text = $"HP: {current:F0}/{max:F0}";
	}
	private void OnEnemyEffectApplied(string effectId)
	{
		GD.Print($"EffectApplied received: '{effectId}'");

		// Загружаем иконку эффекта
		StatusEffect effect = null;
		if (effectId == "burn")
			effect = GD.Load<StatusEffect>("res://Resources/StatusEffects/burn.tres");
		else
			effect = GD.Load<StatusEffect>($"res://Resources/StatusEffects/{effectId}.tres");

		if (effect == null || effect.Icon == null)
		{
			GD.PrintErr($"Не удалось загрузить иконку для эффекта {effectId}");
			return;
		}

		// Создаём спрайт внутри врага
		var marker = _enemy.GetNode<Marker2D>("StatusIconsAnchor");
		if (marker == null)
		{
			GD.PrintErr("У врага нет Marker2D с именем 'StatusIconsAnchor'!");
			return;
		}

		var sprite = new Sprite2D();
		sprite.Texture = effect.Icon;
		sprite.Scale = new Vector2(0.5f, 0.5f); // иконка станет 16×16 (если исходная 32×32)
		sprite.Position = new Vector2(_activeEffectSprites.Count * 20, 0); // смещаем каждую следующую иконку вправо
		marker.AddChild(sprite);

		_activeEffectSprites[effectId] = sprite;
	}

	private void OnEnemyEffectRemoved(string effectId)
	{
		if (_activeEffectSprites.TryGetValue(effectId, out var sprite))
		{
			sprite.QueueFree();
			_activeEffectSprites.Remove(effectId);
		}
	}
	


	#endregion
	// Каждый кадр обновляем позицию контейнера, чтобы он следовал за врагом
	public override void _Process(double delta)
	{
		
	}

	
	


	

	private void OnPlayerHealthChanged(float current, float max)
	{
		_hpBar.MaxValue = max;
		_hpBar.Value = current;
		_hpLabel.Text = $"HP: {current:F0}/{max:F0}";
	}

	private void OnPlayerResourceChanged(string id, float current, float max)
	{
		if (id == "MP")
		{
			_mpBar.MaxValue = max;
			_mpBar.Value = current;
			_mpLabel.Text = $"MP: {current:F0}/{max:F0}";
		}
	}

	private void OnExpChanged(int currentExp, int expToNextLevel)
	{
		int requiredForNext = currentExp + expToNextLevel;
		_expLabel.Text = $"До уровня:{requiredForNext}/{expToNextLevel}";
	}
	private void OnLevelUp(int newLevel)
	{
		GD.Print($"Уровень повышен до {newLevel}!");
		// Здесь можно обновить UI уровня
	}

	private void UpdateUI()
	{
		OnPlayerHealthChanged(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
		OnPlayerResourceChanged("MP", _playerResources.GetCurrent("MP"), _playerResources.GetMax("MP"));
		OnExpChanged(_playerLevel.CurrentExperience, _playerLevel.GetExpToNextLevel());
	}

	#region SpellControl

	private SpellData CreateSpell(string name, params string[] modifierPaths)
	{
		var spell = new SpellData();
		spell.Modifiers = new List<ModifierData>();
		foreach (var path in modifierPaths)
		{
			var mod = GD.Load<ModifierData>(path);
			if (mod != null) spell.Modifiers.Add(mod);
		}
		spell.RecalculateStats();
		spell.SpellName = name;
		_playerSkill.LearnSpell(spell);
		return spell;
	}

	private void SetupQuickSlots()
	{
		// Слот 1: Огненная стрела (уже есть)
		_quickSlots[0] = CreateSpell("Огненная стрела",
			"res://Resources/Modifiers/projectyle_form.tres",
			"res://Resources/Modifiers/fire_element.tres",
			"res://Resources/Modifiers/burn_effect.tres");

		// Слот 2: Ядовитый снаряд
		_quickSlots[1] = CreateSpell("Ядовитый снаряд",
			"res://Resources/Modifiers/projectyle_form.tres",
			"res://Resources/Modifiers/poison_effect.tres"); // без элемента, пусть будет чистый яд

		// Слот 3: Ледяное копьё (замедляет)
		_quickSlots[2] = CreateSpell("Ледяное копьё",
			"res://Resources/Modifiers/projectyle_form.tres",
			"res://Resources/Modifiers/ice_element.tres",
			"res://Resources/Modifiers/freeze_effect.tres");

		// Слот 4: Молния (просто большой урон)
		_quickSlots[3] = CreateSpell("Молния",
			"res://Resources/Modifiers/projectyle_form.tres",
			"res://Resources/Modifiers/lightning_element.tres");

		// Слот 5: Чистая магия (без эффектов)
		_quickSlots[4] = CreateSpell("Магический снаряд",
			"res://Resources/Modifiers/projectyle_form.tres");
	}

	#endregion



	#region ButtonsControl

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) // клавиша Escape
		{
			GetTree().Paused = !GetTree().Paused;
			var status = _ui.GetNode<Label>("CrafterStatus");
			status.Text = GetTree().Paused ? "ПАУЗА" : "БОЙ";
		}

		if (@event.IsActionPressed("ui_character_sheet") && _playerLevel.AvailableAttributePoints > 0)
		{
			 _attrWindow.PopupCentered();
			 GetTree().Paused = true;
		}

		if (!GetTree().Paused)
		{
			for (int i = 0; i < _quickSlots.Length; i++)
			{
				if (@event.IsActionPressed($"spell_{i + 1}"))
				{
					if (_quickSlots[i] != null)
					{
						_playerSkill.CastSpell(_quickSlots[i], _enemy);
						GD.Print($"Слот {i + 1}: {_quickSlots[i].SpellName}");
					}
					else
					{
						GD.Print($"Слот {i + 1} пуст!");
					}
				}
			}
		}
	}

	public void OnCastButtonPressed(string spellName)
	{
		var spell = _playerSkill.GetKnownSpells().FirstOrDefault(s => s.SpellName == spellName);
		if (spell != null)
		{
			_playerSkill.CastSpell(spell, _enemy);
		}
	}

	public void OnAttackButtonPressed()
	{
		float attack = _playerStats.Strength;
		float defense = _enemyStats.Constitution;
		float damage = Mathf.Max(1, attack - defense / 2);

		_enemyHealth.Damage(damage);
		GD.Print($"Игрок наносит {damage} урона. У врага осталось {_enemyHealth.CurrentHealth} HP.");
	}

	private void OnOpenCrafter()
	{
		_crafter.ClearCurrent();
		var modifiers = LoadAllModifiers();
		foreach (var mod in modifiers)
		{
			_crafter.TryAddModifier(mod);
		}
		UpdateCrafterStatus();
	}

	private List<ModifierData> LoadAllModifiers()
	{
		var list = new List<ModifierData>();
		using var dir = DirAccess.Open("res://Resources/Modifiers/");
		if (dir != null)
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();
			while (fileName != "")
			{
				if (fileName.EndsWith(".tres") || fileName.EndsWith(".res"))
				{
					var mod = GD.Load<ModifierData>($"res://Resources/Modifiers/{fileName}");
					if (mod != null) list.Add(mod);
				}
				fileName = dir.GetNext();
			}
			dir.ListDirEnd();
		}
		return list;
	}

	private void OnCreateSpell()
	{
		var nameInput = _ui.GetNode<LineEdit>("SpellNameInput");
		string spellName = nameInput.Text;
		if (string.IsNullOrEmpty(spellName))
			spellName = "Тестовое заклинание";

		_lastCraftedSpell = _crafter.CraftSpell(spellName);
		if (_lastCraftedSpell != null)
		{
			_playerSkill.LearnSpell(_lastCraftedSpell);
			_ui.GetNode<Label>("CrafterStatus").Text = $"Заклинание '{_lastCraftedSpell.SpellName}' создано! MP: {_lastCraftedSpell.TotalManaCost}";
		}
		else
		{
			_ui.GetNode<Label>("CrafterStatus").Text = "Ошибка создания заклинания!";
		}
	}

	

	private void UpdateCrafterStatus()
	{
		var status = _ui.GetNode<Label>("CrafterStatus");
		status.Text = $"Слотов: {_crafter.GetCurrentSlotCount()}/{_crafter.GetMaxSlots()}";
	}
	
	#endregion
}
