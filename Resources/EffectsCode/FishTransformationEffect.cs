using Godot;

[GlobalClass]
public partial class FishTransformationEffect : StatusEffect
{
    [Export] public Texture2D FishTexture { get; set; }

    private Texture2D _originalTexture;   // запомним настоящий спрайт цели
    private Sprite2D _sprite;             // ссылка на спрайт цели

    public override void OnApply(StatusEffectsComponent target)
    {
        // Ищем спрайт у родителя цели. Обычно это враг с Sprite2D как дочерний узел.
        _sprite = target.GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");

        if (_sprite != null && FishTexture != null)
        {
            _originalTexture = _sprite.Texture;   // запоминаем оригинал
            _sprite.Texture = FishTexture;        // подменяем на рыбу
        }

        // Эффекты вроде PreventActions / PreventMovement настроим в ресурсе, код не трогаем
    }

    public override void OnExpire(StatusEffectsComponent target)
    {
        // Возвращаем оригинальную текстуру
        if (_sprite != null && _originalTexture != null)
        {
            _sprite.Texture = _originalTexture;
        }
    }
}

