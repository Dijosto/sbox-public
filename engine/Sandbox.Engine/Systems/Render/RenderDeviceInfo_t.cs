using Sandbox;

internal struct RenderDeviceInfo_t
{
	public int m_nVersion;
	public RenderDisplayMode_t m_DisplayMode;
	public int m_nBackBufferCount;             // valid values are 1 or 2 [2 results in triple buffering]
	public NativeEngine.RenderMultisampleType m_nMultisampleType;
	public byte m_nModeUsage;                 // RENDER_DISPLAY_MODE usage flags for fullscreen/windowed.
	public byte m_bUseStencil;
	public byte m_bWaitForVSync;           // Would we not present until vsync?
	public byte m_bUsingMultipleWindows;   // Forces D3DPresent to use _COPY instead
	public byte m_bIsMainWindow;

	public byte m_padding01;
}

internal struct RenderDisplayMode_t
{
	public int m_nVersion;
	public int m_nWidth;                   // 0 when running windowed means use desktop resolution
	public int m_nHeight;
	public ImageFormat m_Format;           // use ImageFormats (ignored for windowed mode)
	public int m_nRefreshRateNumerator;    // Refresh rate. Use 0 in numerator + denominator for a default setting.
	public int m_nRefreshRateDenominator;  // Refresh rate = numerator / denominator.
	public uint m_nFlags;
}
