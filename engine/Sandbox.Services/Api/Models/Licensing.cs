namespace Sandbox;

public enum LicenseType
{
	None,
	CC0,
	CCBYNCND,
	CCBY,
	CCBYSA
}

public static class Licensing
{
	public readonly record struct LicenseDescription( LicenseType type, string name, string title, string icon, string description, string url );

	public static LicenseDescription[] Assets = new LicenseDescription[]
	{
		new ()
		{
			type = LicenseType.CC0,
			name = "CC0",
			title = "CC0",
			icon = "https://licensebuttons.net/l/zero/1.0/88x31.png",
			description = "CC0 allows reusers to distribute, remix, adapt, and build upon the material in any medium or format, with no conditions.",
			url = "https://creativecommons.org/publicdomain/zero/1.0/"
		},

		new ()
		{
			type = LicenseType.CCBYNCND,
			name = "CC_BYNCND",
			title = "CC BY-NC-ND",
			icon = "https://licensebuttons.net/l/by-nc-nd/4.0/88x31.png",
			description = "Allows reusers to copy and distribute the material in any medium or format in unadapted form only, for noncommercial purposes only, and only so long as attribution is given to the creator.",
			url = "https://creativecommons.org/licenses/by-nc-nd/4.0/"
		},

		new ()
		{
			type = LicenseType.CCBY,
			name = "CC_BY",
			title = "CC BY",
			icon = "https://licensebuttons.net/l/by/4.0/88x31.png",
			description = "Allows reusers to distribute, remix, adapt, and build upon the material in any medium or format, so long as attribution is given to the creator. The license allows for commercial use.",
			url = "https://creativecommons.org/licenses/by-nc-nd/4.0/"
		},

		new ()
		{
			type = LicenseType.CCBYSA,
			name = "CC_BYSA",
			title = "CC BY-SA",
			icon = "https://licensebuttons.net/l/by-sa/4.0/88x31.png",
			description = "Allows reusers to copy and distribute the material in any medium or format in unadapted form only, for noncommercial purposes only, and only so long as attribution is given to the creator.",
			url = "https://creativecommons.org/licenses/by-nc-nd/4.0/"
		},
	};
}
