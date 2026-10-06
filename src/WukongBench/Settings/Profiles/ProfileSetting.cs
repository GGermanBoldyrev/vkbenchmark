namespace WukongBench.Settings.Profiles;

// Одна настройка: в какой секции, какой ключ, какое значение.
internal sealed record ProfileSetting(string Section, string Key, string Value);
