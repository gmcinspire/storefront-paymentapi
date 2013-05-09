using System.Collections.Generic;
using System.Linq;
using Gmc.Fido.Payment.Api;

namespace Payment.Dummy
{
	public class Settings
	{
		readonly Dictionary<string, string> _settings;

		public Settings(Dictionary<string, string> settings)
		{
			_settings = settings;
		}

		string PayString
		{
			get { return _settings[SettingsIds.Pay].ToLower(); }
		}

		public bool Pay
		{
			get { return PayString == "true"; }
		}

		string UseSetupString
		{
			get { return _settings[SettingsIds.UseSetup].ToLower(); }
		}

		public bool UseSetup
		{
			get { return UseSetupString == "true"; }
		}

		public IEnumerable<string> SupportedCurrencies
		{
			get { return _settings[SettingsIds.Currencies].Split(',').Select(c => c.Trim()); }
		}

		public static PaymentSettingsEntry[] GetDescription()
		{
			return new[]
				{
					new PaymentSettingsEntry(SettingsIds.Pay, "Pay", "'true' to instantly mark order as 'Paid', 'false' to cancel payment."),
					new PaymentSettingsEntry(SettingsIds.UseSetup, "Use setup", "'true' to display the setup form."),
					new PaymentSettingsEntry(SettingsIds.Currencies, "Supported currencies", "Comma separated list of of currency codes that this plugin claims to support.")
				};
		}

		public string[] Validate()
		{
			var errors = new List<string>();

			if (PayString != "true" && PayString != "false")
				errors.Add("'Pay' needs to be set to either 'true' or 'false'.");

			if (UseSetupString != "true" && UseSetupString != "false")
				errors.Add("'Use setup' needs to be set to either 'true' or 'false'.");

			if (SupportedCurrencies.Any(c => c.Length != 3 && c.Length != 0))
				errors.Add("'Supported currencies' are not in correct format. Every code is supposed to be three letters long. Codes should be separated by ','.");

			return errors.ToArray();
		}
	}
}