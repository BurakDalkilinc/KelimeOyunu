/// <summary>
/// "melee": silah ateş etmez, yakından vurur (kılıç, sopa). "weapon" etiketiyle birlikte kullanılır; Boşluk'a basınca
/// önündeki yay içindekilere kelimenin hasarı kadar vurur (bkz. <see cref="MeleeRules"/>). Menzil "stats.radius".
/// Yaratık davranışı yoktur; etiket yalnızca silahın türünü söyler.
/// </summary>
public class MeleeTag : ITagBehaviour
{
    public const string Name = "melee";

    public string Tag => Name;

    public void Configure(CreatureModel creature, SpawnContext context) { }
}
