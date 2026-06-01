using DXMainClientMvvmContract;

namespace DXMainClientViewModel.Online;

public readonly record struct Rgb24Color(byte R, byte G, byte B) : IRgb24Color;
