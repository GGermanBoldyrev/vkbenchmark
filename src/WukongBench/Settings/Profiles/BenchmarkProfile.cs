namespace WukongBench.Settings.Profiles;

// Один проход бенчмарка: имя (из имени файла) и настройки, которые надо выставить.
internal sealed record BenchmarkProfile(string Name, IReadOnlyList<ProfileSetting> Settings);
