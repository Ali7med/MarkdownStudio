using System.Windows.Data;
using System.Windows.Markup;

namespace MarkdownStudio.Localization;

/// <summary>
/// امتداد XAML للنصوص المُوطَّنة: <c>{loc:Loc toolbar.new}</c>.
/// يبني ربطاً بمفهرس <see cref="Localizer"/> فيتحدّث النص فوراً عند تبديل اللغة.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public LocExtension() { }
    public LocExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
