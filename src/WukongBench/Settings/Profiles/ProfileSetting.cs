namespace WukongBench.Settings.Profiles;

// Одна настройка: в какой секции, какой ключ, какое значение.
public sealed record ProfileSetting(string Section, string Key, string Value);
