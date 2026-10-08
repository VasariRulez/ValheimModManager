namespace ValheimModManager.Core.Localization;

using System;
using System.Collections.Generic;
using System.Globalization;

public sealed record LanguageOption(string Code, string DisplayName);

public sealed class LocalizationService
{
    public const string LanguageItalian = "it";
    public const string LanguageEnglish = "en";

    public static LocalizationService Instance { get; } = new();

    public AppStrings CurrentStrings { get; set; } = ItalianStrings.Instance;

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new LanguageOption(LanguageItalian, "Italiano 🇮🇹"),
        new LanguageOption(LanguageEnglish, "English 🇬🇧")
    ];

    public AppStrings GetStrings(string? languageCode)
    {
        return (languageCode?.Trim().ToLowerInvariant()) switch
        {
            LanguageEnglish => EnglishStrings.Instance,
            LanguageItalian => ItalianStrings.Instance,
            _ => DetectDefaultStrings()
        };
    }

    public string NormalizeLanguageCode(string? languageCode)
    {
        return (languageCode?.Trim().ToLowerInvariant()) switch
        {
            LanguageEnglish => LanguageEnglish,
            LanguageItalian => LanguageItalian,
            _ => DetectInitialLanguage()
        };
    }

    public string DetectInitialLanguage()
    {
        var current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return current == LanguageItalian ? LanguageItalian : LanguageEnglish;
    }

    private AppStrings DetectDefaultStrings()
    {
        return DetectInitialLanguage() == LanguageItalian
            ? ItalianStrings.Instance
            : EnglishStrings.Instance;
    }
}
